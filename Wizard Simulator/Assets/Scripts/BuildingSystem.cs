using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    public GameObject[] planePrefabs; // Array of plane prefabs
    public LayerMask floorLayer; // Layer mask for detecting the floor
    public float gridSize = 0.5f; // Grid size for snapping
    public float heightIncrement = 0.125f; // Height adjustment increment

    private GameObject ghostPlaneInstance; // The ghost plane instance
    private int selectedPlaneIndex = 0; // Index of the currently selected plane
    private float currentHeight = 0f;
    private int currentRotationStep = 0; // Current rotation step (in increments of 90 degrees)

    private bool isInNoBuildZone = false; // Tracks whether the ghost plane is in a no-build zone

    void Start()
    {
        // Initialize the ghost plane only if the first prefab is not null
        if (planePrefabs.Length > 0 && planePrefabs[selectedPlaneIndex] != null)
        {
            CreateGhostPlane(planePrefabs[selectedPlaneIndex]);
        }
    }

    void Update()
    {
        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SelectPlane(0); // Set selectedPlaneIndex to 0 (no selection)
            return; // Exit early to avoid unnecessary processing
        }

        // Adjust height with + and - keys
        if (Input.GetKeyDown(KeyCode.Equals)) // "+" key
        {
            currentHeight += heightIncrement;
        }
        if (Input.GetKeyDown(KeyCode.Minus)) // "-" key
        {
            currentHeight -= heightIncrement;
        }

        // Rotate the ghost plane with Q and E keys
        if (Input.GetKeyDown(KeyCode.Q)) // Rotate counterclockwise
        {
            currentRotationStep--;
            if (currentRotationStep < 0) currentRotationStep += 4; // Wrap around to 360 degrees
        }
        if (Input.GetKeyDown(KeyCode.E)) // Rotate clockwise
        {
            currentRotationStep++;
            if (currentRotationStep >= 4) currentRotationStep -= 4; // Wrap around to 0 degrees
        }
        
        // Position the ghost plane only if a valid prefab is selected
        if (planePrefabs.Length > 0 && planePrefabs[selectedPlaneIndex] != null)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayer))
            {
                Vector3 snappedPosition = SnapToGrid(hit.point, gridSize);
                snappedPosition.y += currentHeight; // Adjust height

                // Update ghost plane position and rotation
                ghostPlaneInstance.transform.position = snappedPosition;

                // Combine the prefab's original rotation with the player-applied rotation
                Quaternion baseRotation = planePrefabs[selectedPlaneIndex].transform.rotation;
                Quaternion playerRotation = Quaternion.Euler(0, 0, currentRotationStep * 90f);
                ghostPlaneInstance.transform.rotation = baseRotation * playerRotation;

                // Check if the ghost plane is in a no-build zone
                CheckNoBuildZone();

                // Place a plane on left mouse click
                if (Input.GetMouseButtonDown(0) && !isInNoBuildZone)
                {
                    PlacePlane(snappedPosition, ghostPlaneInstance.transform.rotation);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the ghost plane is overlapping with any no-build zones
    /// </summary>
    void CheckNoBuildZone()
    {
        // Get the ghost plane's MeshCollider
        MeshCollider meshCollider = ghostPlaneInstance.GetComponent<MeshCollider>();
        if (meshCollider == null || meshCollider.sharedMesh == null)
        {
            Debug.LogWarning("Ghost plane does not have a valid MeshCollider.");
            return;
        }

        // Find all no-build zone colliders
        Collider[] noBuildZones = Physics.OverlapSphere(ghostPlaneInstance.transform.position, 10f, LayerMask.GetMask("NoBuildZone"));

        bool foundNoBuildZone = false;

        foreach (Collider noBuildZone in noBuildZones)
        {
            Vector3 direction;
            float distance;

            if (Physics.ComputePenetration(
                meshCollider,
                ghostPlaneInstance.transform.position,
                ghostPlaneInstance.transform.rotation,
                noBuildZone,
                noBuildZone.transform.position,
                noBuildZone.transform.rotation,
                out direction,
                out distance
            ))
            {
                foundNoBuildZone = true;
                break;
            }
        }

        // Update the ghost plane material based on whether it's in a no-build zone
        if (foundNoBuildZone)
        {
            if (!isInNoBuildZone)
            {
                ApplyNoBuildMaterial();
                isInNoBuildZone = true;
            }
        }
        else
        {
            if (isInNoBuildZone)
            {
                ApplyGhostMaterial();
                isInNoBuildZone = false;
            }
        }
    }

    /// <summary>
    /// Applies the "NoBuildMaterial" to the ghost plane
    /// </summary>
    void ApplyNoBuildMaterial()
    {
        Renderer renderer = ghostPlaneInstance.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material noBuildMaterial = Resources.Load<Material>("NoBuildMaterial"); // Load the no-build material
            if (noBuildMaterial != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = noBuildMaterial;
                }
                renderer.materials = materials;
            }
            else
            {
                Debug.LogError("NoBuildMaterial not found! Please ensure the material is in the Resources folder.");
            }
        }
    }

    /// <summary>
    /// Applies the "GhostMaterial" to the ghost plane
    /// </summary>
    void ApplyGhostMaterial()
    {
        Renderer renderer = ghostPlaneInstance.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material ghostMaterial = Resources.Load<Material>("GhostMaterial"); // Load the ghost material
            if (ghostMaterial != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = ghostMaterial;
                }
                renderer.materials = materials;
            }
            else
            {
                Debug.LogError("GhostMaterial not found! Please ensure the material is in the Resources folder.");
            }
        }
    }

    Vector3 SnapToGrid(Vector3 position, float gridSize)
    {
        float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(position.y / gridSize) * gridSize;
        float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
        return new Vector3(snappedX, snappedY, snappedZ);
    }

    void PlacePlane(Vector3 position, Quaternion rotation)
    {
        // Instantiate the selected plane prefab only if it's not null
        if (selectedPlaneIndex >= 0 && selectedPlaneIndex < planePrefabs.Length && planePrefabs[selectedPlaneIndex] != null)
        {
            Instantiate(planePrefabs[selectedPlaneIndex], position, rotation);
        }
    }

    public void SelectPlane(int index)
    {
        if (index >= 0 && index < planePrefabs.Length)
        {
            selectedPlaneIndex = index;

            // Update the ghost plane only if the selected prefab is not null
            if (planePrefabs[selectedPlaneIndex] != null)
            {
                CreateGhostPlane(planePrefabs[selectedPlaneIndex]);
            }
            else
            {
                // Destroy the ghost plane if no prefab is selected
                if (ghostPlaneInstance != null)
                {
                    Destroy(ghostPlaneInstance);
                    ghostPlaneInstance = null;
                }
            }

            // Reset the rotation step when switching planes
            currentRotationStep = 0;
        }
    }

    /// <summary>
    /// Public method to select a plane prefab by its name.
    /// This method is intended to be used with UI buttons via OnClick events.
    /// </summary>
    /// <param name="planeName">The name of the plane prefab to select.</param>
    public void SelectPlaneByName(string planeName)
    {
        for (int i = 0; i < planePrefabs.Length; i++)
        {
            if (planePrefabs[i] != null && planePrefabs[i].name == planeName)
            {
                SelectPlane(i); // Call the existing SelectPlane method
                Debug.Log($"Selected plane: {planeName}");
                return;
            }
        }

        Debug.LogWarning($"No plane prefab found with the name: {planeName}");
    }

    void CreateGhostPlane(GameObject prefab)
    {
        // Destroy the existing ghost plane if it exists
        if (ghostPlaneInstance != null)
        {
            Destroy(ghostPlaneInstance);
        }

        // Instantiate the new ghost plane only if the prefab is not null
        if (prefab != null)
        {
            ghostPlaneInstance = Instantiate(prefab);

            // Assign the custom ghost material to the ghost plane
            Renderer renderer = ghostPlaneInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material customGhostMaterial = Resources.Load<Material>("GhostMaterial"); // Load your custom material
                if (customGhostMaterial != null)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = customGhostMaterial;
                    }
                    renderer.materials = materials;
                }
                else
                {
                    Debug.LogError("Custom ghost material not found! Please ensure the material is in the Resources folder.");
                }
            }

            // Disable colliders on the ghost plane to prevent interference
            Collider collider = ghostPlaneInstance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (ghostPlaneInstance != null)
        {
            Collider collider = ghostPlaneInstance.GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
            }
        }
    }
}