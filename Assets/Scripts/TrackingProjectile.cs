using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackingProjectile : MonoBehaviour
{
    [Header("Tarodev Homing Settings")]
    [Tooltip("Forward velocity speed of the rocket.")]
    public float speed = 18f;
    [Tooltip("How tightly the rocket can turn. Lower values (3.5 - 5.5) make the rocket highly dodgeable!")]
    public float turnSpeed = 4.5f;
    [Tooltip("Predicts target positioning based on their movement velocity vectors.")]
    public bool usePrediction = true;
    [Tooltip("Scale factor for the prediction look-ahead window.")]
    public float predictionScale = 0.15f;

    [Header("Editor AoE & Control Values")]
    [Tooltip("The radius of the damage area sphere.")]
    public float explosionRadius = 5f;
    [Tooltip("Damage dealt to characters caught within the blast area.")]
    public float rocketDamage = 35f;
    [Tooltip("The radial explosion knockback force applied to players.")]
    public float baseBlastForce = 22f;
    [Tooltip("The rocket automatically self-destructs if it flies for this long without hitting anything.")]
    public float maxLifetime = 5f;

    [Header("Visual Effects")]
    public GameObject explosionParticlePrefab;

    private Transform target;
    private GameObject shooter;
    private Rigidbody rb;
    private bool hasExploded = false;

    public void Initialize(Transform liveTarget, GameObject projectileOwner, float customSpeed)
    {
        target = liveTarget;
        shooter = projectileOwner;
        speed = customSpeed;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // MATCHING YOUR PREFAB EXACTLY: Run as a standard active physics object
        rb.isKinematic = false;
        rb.useGravity = false;

        // SOLID BLOCKING FIX: Keep isTrigger = false so the physics engine treats the 
        // environment as a solid wall, preventing the rocket from passing through floors or obstacles.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;
        }

        // TUNNELING SAFETY ANCHOR: Switch detection mode to Continuous. 
        // This stops high-speed tracking forces from clipping through geometry edges!
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Safety fuse to kill the rocket if outrun for too long
        Destroy(gameObject, maxLifetime);
    }

    void FixedUpdate()
    {
        if (hasExploded) return;

        // Default: Fly perfectly straight forward if no target is active
        Vector3 targetPredictionPoint = transform.position + transform.forward;

        if (target != null)
        {
            targetPredictionPoint = target.position;

            // REAL-TIME TRAJECTORY PREDICTION (Tarodev Math)
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

            // Smoothly rotate the heading vector toward the target's predicted location
            Vector3 targetDirection = (targetPredictionPoint - transform.position).normalized;
            Vector3 smoothTurnDirection = Vector3.RotateTowards(transform.forward, targetDirection, turnSpeed * Time.fixedDeltaTime, 0.0f);

            transform.rotation = Quaternion.LookRotation(smoothTurnDirection);
        }

        // Apply authentic continuous physical propulsion forward
        rb.velocity = transform.forward * speed;
    }

    // Handles solid physical collisions (Ground, walls, obstacle platforms)
    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded || collision.gameObject == shooter) return;
        ExplodeBlastRadius();
    }

    // Fallback trigger handler in case it hits a bounding zone or alternative projectile trigger
    void OnTriggerEnter(Collider other)
    {
        if (hasExploded || other.gameObject == shooter) return;
        ExplodeBlastRadius();
    }

    void ExplodeBlastRadius()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Instantly shut off physics and visuals to prevent multi-hit frame glitches
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        GetComponent<Collider>().enabled = false;

        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        // Spawn your custom explosion VFX
        if (explosionParticlePrefab != null)
        {
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
        }

        // --- RESTORED IN-GAME PURPLE AOE SPHERE VISUALIZER ---
        // Dynamically instantiates a temporary purple sphere representing the exact blast radius
        GameObject purpleSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        purpleSphere.transform.position = transform.position;
        purpleSphere.transform.localScale = Vector3.one * (explosionRadius * 2f); // Diameter = Radius * 2

        // Destroy its collider so it remains strictly a visual effect tool
        Destroy(purpleSphere.GetComponent<Collider>());

        Renderer sphereRender = purpleSphere.GetComponent<Renderer>();
        if (sphereRender != null)
        {
            // Apply a runtime transparent material setup
            Material transparentMat = new Material(Shader.Find("Standard"));
            transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            transparentMat.SetInt("_ZWrite", 0);
            transparentMat.EnableKeyword("_ALPHABLEND_ON");
            transparentMat.renderQueue = 3000;

            // Classic semi-transparent deep purple representation
            transparentMat.color = new Color(0.6f, 0.0f, 1.0f, 0.35f);
            sphereRender.material = transparentMat;
        }
        Destroy(purpleSphere, 0.35f); // Automatically clean up after a split second
        // ----------------------------------------------------

        // SPHERICAL EXPLOSION AOE AREA SCAN
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        HashSet<HealthAndRagdoll> uniqueVictims = new HashSet<HealthAndRagdoll>();

        foreach (Collider col in hitColliders)
        {
            HealthAndRagdoll health = col.GetComponent<HealthAndRagdoll>();
            if (health == null) health = col.GetComponentInParent<HealthAndRagdoll>();

            if (health != null && !uniqueVictims.Contains(health))
            {
                uniqueVictims.Add(health);

                // Calculate knockback direction exploding outward away from the blast center
                Vector3 knockbackVector = (health.transform.position - transform.position).normalized;
                knockbackVector.y = 0.5f; // Add a clean loft arc pop

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