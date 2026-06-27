using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7.0f;
    [SerializeField] private float rotationSpeed = 15.0f;

    [Header("Jump & Physics (Snappy)")]
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float baseGravity = -30.0f;
    [SerializeField] private float fallMultiplier = 2.0f;

    [Header("Jump Grace Period (Coyote Time)")]
    [Tooltip("How many seconds after leaving a platform you can still jump. Fixes moving platform jitters.")]
    [SerializeField] private float coyoteTimeDuration = 0.15f;
    private float _coyoteTimer;

    [Header("Slope Sliding")]
    [Tooltip("How fast the player slides down steep platforms.")]
    [SerializeField] private float slideSpeed = 12.0f;

    [Header("Knockdown & Fall Mechanics")]
    [SerializeField] private string obstacleTag = "MovingObstacle";
    [SerializeField] private float obstacleKnockbackForce = 15.0f;
    [SerializeField] private float minimumLieTime = 2.0f;

    private CharacterController _controller;
    private Transform _cameraTransform;
    private Vector3 _velocity;
    private bool _isGrounded;

    // Knockdown State Tracking
    private bool _isKnockedDown = false;
    private bool _isStandingUp = false;
    private Vector3 _knockbackVelocity;
    private float _lieTimer = 0.0f;
    private Quaternion _knockdownTargetRotation;

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        _isGrounded = _controller.isGrounded;

        if (_isKnockedDown)
        {
            HandleKnockdownState();
            return;
        }

        // Manage Coyote Time timer based on grounding state
        if (_isGrounded)
        {
            _coyoteTimer = coyoteTimeDuration; // Reset timer while on the ground
            if (_velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }
        else
        {
            _coyoteTimer -= Time.deltaTime; // Count down when airborne/slipping
        }

        // --- SLOPE SLIDING CALCULATION ---
        Vector3 slideMovement = Vector3.zero;
        bool isSliding = false;

        // Use a raycast slightly longer than half-height to catch slopes even during physics jitters
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, (_controller.height / 2f) + 0.6f))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

            // Trigger slide if surface angle exceeds the controller's slope limit
            if (slopeAngle > _controller.slopeLimit)
            {
                isSliding = true;
                slideMovement = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized * slideSpeed;
            }
        }

        // 2. Movement Input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        Vector3 moveDirection = Vector3.zero;

        // Allow directional control even when sliding to fight against gravity
        if (inputDirection.magnitude >= 0.1f)
        {
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDirection = camForward * inputDirection.z + camRight * inputDirection.x;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Jumping (Uses Coyote Timer instead of raw _isGrounded to prevent eaten inputs)
        if (Input.GetButtonDown("Jump") && _coyoteTimer > 0f)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * baseGravity);
            _coyoteTimer = 0f; // Instantly exhaust timer so they can't double jump
        }

        // 4. Custom Snappy Gravity Application
        if (_velocity.y < 0)
        {
            _velocity.y += baseGravity * fallMultiplier * Time.deltaTime;
        }
        else
        {
            _velocity.y += baseGravity * Time.deltaTime;
        }

        // Combine normal walking force, slide sliding slide forces, and gravity
        Vector3 finalMovement = (moveDirection * moveSpeed) + slideMovement;
        finalMovement.y = _velocity.y;

        _controller.Move(finalMovement * Time.deltaTime);
    }

    private void HandleKnockdownState()
    {
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }
        else
        {
            if (_velocity.y < 0) _velocity.y += baseGravity * fallMultiplier * Time.deltaTime;
            else _velocity.y += baseGravity * Time.deltaTime;
        }

        _knockbackVelocity.x = Mathf.MoveTowards(_knockbackVelocity.x, 0f, 8f * Time.deltaTime);
        _knockbackVelocity.z = Mathf.MoveTowards(_knockbackVelocity.z, 0f, 8f * Time.deltaTime);

        Vector3 finalPhysicsFrame = _knockbackVelocity;
        finalPhysicsFrame.y = _velocity.y;
        _controller.Move(finalPhysicsFrame * Time.deltaTime);

        if (!_isStandingUp)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, _knockdownTargetRotation, 10f * Time.deltaTime);
        }

        if (_isGrounded && Mathf.Abs(_knockbackVelocity.x) < 0.2f && Mathf.Abs(_knockbackVelocity.z) < 0.2f)
        {
            _knockbackVelocity = Vector3.zero;
            _lieTimer += Time.deltaTime;

            if (_lieTimer >= minimumLieTime && !_isStandingUp)
            {
                float hInput = Input.GetAxisRaw("Horizontal");
                float vInput = Input.GetAxisRaw("Vertical");

                if (Mathf.Abs(hInput) > 0.1f || Mathf.Abs(vInput) > 0.1f)
                {
                    StartCoroutine(StandUpRoutine());
                }
            }
        }
    }

    public void ReceiveExplosionKnockback(Vector3 explosionOrigin, float force)
    {
        if (_isKnockedDown) return;

        Vector3 rawDirection = (transform.position - explosionOrigin).normalized;
        rawDirection.y = 0.4f;

        ApplyKnockdown(rawDirection.normalized * force);
    }

    public void LaunchUpward(float launchForce)
    {
        _isKnockedDown = false;
        _velocity.y = launchForce;
    }

    private void ApplyKnockdown(Vector3 forceVector)
    {
        _isKnockedDown = true;
        _isStandingUp = false;
        _lieTimer = 0.0f;
        _knockbackVelocity = forceVector;
        _velocity.y = forceVector.y * 0.75f;

        Vector3 HallwayDir = new Vector3(forceVector.x, 0, forceVector.z).normalized;
        if (HallwayDir != Vector3.zero)
        {
            _knockdownTargetRotation = Quaternion.LookRotation(HallwayDir) * Quaternion.Euler(90, 0, 0);
        }
        else
        {
            _knockdownTargetRotation = transform.rotation * Quaternion.Euler(90, 0, 0);
        }
    }

    private IEnumerator StandUpRoutine()
    {
        _isStandingUp = true;
        float elapsed = 0f;
        float standDuration = 0.6f;

        Quaternion currentRot = transform.rotation;
        Vector3 forwardCorrection = transform.up;
        forwardCorrection.y = 0f;
        if (forwardCorrection == Vector3.zero) forwardCorrection = transform.forward;

        Quaternion uprightTarget = Quaternion.LookRotation(forwardCorrection.normalized, Vector3.up);

        while (elapsed < standDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(currentRot, uprightTarget, elapsed / standDuration);
            yield return null;
        }

        _isKnockedDown = false;
        _isStandingUp = false;
        _lieTimer = 0.0f;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!_isKnockedDown && hit.gameObject.CompareTag(obstacleTag))
        {
            Vector3 pushDirection = -hit.normal;
            pushDirection.y = 0.5f;
            ApplyKnockdown(pushDirection.normalized * obstacleKnockbackForce);
            return;
        }

        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        if (hit.normal.y > 0.6f)
        {
            float baseWeight = 25f;
            float impactFactor = Mathf.Max(1f, Mathf.Abs(_velocity.y) * 0.3f);
            Vector3 downwardForce = Vector3.down * (baseWeight * impactFactor);
            body.AddForceAtPosition(downwardForce, hit.point, ForceMode.Force);
        }
    }
}