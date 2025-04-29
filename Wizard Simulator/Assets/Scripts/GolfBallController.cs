using UnityEngine;
using System.Collections;

public class GolfBallController : MonoBehaviour
{
    //public float forceMultiplier = 1f;
    private Rigidbody rb;
    public float time = 2.5f; // Duration of the slowdown
    public int powerDampen = 100; // Duration of the slowdown
    public Transform cameraParent; // Reference to the CameraParent object
    [SerializeField] private bool canHit = true;
    [SerializeField] private bool isStationary = true;
    [SerializeField] private bool isSlowing;

    public LayerMask floorLayer;

    public Camera golfCamera;

    // Camera offset values
    public Vector3 cameraOffsetPosition = new Vector3(0, 2, -2.3f); // Offset for position
    public Vector3 cameraOffsetRotation = new Vector3(35, 0, 0);     // Offset for rotation

    private Coroutine slowDownCoroutine; // Coroutine reference to manage slowing down

    public GameObject EmptyAim;

    public LineRenderer lineRenderer;

    private void Awake()
    {
        GameObject empty = GameObject.Find("EmptyAim");
        if (empty != null)
        {
            // Assign the GolfCam transform to cameraParent
            EmptyAim = empty;
        }
        // Find the GameObject named "GolfCam" in the hierarchy
        GameObject golfCam = GameObject.Find("GolfCam");
        if (golfCam != null)
        {
            // Assign the GolfCam transform to cameraParent
            cameraParent = golfCam.transform;
            golfCamera = golfCam.GetComponentInChildren<Camera>();
        }
        else
        {
            Debug.LogError("GolfCam not found in the hierarchy! Please ensure a GameObject named 'GolfCam' exists.");
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on the golf ball! Please add one.");
        }

        if (cameraParent == null)
        {
            Debug.LogError("CameraParent not assigned! Please assign it in the Inspector.");
        }
    }

    

    void Update()
    {
        IsMoving();
        if (lineRenderer != null && EmptyAim != null)
        {
            // Set position 0 to the current GameObject's world position
            lineRenderer.SetPosition(0, transform.position);

            // Calculate the direction from this GameObject to the EmptyAim
            Vector3 direction = (EmptyAim.transform.position - transform.position).normalized;

            // Calculate the distance to the EmptyAim
            float distanceToEmptyAim = Vector3.Distance(transform.position, EmptyAim.transform.position);

            // Clamp the distance to a maximum of 2 units
            float clampedDistance = Mathf.Min(distanceToEmptyAim, 2f);

            // Calculate the clamped position for the line endpoint
            Vector3 clampedPosition = transform.position + direction * clampedDistance;

            // Set position 1 to the clamped position
            lineRenderer.SetPosition(1, clampedPosition);
        }

        if (Input.GetMouseButton(1)) // Check if the right mouse button is held down
        {
            float mouseX = Input.GetAxis("Mouse X"); // Get horizontal mouse movement
            cameraOffsetRotation.y += mouseX * 5f; // Adjust the Y rotation based on mouse movement
        }


        if (golfCamera != null)
        {
            RaycastHit hit;
            Ray ray = golfCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, floorLayer))
            {
                if (EmptyAim != null)
                {
                    EmptyAim.transform.position = hit.point + new Vector3(0, 0.25f, 0); ;
                }
                else { Debug.LogWarning("no emptyfound"); }
            }
        }

        // Keep the CameraParent at the ball's position with the specified offset
        if (cameraParent != null)
        {
            cameraParent.position = transform.position;

            // Apply the rotational offset
            cameraParent.rotation = Quaternion.Euler(cameraOffsetRotation);
        }


        if (!canHit && isStationary && !isSlowing) // Threshold for "stopped"
        {
            canHit = true;
        }
        else if (!isStationary || isSlowing)
        {
            canHit = false;
        }

        if (Mathf.Abs(rb.velocity.y) > 0.02f) // Check if the Y velocity is above a small threshold
        {
            if (slowDownCoroutine != null)
            {
                StopCoroutine(slowDownCoroutine); // Stop the slowdown coroutine
                isSlowing = false;
                slowDownCoroutine = null; // Reset the coroutine reference
            }
        }
        else
        {
            if (!isSlowing && !isStationary)
            {
                if (slowDownCoroutine != null)
                {
                    StopCoroutine(slowDownCoroutine);
                }

                // Introduce a delay before starting the slowdown
                slowDownCoroutine = StartCoroutine(SlowDownOverTime(time));
            }
        }
        
    }

    public void ApplyForce(float forceMultiplier)
    {
        if (EmptyAim == null)
        {
            Debug.LogError("EmptyAim is not assigned! Please assign it in the Inspector.");
            return;
        }

        // Calculate the direction from the ball to the EmptyAim position
        Vector3 direction = (EmptyAim.transform.position - transform.position).normalized;

        // Apply force in the direction of the EmptyAim
        Vector3 force = direction * ((forceMultiplier/powerDampen));
        rb.AddForce(force, ForceMode.Impulse);

        Debug.Log($"Applied force: {force}");

        canHit = false;

        // Stop any existing slow-down coroutine and start a new one
        if (slowDownCoroutine != null)
        {
            StopCoroutine(slowDownCoroutine);
        }

        // Introduce a delay before starting the slowdown
        slowDownCoroutine = StartCoroutine(DelayedSlowDown(time));
    }

    IEnumerator DelayedSlowDown(float duration)
    {
        // Allow the ball to move freely for a short period (e.g., 0.2 seconds)
        yield return new WaitForSeconds(0.2f);

        // Start the slowdown process
        StartCoroutine(SlowDownOverTime(duration));
    }

    IEnumerator SlowDownOverTime(float duration)
    {
        float elapsedTime = 0f;
        Vector3 initialVelocity = rb.velocity;

        while (elapsedTime < duration)
        {
            isSlowing = true;
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration; // Normalize time to [0, 1]

            // Only lerp the horizontal velocity (X and Z components)
            Vector3 horizontalVelocity = new Vector3(initialVelocity.x, 0, initialVelocity.z);
            Vector3 targetHorizontalVelocity = Vector3.zero;
            Vector3 lerpedHorizontalVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, t);

            // Preserve the Y component of the velocity
            rb.velocity = new Vector3(lerpedHorizontalVelocity.x, rb.velocity.y, lerpedHorizontalVelocity.z);

            yield return null;
        }

        // Ensure the horizontal velocity comes to a complete stop
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
        rb.angularVelocity = Vector3.zero;
        isSlowing = false;
    }

    void IsMoving()
    {
        // Ensure the GameObject has a Rigidbody component
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody not found on this GameObject!");
                return;
            }
        }

        // Define a small threshold to account for floating-point inaccuracies
        float velocityThreshold = 0.01f;

        // Check if the object is moving by comparing the velocity magnitude to the threshold
        if (rb.velocity.magnitude > velocityThreshold)
        {
            Debug.Log("Object is in motion.");
            isStationary = false;
        }
        else
        {
            Debug.Log("Object is stationary.");
            isStationary = true;
        }
    }
}