using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform playerTarget;
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
    public LayerMask groundLayer; // Used for both staying grounded and checking walls ahead!

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

    private Rigidbody rb;
    private RocketLauncher launcher;
    private Vector3 moveDirection;

    private bool canDash = true;
    private bool isDashing = false;
    private bool aiActive = true;
    private float attackTimer;

    [Header("Debug Status (Read Only)")]
    public bool isGrounded;

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
    }

    void Update()
    {
        if (!aiActive || playerTarget == null) return;

        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        attackTimer += Time.deltaTime;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (distanceToPlayer <= shootingRange && attackTimer >= attackRate && !isDashing)
        {
            if (launcher != null)
            {
                launcher.TryFire(playerTarget);
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] {gameObject.name} is trying to shoot but is missing a RocketLauncher component!");
            }
            attackTimer = 0f;
        }

        if (enableAIDashing && canDash && !isDashing && distanceToPlayer > shootingRange)
        {
            StartCoroutine(PerformAIDistanceDash());
        }

        // FIXED: The forward detection raycast now scans your groundLayer instead of obstacleLayer
        if (isGrounded && obstacleCheckPoint != null)
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

        moveDirection = (playerTarget.position - transform.position);
        moveDirection.y = 0;
        moveDirection.Normalize();

        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentVelocity = rb.velocity;
        Vector3 velocityChange = targetVelocity - currentVelocity;

        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
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
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        if (obstacleCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(obstacleCheckPoint.position, obstacleCheckPoint.position + transform.forward * obstacleCheckDistance);
        }
    }
}