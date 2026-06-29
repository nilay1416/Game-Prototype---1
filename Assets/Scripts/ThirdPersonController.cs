using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("3rd Person Movement")]
    public float moveSpeed = 14f;
    public float rotationSpeed = 15f;

    [Header("Snappy Jump Settings")]
    public float jumpForce = 14f;
    public float fallMultiplier = 3.5f;
    public float groundCheckRadius = 0.35f;
    public LayerMask groundLayer;

    [Header("Distance-Based Dash")]
    public float dashDistance = 8f;
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

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        if (isDead) return;

        Vector3 spherePosition = transform.position + Vector3.down * 0.85f;
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer);

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (mainCameraTransform != null)
        {
            Vector3 camForward = mainCameraTransform.forward;
            Vector3 camRight = mainCameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveInput = (camForward * moveZ + camRight * moveX).normalized;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            StartCoroutine(PerformDistanceDash());
        }
    }

    void FixedUpdate()
    {
        if (isDead || isDashing) return;

        // --- GLITCH FIX: COMPLETE ROTATIONAL ANCHOR LOCK ---
        // Forcefully wipes out any spinning torque physics glitches gathered from bridges/platforms
        rb.angularVelocity = Vector3.zero;

        // Locks your character to a strict upright vertical alignment, stopping random tilting loops completely
        transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
        // ----------------------------------------------------

        Vector3 targetVelocity = moveInput * moveSpeed;
        Vector3 currentVelocity = rb.velocity;
        Vector3 velocityChange = targetVelocity - currentVelocity;

        velocityChange.y = 0;
        rb.AddForce(velocityChange, ForceMode.VelocityChange);

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }

        if (moveInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    IEnumerator PerformDistanceDash()
    {
        canDash = false;
        Vector3 dashDirection = moveInput != Vector3.zero ? moveInput : transform.forward;
        dashDirection.y = 0;
        dashDirection.Normalize();

        float dashVelocity = dashDistance / dashDuration;
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            isDashing = true;
            rb.velocity = dashDirection * dashVelocity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 spherePosition = transform.position + Vector3.down * 0.85f;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}