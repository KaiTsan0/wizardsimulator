using UnityEngine;
using System.Collections;

public class GolfBallController : MonoBehaviour
{
    public float forceMultiplier = 1f;
    private Rigidbody rb;
    public float time = 2f; // Duration of the slowdown
    public Transform cameraParent; // Reference to the CameraParent object
    [SerializeField] private bool canHit = true;

    // Camera offset values
    public Vector3 cameraOffsetPosition = new Vector3(0, 2, -2.3f); // Offset for position
    public Vector3 cameraOffsetRotation = new Vector3(35, 0, 0);     // Offset for rotation

    private Coroutine slowDownCoroutine; // Coroutine reference to manage slowing down

    private void Awake()
    {
        // Find the GameObject named "GolfCam" in the hierarchy
        GameObject golfCam = GameObject.Find("GolfCam");
        if (golfCam != null)
        {
            // Assign the GolfCam transform to cameraParent
            cameraParent = golfCam.transform;
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
        // Keep the CameraParent at the ball's position with the specified offset
        if (cameraParent != null)
        {
            // Apply the positional offset in world space
            cameraParent.position = transform.position + cameraOffsetPosition;

            // Apply the rotational offset
            cameraParent.rotation = Quaternion.Euler(cameraOffsetRotation);
        }

        if (Input.GetMouseButtonDown(0) && canHit)
        {
            ApplyForce();
        }

        if (!canHit && rb.velocity.magnitude < 0.1f) // Threshold for "stopped"
        {
            canHit = true;
        }
    }

    void ApplyForce()
    {
        Vector3 force = transform.forward * forceMultiplier;
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
    }
}