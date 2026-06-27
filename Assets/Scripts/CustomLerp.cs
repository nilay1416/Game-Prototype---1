using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomLerp : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How far the obstacle moves from its starting position along the X, Y, and Z axes. Set an axis to 0 to disable movement on it.")]
    public Vector3 movementIntensity = new Vector3(5f, 0f, 0f);

    [Tooltip("How fast the obstacle completes a movement cycle.")]
    public float speed = 1f;

    [Header("Space & Easing")]
    [Tooltip("If true, movement is relative to the object's own rotation (Local). If false, it follows global directions (World).")]
    public bool useLocalSpace = true;

    [Tooltip("Controls the smoothing/easing of the movement. Modifying this graph changes the feel of the obstacle.")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Vector3 startPosition;

    void Start()
    {
        // Record the starting position based on the chosen space
        startPosition = useLocalSpace ? transform.localPosition : transform.position;
    }

    void Update()
    {
        // PingPong cycles a value smoothly back and forth between 0 and 1
        float rawTime = Mathf.PingPong(Time.time * speed, 1f);

        // Pass the raw time into the Animation Curve for highly custom easing (smooth step, sudden snaps, etc.)
        float evaluatedTime = movementCurve.Evaluate(rawTime);

        // Calculate the current offset by multiplying the intensity by our 0-1 interpolation value
        Vector3 targetOffset = movementIntensity * evaluatedTime;

        // Apply the new position
        if (useLocalSpace)
        {
            transform.localPosition = startPosition + targetOffset;
        }
        else
        {
            transform.position = startPosition + targetOffset;
        }
    }
}