using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target Tracking & Senses")]
    public Transform playerTarget;
    [Tooltip("How close the player must get to alert the AI for the first time.")]
    public float detectionRange = 25f;
    [Tooltip("The sweet-spot distance the AI tries to maintain to shoot from afar without getting too close.")]
    public float maintainDistance = 10f;
    [Tooltip("Matches the player's top movement speed.")]
    public float moveSpeed = 14f;
    [Tooltip("How fast the AI turns to face its movement vector.")]
    public float rotationSpeed = 15f;
    public float shootingRange = 15f;
    public float attackRate = 3f;

    [Header("Snappy Jump Settings")]
    public float jumpForce = 14f;
    [Tooltip("Brings the AI down to earth quickly, matching player gravity properties.")]
    public float fallMultiplier = 3.5f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    [Header("AI Jump Navigation")]
    [Tooltip("Point near the shins/knees to check for walls or hurdles.")]
    public Transform obstacleCheckPoint;
    [Tooltip("How far forward the AI checks for walls on the Ground layer to jump.")]
    public float obstacleCheckDistance = 1.2f;

    [Header("Distance-Based Dash")]
    public bool enableAIDashing = true;
    public float dashDistance = 8f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 2.5f;

    [Header("Tracking Status Tracking (Read Only)")]
    public bool isChasing = false;
    public bool isGrounded;

    private Rigidbody rb;
    private RocketLauncher launcher;
    private HealthAndRagdoll playerHealth;
    private Vector3 moveDirection;

    private bool canDash = true;
    private bool isDashing = false;
    private bool aiActive = true;
    private float attackTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        launcher = GetComponent<RocketLauncher>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTarget = player.transform;
        }

        // Cache the player's health reference to monitor death states cleanly
        if (playerTarget != null)
        {
            playerHealth = playerTarget.GetComponent<HealthAndRagdoll>();
        }
    }

    void Update()
    {
        if (!aiActive || playerTarget == null) return;

        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // --- RESET AGGRO SYSTEM UPON PLAYER DEATH ---
        if (playerHealth != null && playerHealth.currentHealth <= 0f)
        {
            isChasing = false;
        }

        // --- SENSORY DETECTION CHECK ---
        if (!isChasing)
        {
            // Only trigger chase state if player is within range and alive
            if (distanceToPlayer <= detectionRange && (playerHealth == null || playerHealth.currentHealth > 0f))
            {
                isChasing = true;
            }
        }

        // Only fire weapons if AI has actively spotted the player
        if (isChasing)
        {
            attackTimer += Time.deltaTime;

            if (distanceToPlayer <= shootingRange && attackTimer >= attackRate && !isDashing)
            {
                if (launcher != null)
                {
                    launcher.TryFire(playerTarget);
                }
                attackTimer = 0f;
            }

            if (enableAIDashing && canDash && !isDashing && distanceToPlayer > shootingRange)
            {
                StartCoroutine(PerformAIDistanceDash());
            }
        }

        if (isGrounded && obstacleCheckPoint != null && isChasing)
        {
            if (Physics.Raycast(obstacleCheckPoint.position, transform.forward, obstacleCheckDistance, groundLayer))
            {
                Jump();
            }
        }
    }

    void FixedUpdate()
    {
        if (!aiActive || playerTarget == null || isDashing) return;

        Vector3 targetVelocity = Vector3.zero;

        // Only execute pathfinding mechanics if aggro lock is active
        if (isChasing)
        {
            moveDirection = (playerTarget.position - transform.position);
            moveDirection.y = 0;
            moveDirection.Normalize();

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // --- RANGED COMBAT MOVEMENT SWEET-SPOT CONTROL ---
            if (distanceToPlayer > maintainDistance)
            {
                // Player is far away; advance forward at normal pacing
                targetVelocity = moveDirection * moveSpeed;
            }
            else if (distanceToPlayer < maintainDistance * 0.75f)
            {
                // Player is crowding the AI; back up smoothly to keep distance
                targetVelocity = -moveDirection * (moveSpeed * 0.6f);
            }
            else
            {
                // Sweet spot reached; halt horizontal movement to stay firmly at range
                targetVelocity = Vector3.zero;
            }

            // Keep facing the target even when standing still to attack from a distance
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // Process physics locomotion forces
        Vector3 currentVelocity = rb.velocity;
        Vector3 velocityChange = targetVelocity - currentVelocity;
        velocityChange.y = 0;

        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    IEnumerator PerformAIDistanceDash()
    {
        canDash = false;

        Vector3 dashHeading = moveDirection != Vector3.zero ? moveDirection : transform.forward;
        dashHeading.y = 0;
        dashHeading.Normalize();

        float dashVelocity = dashDistance / dashDuration;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            isDashing = true;
            rb.velocity = dashHeading * dashVelocity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void SetControllable(bool state)
    {
        aiActive = state;
        if (!state) isDashing = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw standard ground lines
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        if (obstacleCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(obstacleCheckPoint.position, obstacleCheckPoint.position + transform.forward * obstacleCheckDistance);
        }

        // --- EDITOR SENSORY WIRE Visualizers ---
        // Draws a light blue circle around your AI indicating the initial spotting threshold
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draws a red circle around your AI showing the distance it tries to hold back at
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maintainDistance);
    }
}