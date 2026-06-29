using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponTilt : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            // Force the weapon pivot to match the camera's up/down tilt look angles
            Vector3 camEuler = Camera.main.transform.eulerAngles;
            transform.localRotation = Quaternion.Euler(camEuler.x, 0f, 0f);
        }
    }
}