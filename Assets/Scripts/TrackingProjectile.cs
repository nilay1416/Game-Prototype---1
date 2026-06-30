using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackingProjectile : MonoBehaviour
{
    [Header("Tarodev Homing Settings")]
    public float speed = 18f;
    public float turnSpeed = 4.5f;
    public bool usePrediction = true;
    public float predictionScale = 0.15f;

    [Header("Dodge & Balance Configurations")]
    [Tooltip("Deactivates tracking if the rocket gets closer than this distance (in meters) to the target, allowing it to overshoot when you move/dash.")]
    public float homingDisableDistance = 5f;
    [Tooltip("Maximum time (in seconds) the rocket is allowed to home in on the target before it gives up and flies completely straight.")]
    public float maxHomingDuration = 2.0f;

    [Header("Editor AoE & Control Values")]
    public float explosionRadius = 5f;
    public float rocketDamage = 35f;
    public float baseBlastForce = 22f;
    public float maxLifetime = 5f;

    [Header("Obstacle Cover Settings")]
    [Tooltip("Select the Layers that should block explosion damage (e.g., Obstacles, Environment, Buildings).")]
    public LayerMask obstacleLayers;

    [Header("Visual Effects")]
    public GameObject explosionParticlePrefab;

    [Header("Audio Settings")]
    public AudioClip explosionSoundClip;

    private Transform target;
    private GameObject shooter;
    private Rigidbody rb;
    private bool hasExploded = false;

    // Tracking variables for balance control loops
    private float activeHomingTimer = 0f;
    private bool trackingDeactivated = false;

    public void Initialize(Transform liveTarget, GameObject projectileOwner, float customSpeed)
    {
        target = liveTarget;
        shooter = projectileOwner;
        speed = customSpeed;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        Destroy(gameObject, maxLifetime);
    }

    void FixedUpdate()
    {
        if (hasExploded) return;

        Vector3 targetPredictionPoint = transform.position + transform.forward;

        if (target != null)
        {
            float currentDistance = Vector3.Distance(transform.position, target.position);
            activeHomingTimer += Time.fixedDeltaTime;

            // --- BALANCED DODGE CHECK SYSTEM ---
            // If the rocket gets too close OR has tracked for too long, permanently turn off tracking adjustments
            if (currentDistance <= homingDisableDistance || activeHomingTimer >= maxHomingDuration)
            {
                trackingDeactivated = true;
            }

            // Only update steering rotation angles if homing tracking is still active
            if (!trackingDeactivated)
            {
                targetPredictionPoint = target.position;

                if (usePrediction)
                {
                    Rigidbody targetRb = target.GetComponent<Rigidbody>();
                    if (targetRb != null)
                    {
                        float flatDistance = Vector3.Distance(transform.position, target.position);
                        float lookAheadTime = (flatDistance * predictionScale) / speed;
                        targetPredictionPoint += targetRb.velocity * lookAheadTime;
                    }
                }

                Vector3 targetDirection = (targetPredictionPoint - transform.position).normalized;
                Vector3 smoothTurnDirection = Vector3.RotateTowards(transform.forward, targetDirection, turnSpeed * Time.fixedDeltaTime, 0.0f);
                transform.rotation = Quaternion.LookRotation(smoothTurnDirection);
            }
        }

        // NO CHANGES TO PROJECTILE MOTION: Maintained the exact forward momentum implementation vector
        rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded || collision.gameObject == shooter) return;
        ExplodeBlastRadius();
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasExploded || other.gameObject == shooter) return;
        ExplodeBlastRadius();
    }

    void ExplodeBlastRadius()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionSoundClip != null)
        {
            AudioSource.PlayClipAtPoint(explosionSoundClip, transform.position);
        }

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        if (explosionParticlePrefab != null)
        {
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
        }

        // Purple visual area sphere overlay
        GameObject purpleSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        purpleSphere.transform.position = transform.position;
        purpleSphere.transform.localScale = Vector3.one * (explosionRadius * 2f);
        Destroy(purpleSphere.GetComponent<Collider>());

        Renderer sphereRender = purpleSphere.GetComponent<Renderer>();
        if (sphereRender != null)
        {
            Material transparentMat = new Material(Shader.Find("Standard"));
            transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMat.SetInt("_ZWrite", 0);
            transparentMat.EnableKeyword("_ALPHABLEND_ON");
            transparentMat.renderQueue = 3000;

            transparentMat.color = new Color(0.55f, 0.0f, 1.0f, 0.35f);
            sphereRender.material = transparentMat;
        }
        Destroy(purpleSphere, 0.4f);

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<HealthAndRagdoll> uniqueVictims = new HashSet<HealthAndRagdoll>();

        foreach (Collider col in hitColliders)
        {
            HealthAndRagdoll health = col.GetComponent<HealthAndRagdoll>();
            if (health == null) health = col.GetComponentInParent<HealthAndRagdoll>();

            if (health != null && !uniqueVictims.Contains(health))
            {
                Vector3 targetCenter = col.bounds.center;
                Vector3 raycastStartPoint = transform.position + (targetCenter - transform.position).normalized * 0.05f;

                if (Physics.Linecast(raycastStartPoint, targetCenter, obstacleLayers))
                {
                    continue;
                }

                uniqueVictims.Add(health);

                Vector3 knockbackVector = (health.transform.position - transform.position).normalized;
                knockbackVector.y = 0.5f;

                if (knockbackVector == Vector3.zero) knockbackVector = Vector3.up;

                health.TakeDamage(rocketDamage, knockbackVector * baseBlastForce);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}