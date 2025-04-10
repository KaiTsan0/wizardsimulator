using UnityEngine;

public class NoOutline : MonoBehaviour
{
    private Renderer objectRenderer;
    private Material noBuildZoneMaterial;
    private Material[] originalMaterials; // Cache the original materials for restoration

    void Awake()
    {
        // Get the Renderer component attached to this GameObject
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer == null)
        {
            Debug.LogError("No Renderer found on this GameObject. Please attach a Renderer component.");
            return;
        }

        // Load the "NoBuildZone" material from the Resources folder
        noBuildZoneMaterial = Resources.Load<Material>("NoBuildZone");

        if (noBuildZoneMaterial == null)
        {
            Debug.LogError("NoBuildZone material not found in the Resources folder. Please ensure it exists.");
            return;
        }

        // Cache the original materials for potential restoration
        originalMaterials = objectRenderer.sharedMaterials;
    }

    void Start()
    {
        ApplyNoBuildZoneMaterial();
    }

    void Update()
    {
        // Continuously enforce the NoBuildZone material on Element0
        EnforceNoBuildZoneMaterial();
    }

    void ApplyNoBuildZoneMaterial()
    {
        // Enforce that the materials array contains only the "NoBuildZone" material
        Material[] materials = new Material[1]; // Create a new array with only one slot
        materials[0] = noBuildZoneMaterial;    // Set the first slot to "NoBuildZone"

        // Apply the updated materials array back to the Renderer
        objectRenderer.materials = materials;
    }

    void EnforceNoBuildZoneMaterial()
    {
        // Check if the materials array has been modified
        Material[] currentMaterials = objectRenderer.materials;

        // If there is more than one material or the first material is not "NoBuildZone", reset it
        if (currentMaterials.Length != 1 || currentMaterials[0] != noBuildZoneMaterial)
        {
            ApplyNoBuildZoneMaterial(); // Reapply the single-material rule
        }
    }

    void OnDisable()
    {
        // Restore the original materials when the object is disabled
        if (originalMaterials != null)
        {
            objectRenderer.materials = originalMaterials;
        }
    }
}