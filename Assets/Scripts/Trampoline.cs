using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Tooltip("The vertical velocity force applied to the character controller.")]
    public float bounceForce = 22f;

    // Use Trigger execution since CharacterControllers don't trip OnCollisionEnter
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ThirdPersonController playerController = other.GetComponent<ThirdPersonController>();

            if (playerController != null)
            {
                // Call our new upward launch hook inside the player script
                playerController.LaunchUpward(bounceForce);
            }
        }
    }
}