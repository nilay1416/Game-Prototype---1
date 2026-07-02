using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Spawn & Respawn Settings")]
    [Tooltip("If checked, this enemy will respawn normally. If unchecked, they will die permanently and will not come back.")]
    public bool respawnAfterDeath = true;

    [Header("Target Tracking & Senses")]
    public Transform playerTarget; //[cite: 13]
    [Tooltip("How close the player must get to alert the AI for the first time.")]
    public float detectionRange = 25f; //[cite: 13]
    [Tooltip("The sweet-spot distance the AI tries to maintain to shoot from afar without getting too close.")]
    public float maintainDistance = 10f; //[cite: 13]
    [Tooltip("Matches the player's top movement speed.")]
    public float moveSpeed = 14f; //[cite: 13]
    [Tooltip("How fast the AI turns to face its movement vector.")]
    public float rotationSpeed = 15f; //[cite: 13]
    public float shootingRange = 15f; //[cite: 13]
    public float attackRate = 3f; //[cite: 13]

    [Header("Snappy Jump Settings")]
    public float jumpForce = 14f; //[cite: 13]
    [Tooltip("Brings the AI down to earth quickly, matching player gravity properties.")]
    public float fallMultiplier = 3.5f; //[cite: 13]
    public float groundCheckDistance = 1.1f; //[cite: 13]
    public LayerMask groundLayer; //[cite: 13]

    [Header("AI Jump Navigation")]
    [Tooltip("Point near the shins/knees to check for walls or hurdles.")]
    public Transform obstacleCheckPoint; //[cite: 13]
    [Tooltip("How far forward the AI checks for walls on the Ground layer to jump.")]
    public float obstacleCheckDistance = 1.2f; //[cite: 13]

    [Header("Distance-Based Dash")]
    public bool enableAIDashing = true; //[cite: 13]
    public float dashDistance = 8f; //[cite: 13]
    public float dashDuration = 0.15f; //[cite: 13]
    public float dashCooldown = 2.5f; //[cite: 13]

    [Header("Tracking Status Tracking (Read Only)")]
    public bool isChasing = false; //[cite: 13]
    public bool isGrounded; //[cite: 13]

    private Rigidbody rb; //[cite: 13]
    private RocketLauncher launcher; //[cite: 13]
    private HealthAndRagdoll playerHealth; //[cite: 13]
    private HealthAndRagdoll myOwnHealth; // Internal reference to watch its own vital states
    private Vector3 moveDirection; //[cite: 13]

    private bool canDash = true; //[cite: 13]
    private bool isDashing = false; //[cite: 13]
    private bool aiActive = true; //[cite: 13]
    private float attackTimer; //[cite: 13]

    void Start()
    {
        rb = GetComponent<Rigidbody>(); //[cite: 13]
        launcher = GetComponent<RocketLauncher>(); //[cite: 13]
        myOwnHealth = GetComponent<HealthAndRagdoll>(); // Grab own health manager component

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; //[cite: 13]

        if (playerTarget == null) //[cite: 13]
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player"); //[cite: 13]
            if (player != null) playerTarget = player.transform; //[cite: 13]
        }

        if (playerTarget != null) //[cite: 13]
        {
            playerHealth = playerTarget.GetComponent<HealthAndRagdoll>(); //[cite: 13]
        }
    }

    void Update()
    {
        // --- PERMANENT DEATH CHECK ---
        // If the user turned off respawning for this enemy, and they are out of health, 
        // completely destroy the GameObject before the other script's respawn timer triggers!
        if (!respawnAfterDeath && myOwnHealth != null && myOwnHealth.currentHealth <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!aiActive || playerTarget == null) return; //[cite: 13]

        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer); //[cite: 13]

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position); //[cite: 13]

        if (playerHealth != null && playerHealth.currentHealth <= 0f) //[cite: 13]
        {
            isChasing = false; //[cite: 13]
        }

        if (!isChasing) //[cite: 13]
        {
            if (distanceToPlayer <= detectionRange && (playerHealth == null || playerHealth.currentHealth > 0f)) //[cite: 13]
            {
                isChasing = true; //[cite: 13]
            }
        }

        if (isChasing) //[cite: 13]
        {
            attackTimer += Time.deltaTime; //[cite: 13]

            if (distanceToPlayer <= shootingRange && attackTimer >= attackRate && !isDashing) //[cite: 13]
            {
                if (launcher != null) //[cite: 13]
                {
                    launcher.TryFire(playerTarget); //[cite: 13]
                }
                attackTimer = 0f; //[cite: 13]
            }

            if (enableAIDashing && canDash && !isDashing && distanceToPlayer > shootingRange) //[cite: 13]
            {
                StartCoroutine(PerformAIDistanceDash()); //[cite: 13]
            }
        }

        if (isGrounded && obstacleCheckPoint != null && isChasing) //[cite: 13]
        {
            if (Physics.Raycast(obstacleCheckPoint.position, transform.forward, obstacleCheckDistance, groundLayer)) //[cite: 13]
            {
                Jump(); //[cite: 13]
            }
        }
    }

    void FixedUpdate()
    {
        if (!aiActive || playerTarget == null || isDashing) return; //[cite: 13]

        Vector3 targetVelocity = Vector3.zero; //[cite: 13]

        if (isChasing) //[cite: 13]
        {
            moveDirection = (playerTarget.position - transform.position); //[cite: 13]
            moveDirection.y = 0; //[cite: 13]
            moveDirection.Normalize(); //[cite: 13]

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position); //[cite: 13]

            if (distanceToPlayer > maintainDistance) //[cite: 13]
            {
                targetVelocity = moveDirection * moveSpeed; //[cite: 13]
            }
            else if (distanceToPlayer < maintainDistance * 0.75f) //[cite: 13]
            {
                targetVelocity = -moveDirection * (moveSpeed * 0.6f); //[cite: 13]
            }
            else //[cite: 13]
            {
                targetVelocity = Vector3.zero; //[cite: 13]
            }

            if (moveDirection != Vector3.zero) //[cite: 13]
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection); //[cite: 13]
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime); //[cite: 13]
            }
        }

        Vector3 currentVelocity = rb.velocity; //[cite: 13]
        Vector3 velocityChange = targetVelocity - currentVelocity; //[cite: 13]
        velocityChange.y = 0; //[cite: 13]

        rb.AddForce(velocityChange, ForceMode.VelocityChange); //[cite: 13]

        if (rb.velocity.y < 0) //[cite: 13]
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime; //[cite: 13]
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); //[cite: 13]
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); //[cite: 13]
    }

    IEnumerator PerformAIDistanceDash()
    {
        canDash = false; //[cite: 13]

        Vector3 dashHeading = moveDirection != Vector3.zero ? moveDirection : transform.forward; //[cite: 13]
        dashHeading.y = 0; //[cite: 13]
        dashHeading.Normalize(); //[cite: 13]

        float dashVelocity = dashDistance / dashDuration; //[cite: 13]
        float elapsedTime = 0f; //[cite: 13]

        while (elapsedTime < dashDuration) //[cite: 13]
        {
            isDashing = true; //[cite: 13]
            rb.velocity = dashHeading * dashVelocity; //[cite: 13]
            elapsedTime += Time.deltaTime; //[cite: 13]
            yield return null; //[cite: 13]
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f); //[cite: 13]
        isDashing = false; //[cite: 13]

        yield return new WaitForSeconds(dashCooldown); //[cite: 13]
        canDash = true; //[cite: 13]
    }

    public void SetControllable(bool state)
    {
        aiActive = state; //[cite: 13]
        if (!state) isDashing = false; //[cite: 13]
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green; //[cite: 13]
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance); //[cite: 13]

        if (obstacleCheckPoint != null) //[cite: 13]
        {
            Gizmos.color = Color.blue; //[cite: 13]
            Gizmos.DrawLine(obstacleCheckPoint.position, obstacleCheckPoint.position + transform.forward * obstacleCheckDistance); //[cite: 13]
        }

        Gizmos.color = Color.cyan; //[cite: 13]
        Gizmos.DrawWireSphere(transform.position, detectionRange); //[cite: 13]

        Gizmos.color = Color.red; //[cite: 13]
        Gizmos.DrawWireSphere(transform.position, maintainDistance); //[cite: 13]
    }
}