using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Grenade : MonoBehaviour
{
    private float radius;
    private float force;
    private Action explosionCallback;
    private bool hasExploded = false;

    // Called by the Thrower script right as the object is launched
    public void Initialize(float explosionRadius, float explosionForce, Action onExplodeCallback)
    {
        radius = explosionRadius;
        force = explosionForce;
        explosionCallback = onExplodeCallback;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Explode immediately upon hitting any surface, physics object, or player
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Find all objects inside the spherical explosion footprint
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Push enemies, props, or the player outward away from the center
                rb.AddExplosionForce(force, transform.position, radius, 1.0f, ForceMode.Impulse);
            }
        }

        // Notify the thrower script to cycle and generate a new object
        explosionCallback?.Invoke();

        // Self-destruction sequence
        Destroy(gameObject);
    }

    // Optional: Draws the radius in the editor screen when selected for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}