using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeThrower : MonoBehaviour
{
    [Header("Settings")]
    public GameObject grenadePrefab;
    public Transform handPoint;
    public LayerMask environmentLayer; // Set this to include Ground, Walls, Obstacles, etc.
    public float spawnDelay = 1.5f; // Delay in seconds before the next grenade appears

    [Header("Throw Controls")]
    public float maxThrowDistance = 15f;
    public float minThrowDistance = 3f;
    public float mouseSensitivity = 5f;

    [Header("Explosion Properties")]
    public float explosionRadius = 5f;
    public float explosionForce = 700f;

    [Header("Visual Indicators")]
    public LineRenderer trajectoryLine;
    public Transform impactMarker;

    private float currentThrowDistance;
    private GameObject currentGrenade;
    private bool isAiming = false;
    private bool isGrenadeInFlight = false;

    void Start()
    {
        if (trajectoryLine) trajectoryLine.enabled = false;
        if (impactMarker)
        {
            impactMarker.gameObject.SetActive(false);
            impactMarker.localScale = new Vector3(explosionRadius * 2, 0.01f, explosionRadius * 2);
        }

        SpawnGrenadeInHand();
    }

    void Update()
    {
        if (isGrenadeInFlight) return;

        HandleInput();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isAiming = true;
            currentThrowDistance = minThrowDistance;
            if (trajectoryLine) trajectoryLine.enabled = true;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            float mouseInput = Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentThrowDistance = Mathf.Clamp(currentThrowDistance + mouseInput, minThrowDistance, maxThrowDistance);

            UpdateAimIndicators();
        }

        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            ThrowGrenade();
        }
    }

    void UpdateAimIndicators()
    {
        Vector3 targetPoint = transform.position + transform.forward * currentThrowDistance;

        // Initial grounding estimation check for launch velocity calibration
        if (Physics.Raycast(targetPoint + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, environmentLayer))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint.y = transform.position.y;
        }

        Vector3 velocity = CalculateLaunchVelocity(handPoint.position, targetPoint);
        DisplayTrajectory(handPoint.position, velocity);
    }

    void DisplayTrajectory(Vector3 start, Vector3 velocity)
    {
        int maxPoints = 40; // Increased fidelity for smoother line collision tracking
        float timeStep = 0.03f;

        Vector3 currentPos = start;
        Vector3 currentVel = velocity;

        trajectoryLine.positionCount = maxPoints;
        bool hitDetected = false;

        for (int i = 0; i < maxPoints; i++)
        {
            trajectoryLine.SetPosition(i, currentPos);

            Vector3 nextVel = currentVel + Physics.gravity * timeStep;
            Vector3 nextPos = currentPos + currentVel * timeStep;

            // FIX: Linecast looks forward to see if the next segment passes through any geometry
            if (Physics.Linecast(currentPos, nextPos, out RaycastHit hit, environmentLayer))
            {
                // Snap line end to the exact contact point and truncate remaining points
                trajectoryLine.positionCount = i + 1;
                trajectoryLine.SetPosition(i, hit.point);

                // FIX: Reposition and dynamically align impact marker to the surface slope normal
                if (impactMarker)
                {
                    impactMarker.gameObject.SetActive(true);
                    impactMarker.position = hit.point + hit.normal * 0.02f; // Offset slightly to prevent Z-fighting clipping
                    impactMarker.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }

                hitDetected = true;
                break;
            }

            currentVel = nextVel;
            currentPos = nextPos;
        }

        // If the trajectory loop clears the sky without hitting anything, turn off the impact marker
        if (!hitDetected && impactMarker)
        {
            impactMarker.gameObject.SetActive(false);
        }
    }

    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target)
    {
        Vector3 displacement = target - start;
        float timeOfFlight = Mathf.Max(0.5f, displacement.magnitude / 12f);

        float vx = displacement.x / timeOfFlight;
        float vz = displacement.z / timeOfFlight;
        float vy = (displacement.y - 0.5f * Physics.gravity.y * timeOfFlight * timeOfFlight) / timeOfFlight;

        return new Vector3(vx, vy, vz);
    }

    void ThrowGrenade()
    {
        isAiming = false;
        isGrenadeInFlight = true;

        if (trajectoryLine) trajectoryLine.enabled = false;
        if (impactMarker) impactMarker.gameObject.SetActive(false);

        if (currentGrenade != null)
        {
            currentGrenade.transform.SetParent(null);
            Rigidbody rb = currentGrenade.GetComponent<Rigidbody>();
            rb.isKinematic = false;

            Vector3 targetPoint = transform.position + transform.forward * currentThrowDistance;
            if (Physics.Raycast(targetPoint + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f, environmentLayer))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint.y = transform.position.y;
            }

            rb.velocity = CalculateLaunchVelocity(handPoint.position, targetPoint);

            Grenade grenadeScript = currentGrenade.GetComponent<Grenade>();
            grenadeScript.Initialize(explosionRadius, explosionForce, OnGrenadeExploded);
        }
    }

    public void SpawnGrenadeInHand()
    {
        isGrenadeInFlight = false;

        currentGrenade = Instantiate(grenadePrefab, handPoint.position, handPoint.rotation);
        currentGrenade.transform.SetParent(handPoint);

        Rigidbody rb = currentGrenade.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    private void OnGrenadeExploded()
    {
        // FIX: Replaces instantaneous generation loop with an asynchronous delayed spawn sequence
        StartCoroutine(SpawnGrenadeWithDelay());
    }

    private IEnumerator SpawnGrenadeWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnGrenadeInHand();
    }
}