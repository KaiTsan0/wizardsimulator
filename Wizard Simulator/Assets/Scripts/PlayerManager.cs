using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    // Reference to the Player GameObject (used in Play Mode)
    public GameObject playerGameObject;

    // Reference to the Build Cam Camera (used in Build Mode)
    public GameObject buildCamCamera;

    // Reference to the BuildingSystem component
    public BuildingSystem buildingSystem; // Assign this in the Inspector

    // Boolean to track the current mode
    private bool isInBuildMode = false;

    [SerializeField] private float money = 0f; // Private backing field for Money
    public float Money // Property to manage Money with updates
    {
        get { return money; }
        set
        {
            money = value;
            UpdateMoneyText(); // Update the TMP text whenever Money changes
        }
    }

    // Reference to the TextMeshPro UI element
    public TMP_Text moneyTextValue; // Assign this in the Inspector

    void Start()
    {
        // Ensure the Player GameObject is active and the Build Cam Camera is inactive on start
        if (playerGameObject != null)
        {
            playerGameObject.SetActive(true); // Activate Player GameObject
        }

        if (buildCamCamera != null)
        {
            buildCamCamera.SetActive(false); // Deactivate Build Cam Camera
        }

        // Lock the cursor for Play Mode at the start
        SetCursorState(false);

        // Initialize the Money text
        UpdateMoneyText();
    }

    void Update()
    {
        // Check if the "B" key is pressed
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleMode();
        }
    }

    /// <summary>
    /// Called when a value is changed in the Inspector (edit mode or play mode)
    /// </summary>
    private void OnValidate()
    {
        // Ensure the Money property is updated if the money field is changed in the Inspector
        Money = money; // This triggers the setter and updates the UI
    }

    /// <summary>
    /// Toggles between Play Mode and Build Mode
    /// </summary>
    void ToggleMode()
    {
        // Toggle the mode
        isInBuildMode = !isInBuildMode;

        // Activate/Deactivate the Player GameObject
        if (playerGameObject != null)
        {
            buildingSystem.SelectPlane(0); // Deselect any plane
            playerGameObject.SetActive(!isInBuildMode);
        }

        // Activate/Deactivate the Build Cam Camera
        if (buildCamCamera != null)
        {
            buildingSystem.SelectPlane(0); // Deselect any plane
            buildCamCamera.SetActive(isInBuildMode);
        }

        // Update the cursor state based on the mode
        SetCursorState(isInBuildMode);

        // Optional: Log the current mode for debugging
        Debug.Log(isInBuildMode ? "Switched to Build Mode" : "Switched to Play Mode");
    }

    /// <summary>
    /// Sets the cursor state based on the current mode
    /// </summary>
    /// <param name="isBuildMode">True if in Build Mode, False if in Play Mode</param>
    void SetCursorState(bool isBuildMode)
    {
        if (isBuildMode)
        {
            // Unlock the cursor in Build Mode
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Lock the cursor in Play Mode
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Updates the TextMeshPro UI element with the current Money value
    /// </summary>
    void UpdateMoneyText()
    {
        if (moneyTextValue != null)
        {
            moneyTextValue.text = $"£{Money:F2}"; // Format to 2 decimal places
        }
    }

    /// <summary>
    /// Adds a specified amount to the Money value
    /// </summary>
    /// <param name="amount">The amount to add</param>
    public void AddMoney(float amount)
    {
        Money += amount; // Use the property to trigger the update
    }

    /// <summary>
    /// Subtracts a specified amount from the Money value
    /// </summary>
    /// <param name="amount">The amount to subtract</param>
    public void SubtractMoney(float amount)
    {
        Money -= amount; // Use the property to trigger the update
    }
}