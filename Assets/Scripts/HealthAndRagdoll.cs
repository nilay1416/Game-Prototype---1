using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthAndRagdoll : MonoBehaviour
{
    [Header("Percentage Life Settings")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    public float respawnDelay = 3f;
    public float fallThreshold = -10f;

    [Header("Automated UI Link")]
    [Tooltip("Drag your FloatingHealthBar UI Prefab asset here.")]
    public GameObject healthBarPrefab;

    [Header("Stumble Settings")]
    public float baseStumbleDuration = 2f;

    [Header("Dynamic Editor Knockback Multipliers")]
    [Tooltip("Knockback force multiplier when health is Green (100% to 65%). Default: 0.35")]
    public float greenTierKnockbackMultiplier = 0.35f;
    [Tooltip("Knockback force multiplier when health is Yellow (64% to 30%). Default: 2.2")]
    public float yellowTierKnockbackMultiplier = 2.2f;
    [Tooltip("Knockback force multiplier when health is Red / Baseball Mode (Below 30%). Default: 8.5")]
    public float redTierKnockbackMultiplier = 8.5f;

    private Rigidbody rb;
    private ThirdPersonController playerMove;
    private EnemyAI enemyAI;
    private Vector3 spawnPoint;
    private bool isRagdolled = false;

    public bool isGrounded => playerMove != null ? playerMove.isGrounded : (enemyAI != null ? enemyAI.isGrounded : true);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMove = GetComponent<ThirdPersonController>();
        enemyAI = GetComponent<EnemyAI>();
        spawnPoint = transform.position;
        currentHealth = maxHealth;

        if (healthBarPrefab != null)
        {
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas != null)
            {
                GameObject barGo = Instantiate(healthBarPrefab, mainCanvas.transform);
                FloatingHealthBar barScript = barGo.GetComponent<FloatingHealthBar>();
                if (barScript != null)
                {
                    barScript.InitializeTarget(this);
                }
            }
        }
    }

    void Update()
    {
        if (transform.position.y < fallThreshold && !isRagdolled)
        {
            StartCoroutine(RespawnSequence());
        }
    }

    public void TakeDamage(float damageAmount, Vector3 baseKnockbackForce)
    {
        if (isRagdolled && currentHealth <= 0) return;

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount);

        // STUMBLE & KNOCKBACK INTENSITY MODIFIERS READ DIRECTLY FROM THE EDITOR
        float knockbackMultiplier = 1f;

        if (currentHealth >= 65f)
        {
            knockbackMultiplier = greenTierKnockbackMultiplier;
        }
        else if (currentHealth < 65f && currentHealth >= 30f)
        {
            knockbackMultiplier = yellowTierKnockbackMultiplier;
        }
        else if (currentHealth < 30f)
        {
            knockbackMultiplier = redTierKnockbackMultiplier;
        }

        TriggerRagdoll(baseKnockbackForce * knockbackMultiplier);

        if (currentHealth <= 0f)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            StartCoroutine(RecoverFromStumble());
        }
    }

    void TriggerRagdoll(Vector3 force)
    {
        isRagdolled = true;

        if (playerMove != null) playerMove.enabled = false;
        if (enemyAI != null) enemyAI.SetControllable(false);

        rb.constraints = RigidbodyConstraints.None;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * force.magnitude, ForceMode.Impulse);
    }

    IEnumerator RecoverFromStumble()
    {
        yield return new WaitForSeconds(baseStumbleDuration);

        if (currentHealth > 0f)
        {
            transform.rotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (playerMove != null) playerMove.enabled = true;
            if (enemyAI != null) enemyAI.SetControllable(true);
            isRagdolled = false;
        }
    }

    IEnumerator RespawnSequence()
    {
        isRagdolled = true;
        if (playerMove != null) playerMove.enabled = false;
        if (enemyAI != null) enemyAI.SetControllable(false);

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPoint;
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        currentHealth = maxHealth;
        isRagdolled = false;

        if (playerMove != null) playerMove.enabled = true;
        if (enemyAI != null) enemyAI.SetControllable(true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isRagdolled) return;

        if (collision.gameObject.CompareTag("TripObstacle"))
        {
            Vector3 stumblePushForce = (transform.position - collision.transform.position).normalized * 12f;
            stumblePushForce.y = 4f;
            TriggerRagdoll(stumblePushForce);
            StartCoroutine(RecoverFromStumble());
        }

        if (collision.gameObject.CompareTag("DamagingTrap"))
        {
            Vector3 trapForce = (transform.position - collision.transform.position).normalized * 10f;
            trapForce.y = 3.5f;
            TakeDamage(20f, trapForce);
        }
    }
}