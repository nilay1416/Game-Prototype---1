using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthAndRagdoll : MonoBehaviour
{
    [Header("Life Settings")]
    public int maxHealth = 3;
    private int currentHealth;
    public float respawnDelay = 2f;
    public float fallThreshold = -10f; // Map boundary Y level

    [Header("Stumble Settings")]
    public float stumbleDuration = 2.5f;

    private Rigidbody rb;
    private ThirdPersonController playerMove;
    private EnemyAI enemyAI;
    private Vector3 spawnPoint;
    private bool isRagdolled = false;

    // Disabled components can still have their variables read safely
    public bool isGrounded => playerMove != null ? playerMove.isGrounded : (enemyAI != null ? enemyAI.isGrounded : true);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMove = GetComponent<ThirdPersonController>();
        enemyAI = GetComponent<EnemyAI>();
        spawnPoint = transform.position;
        currentHealth = maxHealth;
    }

    void Update()
    {
        // Out of bounds check
        if (transform.position.y < fallThreshold && !isRagdolled)
        {
            StartCoroutine(RespawnSequence(true)); // Instant out-of-bounds respawn rule
        }
    }

    public void TakeDamage(int amount, Vector3 knockbackForce)
    {
        if (isRagdolled) return;

        currentHealth -= amount;
        TriggerRagdoll(knockbackForce);

        if (currentHealth <= 0)
        {
            StartCoroutine(RespawnSequence(false));
        }
        else
        {
            StartCoroutine(RecoverFromStumble());
        }
    }

    void TriggerRagdoll(Vector3 force)
    {
        isRagdolled = true;

        // Clean Fix: Disable the movement component directly to kill input instantly
        if (playerMove != null) playerMove.enabled = false;
        if (enemyAI != null) enemyAI.SetControllable(false);

        // Unfreeze rotations so the capsule turns floppy, rolls, and slides dramatically
        rb.constraints = RigidbodyConstraints.None;

        // Apply physics impact
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * force.magnitude, ForceMode.Impulse);
    }

    IEnumerator RecoverFromStumble()
    {
        yield return new WaitForSeconds(stumbleDuration);

        if (currentHealth > 0)
        {
            // Stand the capsule back up smoothly
            transform.rotation = Quaternion.identity;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.angularVelocity = Vector3.zero;

            // Re-enable components
            if (playerMove != null) playerMove.enabled = true;
            if (enemyAI != null) enemyAI.SetControllable(true);
            isRagdolled = false;
        }
    }

    IEnumerator RespawnSequence(bool fellOutOfMap)
    {
        isRagdolled = true;
        if (playerMove != null) playerMove.enabled = false;
        if (enemyAI != null) enemyAI.SetControllable(false);

        yield return new WaitForSeconds(respawnDelay);

        // Reset parameters
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
        // Handle custom tagged obstacles that cause player to trip/slide
        if (collision.gameObject.CompareTag("TripObstacle") && !isRagdolled)
        {
            // Apply a minor backward trip force based on current incoming velocity
            Vector3 tripForce = -rb.velocity * 1.5f + Vector3.up * 2f;
            TriggerRagdoll(tripForce);
            StartCoroutine(RecoverFromStumble());
        }
    }
}