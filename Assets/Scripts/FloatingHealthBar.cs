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
            // OVERRIDE VALUES: Force the slider to normalize between 0.0 and 1.0
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;

            if (healthSlider.fillRect != null)
            {
                fillImage = healthSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
            }

            // Lock UI anchors to center bounds to stop the layout from stretching
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

        // Feed the slider a clean fraction ratio value
        healthSlider.value = hp / maxHp;

        // AUTOMATIC COLOR SYSTEM (Green -> Yellow -> Red)
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

        // Keep it positioned perfectly above the character's head
        Vector3 worldPoint = targetCharacter.transform.position + (Vector3.up * upwardPixelOffset);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldPoint);

        if (screenPoint.z < 0)
        {
            healthSlider.gameObject.SetActive(false);
            return;
        }

        healthSlider.gameObject.SetActive(true);
        transform.position = screenPoint;
    }
}