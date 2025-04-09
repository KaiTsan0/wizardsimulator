using UnityEngine;

public class BuildCam : MonoBehaviour
{
    // Movement settings
    public float moveSpeed = 5f; // Speed of forward/backward/left/right movement
    public float verticalSpeed = 3f; // Speed of upward/downward movement
    public float lookSpeed = 3f; // Speed of camera rotation

    // State variables
    private Vector2 rotation = Vector2.zero;
    private bool isPanning = false;

    void Update()
    {
        // Check if the right mouse button is held down
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            isPanning = true;
            Cursor.lockState = CursorLockMode.Locked; // Lock cursor to center of screen
            Cursor.visible = false;

            // Get input for camera rotation
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            // Update rotation
            rotation.x -= mouseY; // Vertical rotation (pitch)
            rotation.y += mouseX; // Horizontal rotation (yaw)

            // Clamp vertical rotation to prevent flipping
            rotation.x = Mathf.Clamp(rotation.x, -90f, 90f);

            // Apply rotation to the camera
            transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, 0);
        }
        else
        {
            isPanning = false;
            Cursor.lockState = CursorLockMode.None; // Unlock cursor
            Cursor.visible = true;
        }

        // Handle movement
        float forwardMovement = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime; // W/S
        float sideMovement = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime; // A/D
        float upwardMovement = 0f;

        // Upward/downward movement with Shift/Ctrl
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            upwardMovement += verticalSpeed * Time.deltaTime; // Move up
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            upwardMovement -= verticalSpeed * Time.deltaTime; // Move down
        }

        // Calculate movement direction based on camera orientation
        Vector3 movement = transform.forward * forwardMovement + transform.right * sideMovement + transform.up * upwardMovement;

        // Apply movement
        transform.position += movement;
    }
}