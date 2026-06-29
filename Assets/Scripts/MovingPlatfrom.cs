using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatfrom : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Create an empty GameObject for the start position.")]
    public Transform startPoint;
    [Tooltip("Create an empty GameObject for the end position.")]
    public Transform endPoint;
    public float speed = 3f;

    private Rigidbody rb;
    private float percentage;
    private bool movingToEnd = true;

    // A list to track players/enemies currently standing on this platform
    private HashSet<Rigidbody> passengers = new HashSet<Rigidbody>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Essential configuration for moving physics platforms
        rb.isKinematic = true;
        rb.useGravity = false;

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("MovingPlatform missing StartPoint or EndPoint references!");
            return;
        }

        transform.position = startPoint.position;
    }

    void FixedUpdate()
    {
        if (startPoint == null || endPoint == null) return;

        // 1. Calculate the smooth Lerp journey loop
        float distance = Vector3.Distance(startPoint.position, endPoint.position);
        if (distance > 0)
        {
            float step = (speed / distance) * Time.fixedDeltaTime;
            if (movingToEnd)
                percentage += step;
            else
                percentage -= step;

            if (percentage >= 1f) { percentage = 1f; movingToEnd = false; }
            if (percentage <= 0f) { percentage = 0f; movingToEnd = true; }
        }

        // Determine next physical position
        Vector3 nextPosition = Vector3.Lerp(startPoint.position, endPoint.position, percentage);

        // 2. Calculate the exact structural distance moved this frame
        Vector3 deltaMovement = nextPosition - transform.position;

        // 3. Move the platform via the physics engine
        rb.MovePosition(nextPosition);

        // 4. Manually shift all passengers by the exact same frame offset
        // This bypasses the movement scripts fighting physics friction
        foreach (Rigidbody passenger in passengers)
        {
            if (passenger != null)
            {
                passenger.position += deltaMovement;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody passengerRb = collision.gameObject.GetComponent<Rigidbody>();

        // If it has a rigidbody and is standing on top of the platform, add to passenger list
        if (passengerRb != null && collision.transform.position.y > transform.position.y)
        {
            passengers.Add(passengerRb);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Rigidbody passengerRb = collision.gameObject.GetComponent<Rigidbody>();
        if (passengerRb != null)
        {
            passengers.Remove(passengerRb);
        }
    }
}
