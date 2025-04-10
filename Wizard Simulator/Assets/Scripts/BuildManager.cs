using UnityEngine;

public enum BuildMode
{
    SelectMode,
    BuildMode
}
public class BuildManager : MonoBehaviour
{
    // Public enum to control the build mode
    public BuildMode currentMode = BuildMode.SelectMode;

    // Method to switch modes
    public void SetBuildMode(BuildMode mode)
    {
        currentMode = mode;
        Debug.Log($"Build mode changed to: {currentMode}");
    }

    // Example method to check the current mode
    public bool IsInBuildMode()
    {
        return currentMode == BuildMode.BuildMode;
    }

    // Example method to check the current mode
    public bool IsInSelectMode()
    {
        return currentMode == BuildMode.SelectMode;
    }
}