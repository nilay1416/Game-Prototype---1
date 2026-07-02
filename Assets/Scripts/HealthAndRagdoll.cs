using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthAndRagdoll : MonoBehaviour
{
    [Header("Percentage Life Settings")]
    public float maxHealth = 100f; //[cite: 10]
    public float currentHealth { get; private set; } //[cite: 10]
    public float respawnDelay = 3f; //[cite: 10]
    public float fallThreshold = -10f; //[cite: 10]

    [Header("Automated UI Link")]
    public GameObject healthBarPrefab; //[cite: 10]

    [Header("Stumble Settings")]
    public float baseStumbleDuration = 2f; //[cite: 10]

    [Header("Dynamic Editor Knockback Multipliers")]
    public float greenTierMultiplier = 0.35f; //[cite: 10]
    public float yellowTierMultiplier = 2.2f; //[cite: 10]
    public float redTierMultiplier = 8.5f; //[cite: 10]

    [Header("Out-of-Combat Regeneration Settings")]
    public float regenDelay = 5f; //[cite: 10]
    public float regenAmount = 10f; //[cite: 10]

    private Rigidbody rb; //[cite: 10]
    private ThirdPersonController playerMove; //[cite: 10]
    private EnemyAI enemyAI; //[cite: 10]
    private bool isRagdolled = false; //[cite: 10]
    private float timeSinceLastDamage; //[cite: 10]
    private Vector3 nativeSpawnPoint; //[cite: 10]

    public bool isGrounded => playerMove != null ? playerMove.isGrounded : (enemyAI != null ? enemyAI.isGrounded : true); //[cite: 10]

    void Start()
    {
        rb = GetComponent<Rigidbody>(); //[cite: 10]
        playerMove = GetComponent<ThirdPersonController>(); //[cite: 10]
        enemyAI = GetComponent<EnemyAI>(); //[cite: 10]
        currentHealth = maxHealth; //[cite: 10]

        timeSinceLastDamage = regenDelay; //[cite: 10]
        nativeSpawnPoint = transform.position; //[cite: 10]

        if (gameObject.CompareTag("Player")) //[cite: 10]
        {
            CheckpointManager.InitializeInitialSpawn(transform.position); //[cite: 10]
        }

        // --- FIXED ROOT CANVAS TARGETING SYSTEM ---
        if (healthBarPrefab != null) //[cite: 10]
        {
            Canvas mainCanvas = null;
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();

            foreach (Canvas canvas in allCanvases)
            {
                // Filters out panel components by ensuring we look for the true base root canvas layout
                if (canvas.isRootCanvas && canvas.gameObject.name.ToLower().Contains("canvas"))
                {
                    mainCanvas = canvas;
                    break;
                }
            }

            // Safety fallback condition if your scene canvas object uses a custom unique name
            if (mainCanvas == null)
            {
                foreach (Canvas canvas in allCanvases)
                {
                    if (canvas.isRootCanvas)
                    {
                        mainCanvas = canvas;
                        break;
                    }
                }
            }

            // Spawn the overlay asset safely into the validated container layout tree
            if (mainCanvas != null)
            {
                GameObject barGo = Instantiate(healthBarPrefab, mainCanvas.transform); //[cite: 10]
                FloatingHealthBar barScript = barGo.GetComponent<FloatingHealthBar>(); //[cite: 10]
                if (barScript != null) //[cite: 10]
                {
                    barScript.InitializeTarget(this); //[cite: 10]
                }
            }
            else
            {
                Debug.LogError($"[HealthAndRagdoll] {gameObject.name} could not detect a valid Root Canvas in the scene hierarchy to attach the UI Healthbar!");
            }
        }
    }

    void Update()
    {
        if (transform.position.y < fallThreshold && !isRagdolled) //[cite: 10]
        {
            StartCoroutine(RespawnSequence()); //[cite: 10]
        }

        if (currentHealth > 0f && currentHealth < maxHealth) //[cite: 10]
        {
            timeSinceLastDamage += Time.deltaTime; //[cite: 10]

            if (timeSinceLastDamage >= regenDelay) //[cite: 10]
            {
                currentHealth = Mathf.Min(maxHealth, currentHealth + (regenAmount * Time.deltaTime)); // Smooth out-of-combat heal[cite: 10]
            }
        }
    }

    public void TakeDamage(float damageAmount, Vector3 baseKnockbackForce)
    {
        if (isRagdolled && currentHealth <= 0) return; //[cite: 10]

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount); //[cite: 10]
        timeSinceLastDamage = 0f; //[cite: 10]

        float knockbackMultiplier = 1f; //[cite: 10]
        if (currentHealth >= 65f) knockbackMultiplier = greenTierMultiplier; //[cite: 10]
        else if (currentHealth < 65f && currentHealth >= 30f) knockbackMultiplier = yellowTierMultiplier; //[cite: 10]
        else if (currentHealth < 30f) knockbackMultiplier = redTierMultiplier; //[cite: 10]

        TriggerRagdoll(baseKnockbackForce * knockbackMultiplier); //[cite: 10]

        if (currentHealth <= 0f) StartCoroutine(RespawnSequence()); //[cite: 10]
        else StartCoroutine(RecoverFromStumble()); //[cite: 10]
    }

    void TriggerRagdoll(Vector3 force)
    {
        isRagdolled = true; //[cite: 10]
        if (playerMove != null) playerMove.enabled = false; //[cite: 10]
        if (enemyAI != null) enemyAI.SetControllable(false); //[cite: 10]

        rb.constraints = RigidbodyConstraints.None; //[cite: 10]
        rb.AddForce(force, ForceMode.Impulse); //[cite: 10]
        rb.AddTorque(Random.insideUnitSphere * force.magnitude, ForceMode.Impulse); //[cite: 10]
    }

    IEnumerator RecoverFromStumble()
    {
        yield return new WaitForSeconds(baseStumbleDuration); //[cite: 10]

        if (currentHealth > 0f) //[cite: 10]
        {
            transform.rotation = Quaternion.identity; //[cite: 10]
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; //[cite: 10]
            rb.velocity = Vector3.zero; //[cite: 10]
            rb.angularVelocity = Vector3.zero; //[cite: 10]

            if (playerMove != null) playerMove.enabled = true; //[cite: 10]
            if (enemyAI != null) enemyAI.SetControllable(true); //[cite: 10]
            isRagdolled = false; //[cite: 10]
        }
    }

    IEnumerator RespawnSequence()
    {
        isRagdolled = true; //[cite: 10]
        if (playerMove != null) playerMove.enabled = false; //[cite: 10]
        if (enemyAI != null) enemyAI.SetControllable(false); //[cite: 10]

        yield return new WaitForSeconds(respawnDelay); //[cite: 10]

        Vector3 respawnTargetPosition = nativeSpawnPoint; //[cite: 10]
        if (gameObject.CompareTag("Player")) //[cite: 10]
        {
            respawnTargetPosition = CheckpointManager.GetActiveRespawnPoint(); //[cite: 10]
        }

        transform.position = respawnTargetPosition; //[cite: 10]
        transform.rotation = Quaternion.identity; //[cite: 10]
        rb.velocity = Vector3.zero; //[cite: 10]
        rb.angularVelocity = Vector3.zero; //[cite: 10]

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; //[cite: 10]
        currentHealth = maxHealth; //[cite: 10]
        isRagdolled = false; //[cite: 10]

        if (playerMove != null) playerMove.enabled = true; //[cite: 10]
        if (enemyAI != null) enemyAI.SetControllable(true); //[cite: 10]
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isRagdolled) return; //[cite: 10]

        if (collision.gameObject.CompareTag("TripObstacle")) //[cite: 10]
        {
            Vector3 stumblePushForce = (transform.position - collision.transform.position).normalized * 12f; //[cite: 10]
            stumblePushForce.y = 4f; //[cite: 10]
            TriggerRagdoll(stumblePushForce); //[cite: 10]
            StartCoroutine(RecoverFromStumble()); //[cite: 10]
        }

        if (collision.gameObject.CompareTag("DamagingTrap")) //[cite: 10]
        {
            Vector3 trapForce = (transform.position - collision.transform.position).normalized * 10f; //[cite: 10]
            trapForce.y = 3.5f; //[cite: 10]
            TakeDamage(20f, trapForce); //[cite: 10]
        }
    }
}