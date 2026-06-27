using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackingProjectile : MonoBehaviour
{
    public float speed = 12f;
    public float turnSpeed = 2f; // Low value = wide, dodgeable turning radius
    public float baseKnockbackForce = 15f;

    private Transform target;
    private GameObject shooter;
    private Rigidbody rb;

    public void Initialize(Transform targetEnemy, GameObject projectileOwner)
    {
        target = targetEnemy;
        shooter = projectileOwner;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 6f); // Safety net destruction
    }

    void FixedUpdate()
    {
        if (target != null)
        {
            // Smoothly rotate towards the target over time
            Vector3 targetDir = (target.position - transform.position).normalized;
            Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnSpeed * Time.fixedDeltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
        }

        // Move forward constantly at 12 meters per second
        rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Avoid exploding on the person who shot it immediately
        if (collision.gameObject == shooter) return;

        HealthAndRagdoll targetHealth = collision.gameObject.GetComponent<HealthAndRagdoll>();
        if (targetHealth != null)
        {
            // Calculate Knockback
            Rigidbody targetRb = collision.gameObject.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                Vector3 knockbackDir = (collision.transform.position - transform.position).normalized;
                knockbackDir.y = 0.3f; // Pop them slightly up into the air

                float finalForce = baseKnockbackForce;

                // If the target is mid-air, multiply the knockback force by 200%
                if (!targetHealth.isGrounded)
                {
                    finalForce *= 2f;
                }

                targetHealth.TakeDamage(1, knockbackDir * finalForce);
            }
        }

        // Create impact effects here if wanted
        Destroy(gameObject);
    }
}