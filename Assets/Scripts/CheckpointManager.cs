
using UnityEngine;

public static class CheckpointManager
{ // <-- FIXED: Added the missing opening brace here!
    private static Vector3 initialSpawnPoint;
    private static Vector3 activeCheckpointPoint;
    private static bool hasInitialSpawnBeenSet = false;

    public static void InitializeInitialSpawn(Vector3 startPosition)
    {
        // Only lock the true starting spawn coordinate once per level initialization
        if (!hasInitialSpawnBeenSet)
        {
            initialSpawnPoint = startPosition;
            activeCheckpointPoint = startPosition;
            hasInitialSpawnBeenSet = true;
        }
    }

    public static void UpdateCheckpoint(Vector3 newCheckpointPosition)
    {
        activeCheckpointPoint = newCheckpointPosition;
        Debug.Log("[Checkpoint] Active respawn anchor updated to: " + newCheckpointPosition);
    }

    public static Vector3 GetActiveRespawnPoint()
    {
        return activeCheckpointPoint;
    }

    public static void ResetToFirstCheckpoint()
    {
        // Forces the tracking vector back to the true first scene configuration
        activeCheckpointPoint = initialSpawnPoint;
        Debug.Log("[Checkpoint] Global state reset back to the initial level checkpoint.");
    }
}