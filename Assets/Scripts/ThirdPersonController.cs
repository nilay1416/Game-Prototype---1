using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("3rd Person Movement")]
    [Tooltip("How fast the capsule moves horizontally.")]
    public float moveSpeed = 14f;
    [Tooltip("How fast the player rotates to face the camera's direction.")]
    public float rotationSpeed = 15f;

    [Header("Snappy Jump Settings")]
    public float jumpForce = 14f;
    [Tooltip("Multiplier applied to gravity when falling. Higher = faster, heavier landings.")]
    public float fallMultiplier = 3.5f;
    [Tooltip("The radius of the ground check sphere at the player's feet.")]
    public float groundCheckRadius = 0.35f; // Replaced distance with radius for the sphere check
    public LayerMask groundLayer;

    [Header("Distance-Based Dash")]
    [Tooltip("The exact distance in meters the dash should cover.")]
    public float dashDistance = 8f;
    [Tooltip("How fast the dash completes (in seconds). Lower numbers = near instant blink/teleport.")]
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.2f;

    private Rigidbody rb;
    private Transform mainCameraTransform;
    private Vector3 moveInput;

    private bool canDash = true;
    private bool isDashing = false;
    private bool isDead = false;

    [Header("Debug Status (Read Only)")]
    public bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Grab the main camera transform dynamically for relative tracking
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Missing Main Camera in the scene! RPG movement requires a camera reference.");
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (isDead) return;

        // 1. UPGRADED GROUND CHECK (Bulletproof Sphere Check at the feet)
        // Positioned slightly off the bottom (-0.85f) to create a perfect detection bubble
        Vector3 spherePosition = transform.position + Vector3.down * 0.85f;
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);

        // 2. Camera-Relative Input Calculation
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (mainCameraTransform != null)
        {
            // Get camera directional vectors flattened on the Y axis
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Calculate directional input based on where the camera faces
            moveInput = (camForward * moveZ + camRight * moveX).normalized;
        }

        // 3. Jump Input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        // 4. Dash Input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(PerformDistanceDash());
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // If dashing, the dash coroutine handles absolute positioning; bypass normal movement physics
        if (isDashing) return;

        // Snap Movement Physics
        Vector3 targetVelocity = moveInput * moveSpeed;
        Vector3 currentVelocity = rb.velocity;
        Vector3 velocityChange = targetVelocity - currentVelocity;

        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        // Snappy Fall Logic: Modifies gravity pulling down *only* when falling or coming down from a jump
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        // Keep character rotated towards the camera's forward alignment when moving
        if (moveInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void Jump()
    {
        // Keeps original logic structure intact but resets Y tracking for a clean upward pop
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    IEnumerator PerformDistanceDash()
    {
        canDash = false;
        isDashing = false; // Turned off regular fixed movement updates during dash windows

        // Determine the absolute heading direction
        Vector3 dashDirection = moveInput != Vector3.zero ? moveInput : transform.forward;
        dashDirection.y = 0; // Prevent dashing up into the air or down into floor geometry
        dashDirection.Normalize();

        // Calculate velocity required to cover EXACT distance over EXACT duration ($Speed = Distance / Time$)
        float dashVelocity = dashDistance / dashDuration;

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            isDashing = true;
            // Lock horizontal velocity completely to the calculated mathematical dash target
            rb.velocity = dashDirection * dashVelocity;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Snappy stop: Kill momentum immediately at the finish line
        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        isDashing = false;

        // Global cooldown clock execution
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // Draws a green sphere in your scene view so you can visually size your ground check
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 spherePosition = transform.position + Vector3.down * 0.85f;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}