using UnityEngine;

public class GolfTrigger : MonoBehaviour
{
    public GameObject golfBallPrefab; // Reference to the golf ball prefab
    private bool playerInZone = false;
    private PlayerManager playerManager;

    private void Start()
    {
        playerManager = FindObjectOfType<PlayerManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && playerInZone)
        {
            playerManager.GolfModeToggle();
            SpawnGolfBall();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
        }
    }

    void SpawnGolfBall()
    {
        if (golfBallPrefab != null)
        {
            // Disable the player controller
            FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
            if (playerController != null)
            {
                //playerController.enabled = false;
            }

            // Spawn the golf ball at the trigger's position
            GameObject golfBall = Instantiate(golfBallPrefab, transform.position, Quaternion.identity);

            // Activate the golf ball's camera
            GolfBallController golfBallController = golfBall.GetComponent<GolfBallController>();
            if (golfBallController != null)
            {
                //golfBallController.ActivateCamera();
            }
        }
    }
}