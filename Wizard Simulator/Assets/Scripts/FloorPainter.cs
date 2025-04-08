using System.Collections.Generic;
using UnityEngine;

public class FloorPainter : MonoBehaviour
{
    public GameObject floorTilePrefab; // Prefab for the blue plane
    public float hoverHeight = 0.01f;  // Slight offset to avoid z-fighting
    public LayerMask floorLayer;       // Layer mask to detect only the floor
    public float weldThreshold = 0.01f; // Distance threshold for merging vertices

    private List<CombineInstance> combineInstances = new List<CombineInstance>();
    private GameObject mergedMeshObject;
    private Mesh lastMergedMesh; // Store the last merged mesh for visualization
    private List<(int, int)> boundaryEdges; // List of boundary edges (pairs of vertex indices)

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayer))
            {
                Vector3 spawnPosition = hit.point + Vector3.up * hoverHeight;

                // Instantiate the tile temporarily to extract its mesh
                GameObject newTile = Instantiate(floorTilePrefab, spawnPosition, Quaternion.identity);

                // Extract the mesh and add it to the combine instances list
                MeshFilter meshFilter = newTile.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = meshFilter.sharedMesh;
                    ci.transform = newTile.transform.localToWorldMatrix;
                    combineInstances.Add(ci);
                }

                // Destroy the temporary tile
                Destroy(newTile);

                // Merge all tiles into a single mesh
                MergeTiles();
            }
        }
    }

    void MergeTiles()
    {
        if (combineInstances.Count == 0) return;

        // Create or reuse the merged mesh object
        if (mergedMeshObject == null)
        {
            mergedMeshObject = new GameObject("MergedFloor");
            mergedMeshObject.transform.position = Vector3.zero;
        }

        // Combine all meshes into one
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true);

        // Optimize the mesh by welding vertices
        combinedMesh = WeldVertices(combinedMesh, weldThreshold);

        // Remove duplicate triangles
        combinedMesh = RemoveDuplicateTriangles(combinedMesh);

        // Assign the optimized mesh to the merged object
        MeshFilter mergedMeshFilter = mergedMeshObject.GetComponent<MeshFilter>();
        if (mergedMeshFilter == null)
        {
            mergedMeshFilter = mergedMeshObject.AddComponent<MeshFilter>();
        }
        mergedMeshFilter.mesh = combinedMesh;

        // Add a renderer if not already present
        MeshRenderer mergedMeshRenderer = mergedMeshObject.GetComponent<MeshRenderer>();
        if (mergedMeshRenderer == null)
        {
            mergedMeshRenderer = mergedMeshObject.AddComponent<MeshRenderer>();
        }
        mergedMeshRenderer.material = floorTilePrefab.GetComponent<MeshRenderer>().sharedMaterial;

        // Store the last merged mesh for visualization
        lastMergedMesh = combinedMesh;

        // Recalculate boundary edges for the entire mesh
        boundaryEdges = FindBoundaryEdges(lastMergedMesh);
    }

    Mesh WeldVertices(Mesh mesh, float threshold)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Dictionary to map old vertices to new vertices
        Dictionary<int, int> vertexMap = new Dictionary<int, int>();

        // List to store the new vertices
        List<Vector3> weldedVertices = new List<Vector3>();

        // Iterate through all vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            bool found = false;

            // Check if this vertex is close to any existing vertex
            for (int j = 0; j < weldedVertices.Count; j++)
            {
                if (Vector3.SqrMagnitude(vertices[i] - weldedVertices[j]) < threshold * threshold)
                {
                    // Map this vertex to the existing vertex
                    vertexMap[i] = j;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                // Add this vertex as a new unique vertex
                weldedVertices.Add(vertices[i]);
                vertexMap[i] = weldedVertices.Count - 1;
            }
        }

        // Rebuild the triangles using the new vertex indices
        List<int> weldedTriangles = new List<int>();
        foreach (int triangleIndex in triangles)
        {
            weldedTriangles.Add(vertexMap[triangleIndex]);
        }

        // Create a new mesh with the welded vertices and triangles
        Mesh weldedMesh = new Mesh();
        weldedMesh.vertices = weldedVertices.ToArray();
        weldedMesh.triangles = weldedTriangles.ToArray();
        weldedMesh.RecalculateNormals();

        return weldedMesh;
    }

    Mesh RemoveDuplicateTriangles(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // HashSet to track unique triangles
        HashSet<string> uniqueTriangles = new HashSet<string>();
        List<int> uniqueTriangleIndices = new List<int>();

        // Iterate through all triangles
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get the three vertex indices of the triangle
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            // Sort the indices to make the triangle order-independent
            int[] sortedIndices = { v1, v2, v3 };
            System.Array.Sort(sortedIndices);

            // Create a unique key for the triangle
            string triangleKey = $"{sortedIndices[0]}_{sortedIndices[1]}_{sortedIndices[2]}";

            // Check if this triangle is already in the set
            if (!uniqueTriangles.Contains(triangleKey))
            {
                // Add the triangle to the set and keep its indices
                uniqueTriangles.Add(triangleKey);
                uniqueTriangleIndices.Add(v1);
                uniqueTriangleIndices.Add(v2);
                uniqueTriangleIndices.Add(v3);
            }
        }

        // Create a new mesh with the unique triangles
        Mesh uniqueMesh = new Mesh();
        uniqueMesh.vertices = vertices;
        uniqueMesh.triangles = uniqueTriangleIndices.ToArray();
        uniqueMesh.RecalculateNormals();

        return uniqueMesh;
    }

    List<(int, int)> FindBoundaryEdges(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Dictionary<(int, int), int> edgeCounts = new Dictionary<(int, int), int>();

        // Count occurrences of each edge
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            AddEdgeToDictionary(edgeCounts, v1, v2);
            AddEdgeToDictionary(edgeCounts, v2, v3);
            AddEdgeToDictionary(edgeCounts, v3, v1);
        }

        // Collect edges that appear only once (boundary edges)
        List<(int, int)> boundaryEdges = new List<(int, int)>();
        foreach (var kvp in edgeCounts)
        {
            if (kvp.Value == 1)
            {
                boundaryEdges.Add(kvp.Key);
            }
        }

        return boundaryEdges;
    }

    void AddEdgeToDictionary(Dictionary<(int, int), int> edgeCounts, int v1, int v2)
    {
        // Ensure consistent ordering of vertices in the edge
        var edge = v1 < v2 ? (v1, v2) : (v2, v1);

        if (edgeCounts.ContainsKey(edge))
        {
            edgeCounts[edge]++;
        }
        else
        {
            edgeCounts[edge] = 1;
        }
    }

    // Draw gizmos for debugging
    void OnDrawGizmos()
    {
        if (lastMergedMesh != null && boundaryEdges != null)
        {
            Vector3[] vertices = lastMergedMesh.vertices;

            // Draw all edges (for debugging)
            Gizmos.color = Color.yellow;
            int[] triangles = lastMergedMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                Vector3 worldV1 = mergedMeshObject.transform.TransformPoint(vertices[v1]);
                Vector3 worldV2 = mergedMeshObject.transform.TransformPoint(vertices[v2]);
                Vector3 worldV3 = mergedMeshObject.transform.TransformPoint(vertices[v3]);

                Gizmos.DrawLine(worldV1, worldV2);
                Gizmos.DrawLine(worldV2, worldV3);
                Gizmos.DrawLine(worldV3, worldV1);
            }

            // Draw boundary edges
            Gizmos.color = Color.green;
            foreach (var edge in boundaryEdges)
            {
                Vector3 v1 = mergedMeshObject.transform.TransformPoint(vertices[edge.Item1]);
                Vector3 v2 = mergedMeshObject.transform.TransformPoint(vertices[edge.Item2]);
                Gizmos.DrawLine(v1, v2);
            }

            // Optionally, draw vertices as before
            Gizmos.color = Color.red;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 worldPosition = mergedMeshObject.transform.TransformPoint(vertex);
                Gizmos.DrawSphere(worldPosition, 0.02f);
            }
        }
    }
}