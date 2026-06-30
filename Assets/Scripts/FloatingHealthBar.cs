using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingHealthBar : MonoBehaviour
{
    private HealthAndRagdoll targetCharacter;
    private UnityEngine.UI.Slider healthSlider;
    private UnityEngine.UI.Image fillImage;

    [Header("Layout Configurations")]
    public float upwardPixelOffset = 1.6f;
    [Tooltip("Enforces a clean compact size so the slider bar does not stretch across your screen.")]
    public Vector2 barSize = new Vector2(120f, 16f);

    public void InitializeTarget(HealthAndRagdoll character)
    {
        targetCharacter = character;
        healthSlider = GetComponent<UnityEngine.UI.Slider>();

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;

            if (healthSlider.fillRect != null)
            {
                fillImage = healthSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
            }

            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = barSize;
            }
        }
    }

    void LateUpdate()
    {
        if (targetCharacter == null || healthSlider == null) return;

        float hp = targetCharacter.currentHealth;
        float maxHp = targetCharacter.maxHealth;

        healthSlider.value = hp / maxHp;

        if (fillImage != null)
        {
            if (hp >= 65f)
            {
                fillImage.color = Color.green;
            }
            else if (hp < 65f && hp >= 30f)
            {
                fillImage.color = Color.yellow;
            }
            else if (hp < 30f)
            {
                fillImage.color = Color.red;
            }
        }

        Vector3 worldPoint = targetCharacter.transform.position + (Vector3.up * upwardPixelOffset);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        // FIXED LIFECYCLE TRAP: Instead of turning off the GameObject (which freezes this script), 
        // we hide it by casting its coordinates far off-screen out of the player's view!
        if (screenPoint.z < 0)
        {
            transform.position = new Vector3(-10000f, -10000f, 0f);
            return;
        }

        // Snap back to normal screen position tracking when the character is in view or respawns
        transform.position = screenPoint;
    }
}