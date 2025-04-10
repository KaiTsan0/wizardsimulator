using UnityEngine;
using UnityEngine.EventSystems;

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

    public GameObject currentlySelectedObject;

    [SerializeField]private GameObject currentlyHoveredObject;

    public BuildManager buildManager;

    void Start()
    {
        // Initialize the ghost plane only if the first prefab is not null
        if (planePrefabs.Length > 0 && planePrefabs[selectedPlaneIndex] != null)
        {
            CreateGhostPlane(planePrefabs[selectedPlaneIndex]);
        }

        // Get the BuildManager from the BuildCam GameObject
        buildManager = FindObjectOfType<BuildManager>();

        if (buildManager == null)
        {
            Debug.LogError("BuildManager not found! Please ensure it is attached to the BuildCam GameObject.");
        }

        SetSelectMode();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            Camera.main.cullingMask ^= 1 << LayerMask.NameToLayer("NoBuildZone");
        }

        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetSelectMode();
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

        if (buildManager.IsInSelectMode())
        {
            // Shoot a ray and handle interactions with PlayerPlaced objects
            HandleSelectModeRaycast();
        }

        // Position the ghost plane only if a valid prefab is selected
        if (planePrefabs.Length > 0 && planePrefabs[selectedPlaneIndex] != null)
        {
            if (buildManager.IsInBuildMode() || buildManager.IsInMoveMode())
            {
                // Check if the pointer is over a UI element
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return; // Skip raycasting if the pointer is over a UI element
                }

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

        if (buildManager.IsInSelectMode() && currentlyHoveredObject != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                currentlySelectedObject = currentlyHoveredObject;
                buildManager.SetBuildMode(BuildMode.MoveMode);
                SelectPlaneByName(currentlySelectedObject.name.Replace("(Clone)", "").Trim());

                Transform noBuildZoneChild = currentlySelectedObject.transform.Find("NoBuildZoneChild");
                if (noBuildZoneChild != null)
                {
                    noBuildZoneChild.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("No child object named 'NoBuildZone' found in the ghost plane prefab.");
                }
            }
        }
    }

    public void SetSelectMode()
    {
        SelectPlane(0); // Set selectedPlaneIndex to 0 (no selection)
        buildManager.SetBuildMode(BuildMode.SelectMode);

        if (currentlyHoveredObject != null)
        {
            Outline previousOutline = currentlyHoveredObject.GetComponent<Outline>();
            if (previousOutline != null)
            {
                previousOutline.enabled = false;
            }

            // Clear the reference to the currently selected object
            currentlyHoveredObject = null;
        }

        if (currentlySelectedObject != null)
        {
            Transform noBuildZoneChild = currentlySelectedObject.transform.Find("NoBuildZoneChild");
            if (noBuildZoneChild != null)
            {
                noBuildZoneChild.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No child object named 'NoBuildZone' found in the ghost plane prefab.");
            }

            currentlySelectedObject = null;
        }
    }

    void HandleSelectModeRaycast()
    {
        // Create a layer mask that excludes the "NoBuildZone" layer
        int noBuildZoneLayer = LayerMask.NameToLayer("NoBuildZone");
        int layerMask = ~(1 << noBuildZoneLayer); // Exclude the NoBuildZone layer

        // Perform a raycast from the camera through the mouse position, using the layer mask
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Check if the hit object has the "PlayerPlaced" tag
            if (hit.collider.CompareTag("PlayerPlaced"))
            {
                // Get the GameObject that was hit
                GameObject hitObject = hit.collider.gameObject;

                // If the hit object is different from the currently selected object
                if (currentlyHoveredObject != hitObject)
                {
                    // Disable the outline on the previously selected object
                    if (currentlyHoveredObject != null)
                    {
                        Outline previousOutline = currentlyHoveredObject.GetComponent<Outline>();
                        if (previousOutline != null)
                        {
                            previousOutline.enabled = false;
                        }
                    }

                    // Set the new currently selected object
                    currentlyHoveredObject = hitObject;

                    // Access the Outline component on the hit object
                    Outline outline = hitObject.GetComponent<Outline>();

                    // If the object doesn't have an Outline component, add one
                    if (outline == null)
                    {
                        outline = hitObject.AddComponent<Outline>();
                    }

                    // Enable the outline on the currently selected object
                    outline.enabled = true;
                }
            }
            else
            {
                // If no valid object is hit, disable the outline on the currently selected object
                if (currentlyHoveredObject != null)
                {
                    Outline previousOutline = currentlyHoveredObject.GetComponent<Outline>();
                    if (previousOutline != null)
                    {
                        previousOutline.enabled = false;
                    }

                    // Clear the reference to the currently selected object
                    currentlyHoveredObject = null;
                }
            }
        }
        else
        {
            // If no object is hit, disable the outline on the currently selected object
            if (currentlyHoveredObject != null)
            {
                Outline previousOutline = currentlyHoveredObject.GetComponent<Outline>();
                if (previousOutline != null)
                {
                    previousOutline.enabled = false;
                }

                // Clear the reference to the currently selected object
                currentlyHoveredObject = null;
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
            // Check if the "PlacedPlanes" parent object exists; if not, create it
            GameObject placedPlanesParent = GameObject.Find("PlacedPlanes");
            if (placedPlanesParent == null)
            {
                placedPlanesParent = new GameObject("PlacedPlanes");
            }

            // Instantiate the plane and set its parent to "PlacedPlanes"
            GameObject placedPlane = Instantiate(planePrefabs[selectedPlaneIndex], position, rotation);
            placedPlane.transform.parent = placedPlanesParent.transform;

            // remove money here

            if (buildManager.IsInMoveMode())
            {
                buildManager.SetBuildMode(BuildMode.SelectMode);
                Destroy(currentlySelectedObject);
            }
        }
    }

    public void SelectPlane(int index)
    {
        if (!buildManager.IsInMoveMode())
        {
            buildManager.SetBuildMode(BuildMode.BuildMode);

            if (index >= 0 && index < planePrefabs.Length)
            {
                if (selectedPlaneIndex == index && 0 != index)
                {
                    SetSelectMode();
                    return;
                }
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
        else
        {
            Debug.Log("MOVE MODE");

            if (index >= 0 && index < planePrefabs.Length)
            {
                if (selectedPlaneIndex == index && 0 != index)
                {
                    SetSelectMode();
                    return;
                }

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
        
    }

    /// <summary>
    /// Public method to select a plane prefab by its name.
    /// This method is intended to be used with UI buttons via OnClick events.
    /// </summary>
    /// <param name="planeName">The name of the plane prefab to select.</param>
    public void SelectPlaneByName(string planeName)
    {
        if (buildManager.IsInMoveMode())
        {
            if(planeName == currentlySelectedObject.name.Replace("(Clone)", "").Trim())
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
            }
            else
            {
                return;
            }
            
        }

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

            Transform noBuildZoneChild = ghostPlaneInstance.transform.Find("NoBuildZoneChild");
            if (noBuildZoneChild != null)
            {
                noBuildZoneChild.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No child object named 'NoBuildZone' found in the ghost plane prefab.");
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