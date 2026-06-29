using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RocketLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public string enemyTag = "Enemy";
    public float projectileSpeed = 18f;
    public float barrelLengthOffset = 1.2f;

    [Header("Ammo System")]
    public int maxAmmo = 3;
    public float reloadTime = 2.5f;
    private int currentAmmo;
    private bool isReloading = false;

    [Header("Soft-Lock Settings")]
    public float lockRange = 40f;

    [Header("Dual-Aim Configuration")]
    public float longPressThreshold = 0.25f;

    [Header("System Tracking States (Read Only)")]
    public Transform currentTarget;
    public bool isManualAimMode = false;

    private float pressStartTime;
    private bool isHoldingButton = false;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    void Update()
    {
        if (!gameObject.CompareTag("Player")) return;

        if (Input.GetMouseButtonDown(0) && currentAmmo > 0 && !isReloading)
        {
            pressStartTime = Time.time;
            isHoldingButton = true;
            isManualAimMode = false;
        }

        if (isHoldingButton)
        {
            float holdDuration = Time.time - pressStartTime;

            if (holdDuration >= longPressThreshold)
            {
                isManualAimMode = true;
            }

            if (isManualAimMode)
            {
                currentTarget = FindManualMouseTarget();
            }
            else
            {
                currentTarget = FindSoftLockTarget();
            }
        }
        else
        {
            currentTarget = FindSoftLockTarget();
        }

        if (Input.GetMouseButtonUp(0) && isHoldingButton)
        {
            isHoldingButton = false;
            FireRocket();
            isManualAimMode = false;
        }
    }

    public void TryFire(Transform aiTarget = null)
    {
        if (aiTarget != null)
        {
            currentTarget = aiTarget;
        }

        if (currentAmmo > 0 && !isReloading)
        {
            FireRocket();
        }
    }

    void FireRocket()
    {
        if (currentAmmo <= 0 || isReloading) return;
        currentAmmo--;

        Vector3 correctedSpawnPosition = firePoint.position + (firePoint.forward * barrelLengthOffset);

        Quaternion launchRotation;
        if (currentTarget != null)
        {
            Vector3 directionToTarget = (currentTarget.position - correctedSpawnPosition).normalized;
            launchRotation = Quaternion.LookRotation(directionToTarget);
        }
        else
        {
            Vector3 forwardVector = gameObject.CompareTag("Player") ? Camera.main.transform.forward : transform.forward;
            launchRotation = Quaternion.LookRotation(forwardVector);
        }

        GameObject rocket = Instantiate(projectilePrefab, correctedSpawnPosition, launchRotation);

        Collider shooterCollider = GetComponent<Collider>();
        Collider rocketCollider = rocket.GetComponent<Collider>();
        if (shooterCollider != null && rocketCollider != null)
        {
            Physics.IgnoreCollision(rocketCollider, shooterCollider);
        }

        TrackingProjectile projScript = rocket.GetComponent<TrackingProjectile>();
        if (projScript != null)
        {
            // PASS THE LIVE TARGET FOR DYNAMIC HOMING CHASE LOOPS!
            projScript.Initialize(currentTarget, gameObject, projectileSpeed);
        }

        if (currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    Transform FindManualMouseTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform bestTarget = null;
        float closestScreenDistance = Mathf.Infinity;
        Vector2 mousePos = Input.mousePosition;

        foreach (GameObject target in potentialTargets)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(target.transform.position);
            if (screenPoint.z > 0)
            {
                float pixelDistance = Vector2.Distance(mousePos, screenPoint);

                if (pixelDistance < closestScreenDistance && Vector3.Distance(transform.position, target.transform.position) <= lockRange)
                {
                    closestScreenDistance = pixelDistance;
                    bestTarget = target.transform;
                }
            }
        }
        return bestTarget;
    }

    Transform FindSoftLockTarget()
    {
        Camera cam = Camera.main;
        if (cam == null) return null;

        Ray cameraRay = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(cameraRay, out RaycastHit hit, lockRange))
        {
            if (hit.collider.CompareTag(enemyTag)) return hit.transform;
        }

        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform closestTarget = null;
        float closestDistance = lockRange;

        foreach (GameObject target in potentialTargets)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(target.transform.position);
            bool inCameraFOV = viewportPos.z > 0 && viewportPos.x >= 0 && viewportPos.x <= 1 && viewportPos.y >= 0 && viewportPos.y <= 1;

            if (inCameraFOV)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestTarget = target.transform;
                    closestDistance = distance;
                }
            }
        }
        return closestTarget;
    }
}