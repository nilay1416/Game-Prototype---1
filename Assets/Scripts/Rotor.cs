using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotor : MonoBehaviour
{
    [Tooltip("Degrees per second the rotor spins.")]
    public float spinSpeed = 90f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Must be kinematic to forcefully displace players
    }

    void FixedUpdate()
    {
        // Calculate the next rotation step
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, spinSpeed * Time.fixedDeltaTime, 0));

        // Apply via Rigidbody physics so collision velocity calculations work seamlessly
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}