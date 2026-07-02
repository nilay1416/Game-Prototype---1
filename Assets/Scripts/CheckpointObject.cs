using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointObject : MonoBehaviour
{
    private void Start()
    {
        // Enforce trigger boundary settings automatically
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Update the global coordinator with this object's exact center position
            CheckpointManager.UpdateCheckpoint(transform.position);
        }
    }
}
