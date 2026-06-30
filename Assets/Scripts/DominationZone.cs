using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DominationZone : MonoBehaviour
{
    [Header("Scoring Adjustments")]
    [Tooltip("How many points are awarded to the player per second while inside this zone.")]
    public int pointsPerSecond = 1;

    private Coroutine scoreTickCoroutine;

    // Activates the second the player steps into the physical trigger zone boundary box
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (scoreTickCoroutine == null)
            {
                scoreTickCoroutine = StartCoroutine(TickScorePerSecond());
            }
        }
    }

    // Instantly halts point distribution when the player steps outside the zone boundaries
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopActiveScoringLoop();
        }
    }

    private IEnumerator TickScorePerSecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Ticks cleanly exactly once per second

            if (DominationGameManager.Instance != null)
            {
                DominationGameManager.Instance.AddScore(pointsPerSecond);
            }
        }
    }

    private void StopActiveScoringLoop()
    {
        if (scoreTickCoroutine != null)
        {
            StopCoroutine(scoreTickCoroutine);
            scoreTickCoroutine = null;
        }
    }

    // Safety fallback to clean up timing operations if the zone becomes inactive
    private void OnDisable()
    {
        StopActiveScoringLoop();
    }
}