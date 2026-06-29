using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoAimMarker : MonoBehaviour
{
    [Header("References")]
    public RocketLauncher playerLauncher;
    public UnityEngine.UI.Image reticleImage;

    [Header("Visual Configurations")]
    public float UIFollowSpeed = 22f;
    public float targetHeightOffset = 0.5f;

    [Header("Mode Customization")]
    public Color autoAimColor = Color.white;
    public Color manualAimColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan / Light Blue
    public Vector3 manualReticleScale = new Vector3(1.4f, 1.4f, 1.4f); // Grow slightly when manual aiming

    private RectTransform reticleRect;

    void Start()
    {
        if (reticleImage != null)
        {
            reticleRect = reticleImage.GetComponent<RectTransform>();
            reticleImage.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (playerLauncher == null || reticleImage == null || reticleRect == null) return;

        if (playerLauncher.currentTarget != null)
        {
            reticleImage.enabled = true;

            // DYNAMIC COLOR & SCALE TRANSITIONS based on manual hold flags
            if (playerLauncher.isManualAimMode)
            {
                reticleImage.color = manualAimColor;
                reticleRect.localScale = Vector3.Lerp(reticleRect.localScale, manualReticleScale, 15f * Time.deltaTime);
            }
            else
            {
                reticleImage.color = autoAimColor;
                reticleRect.localScale = Vector3.Lerp(reticleRect.localScale, Vector3.one, 15f * Time.deltaTime);
            }

            // Convert world space vectors cleanly to pixel coordinates
            Vector3 targetWorldPosition = playerLauncher.currentTarget.position + (Vector3.up * targetHeightOffset);
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(targetWorldPosition);

            reticleRect.position = Vector2.Lerp(rectilePositionCalculate(), screenPosition, UIFollowSpeed * Time.deltaTime);
        }
        else
        {
            reticleImage.enabled = false;
        }
    }

    Vector2 rectilePositionCalculate()
    {
        return reticleRect.position;
    }
}