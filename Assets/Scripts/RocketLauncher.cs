using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RocketLauncher : MonoBehaviour
{
    [Header("Launcher Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public string enemyTag = "Enemy"; // Set to "Player" for AI launchers

    [Header("Ammo System")]
    public int maxAmmo = 2;
    public float reloadTime = 2.5f;
    private int currentAmmo;
    private bool isReloading = false;

    [Header("Soft-Lock Settings")]
    public float lockRange = 25f;
    public float fovAngle = 90f;

    void Start()
    {
        currentAmmo = maxAmmo;
    }

    // --- ADDED THIS UPDATE METHOD FOR PLAYER INPUT ---
    void Update()
    {
        // If this specific launcher is attached to the Player, listen for Left Mouse Click
        if (gameObject.CompareTag("Player") && Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }
    // -------------------------------------------------

    public void TryFire()
    {
        if (currentAmmo > 0 && !isReloading)
        {
            FireRocket();
        }
    }

    void FireRocket()
    {
        currentAmmo--;
        Transform target = FindSoftLockTarget();

        GameObject rocket = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        TrackingProjectile projScript = rocket.GetComponent<TrackingProjectile>();

        if (projScript != null)
        {
            projScript.Initialize(target, gameObject);
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

    Transform FindSoftLockTarget()
    {
        GameObject[] potentialTargets = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform closestTarget = null;
        float closestDistance = lockRange;

        foreach (GameObject target in potentialTargets)
        {
            Vector3 directionToTarget = target.transform.position - transform.position;
            float distance = directionToTarget.magnitude;

            if (distance < closestDistance)
            {
                // Check if target falls inside the forward-facing 90-degree arc
                float angle = Vector3.Angle(transform.forward, directionToTarget.normalized);
                if (angle <= fovAngle / 2f)
                {
                    closestTarget = target.transform;
                    closestDistance = distance;
                }
            }
        }
        return closestTarget;
    }
}