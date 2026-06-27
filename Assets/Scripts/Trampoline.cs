using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [Header("Bouncing Settings")]
    public float launchForce = 20f;

    [Header("Visual Juice (Optional)")]
    public float bounceSquishDuration = 0.1f;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Get the Rigidbody of whatever landed on the trampoline (Player or Enemy)
        Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // 1. Snappy Physics Trick:
            // Zero out the incoming vertical velocity so the player doesn't absorb the bounce.
            // This ensures they always fly up to the exact same height regardless of how far they fell.
            Vector3 currentVel = rb.velocity;
            rb.velocity = new Vector3(currentVel.x, 0f, currentVel.z);

            // 2. Launch the object straight up using an Impulse force
            rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);

            // 3. Trigger a quick visual bounce effect
            StopAllCoroutines();
            StartCoroutine(VisualBounceEffect());
        }
    }

    private System.Collections.IEnumerator VisualBounceEffect()
    {
        // Quickly squish the trampoline down
        Vector3 squishedScale = new Vector3(originalScale.x * 1.2f, originalScale.y * 0.4f, originalScale.z * 1.2f);
        float elapsed = 0f;

        while (elapsed < bounceSquishDuration)
        {
            transform.localScale = Vector3.Lerp(originalScale, squishedScale, elapsed / bounceSquishDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Snap back to the original size smoothly
        elapsed = 0f;
        while (elapsed < bounceSquishDuration * 2f)
        {
            transform.localScale = Vector3.Lerp(squishedScale, originalScale, elapsed / (bounceSquishDuration * 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}