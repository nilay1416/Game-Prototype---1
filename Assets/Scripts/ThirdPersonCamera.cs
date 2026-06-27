using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Tracking")]
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float heightOffset = 1.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 200.0f;
    [SerializeField] private float minVerticalAngle = -20.0f;
    [SerializeField] private float maxVerticalAngle = 60.0f;

    private float _mouseX;
    private float _mouseY;

    void Start()
    {
        // Lock and hide the cursor for a seamless gameplay feel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Gather mouse input
        _mouseX += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        _mouseY -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

        // Clamp the vertical look angle so the camera doesn't flip upside down
        _mouseY = Mathf.Clamp(_mouseY, minVerticalAngle, maxVerticalAngle);

        // Calculate rotation and position
        Quaternion rotation = Quaternion.Euler(_mouseY, _mouseX, 0);
        Vector3 targetPosition = target.position + Vector3.up * heightOffset;
        Vector3 position = targetPosition - (rotation * Vector3.forward * distance);

        // Apply to camera
        transform.rotation = rotation;
        transform.position = position;
    }
}