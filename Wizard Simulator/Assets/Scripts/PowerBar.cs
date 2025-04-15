using UnityEngine;
using UnityEngine.UI;

public class PowerBar : MonoBehaviour
{
    public Image fillImage; // Reference to the UI Image component
    private float powerValue = 0f;
    private bool increasing = true;

    void Update()
    {
        if (Input.GetMouseButton(0)) // Mouse 1 held down
        {
            // Increment or decrement the power value
            if (increasing)
            {
                powerValue += Time.deltaTime * 100f; // Scale to reach 100 in 1 second
                if (powerValue >= 100f)
                {
                    powerValue = 100f;
                    increasing = false;
                }
            }
            else
            {
                powerValue -= Time.deltaTime * 100f;
                if (powerValue <= 0f)
                {
                    powerValue = 0f;
                    increasing = true;
                }
            }

            // Update the fill amount of the UI Image
            if (fillImage != null)
            {
                fillImage.fillAmount = powerValue / 100f;
            }
        }

        if (Input.GetMouseButtonUp(0)) // Mouse 1 released
        {
            Debug.Log($"Power applied: {powerValue}");
            // Pass the power value to the golf ball controller
            GolfBallController golfBallController = GetComponent<GolfBallController>();
            if (golfBallController != null)
            {
                //golfBallController.ApplyForce(powerValue);
            }

            // Reset the power bar
            powerValue = 0f;
            fillImage.fillAmount = 0f;
        }
    }
}