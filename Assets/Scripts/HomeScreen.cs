using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeScreen : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("The exact name of your playable sandbox/battle scene file.")]
    public string gameplaySceneName = "SampleScene";

    public void PlayGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game Application window...");
        Application.Quit();
    }
}