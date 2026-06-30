using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class DominationGameManager : MonoBehaviour
{
    public static DominationGameManager Instance;

    [Header("Score Configuration Settings")]
    public int targetScoreLimit = 100;
    public int currentScore = 0;

    [Header("UI Panel Connections")]
    public TextMeshProUGUI scoreText;
    public GameObject pauseMenuUI;
    public GameObject endGameMenuUI;
    [Tooltip("Drag your newly created Controls Panel UI GameObject here!")]
    public GameObject controlsMenuUI; // <-- NEW: Controls panel slot

    [Header("Scene Navigation Link")]
    public string homeSceneName = "HomeScene";

    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isControlsOpen = false; // <-- NEW: Tracks controls panel state

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Ensure all UI panels are cleanly hidden when starting the match
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (endGameMenuUI != null) endGameMenuUI.SetActive(false);
        if (controlsMenuUI != null) controlsMenuUI.SetActive(false); // <-- NEW

        Time.timeScale = 1f;
        UpdateScoreUI();
    }

    void Update()
    {
        if (isGameOver) return;

        // 1. ESCAPE KEY: Toggles standard Pause Menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If controls panel is open, close it first instead of opening double menus
            if (isControlsOpen)
            {
                CloseControlsMenu();
            }
            else
            {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        // 2. 'C' KEY: Toggles the Controls Panel
        if (Input.GetKeyDown(KeyCode.C) && !isPaused)
        {
            if (isControlsOpen) CloseControlsMenu();
            else OpenControlsMenu();
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver) return;

        currentScore += amount;
        UpdateScoreUI();

        if (currentScore >= targetScoreLimit)
        {
            WinGameLoop();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore + " / " + targetScoreLimit;
        }
    }

    // --- PAUSE MENU ACTIONS ---
    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
    }

    // --- NEW: CONTROLS PANEL ACTIONS ---
    public void OpenControlsMenu()
    {
        isControlsOpen = true;
        if (controlsMenuUI != null) controlsMenuUI.SetActive(true);
        Time.timeScale = 0f; // Pauses physics and game loops while looking at controls
    }

    public void CloseControlsMenu()
    {
        isControlsOpen = false;
        if (controlsMenuUI != null) controlsMenuUI.SetActive(false);
        Time.timeScale = 1f; // Resumes the game smoothly
    }

    void WinGameLoop()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        if (endGameMenuUI != null) endGameMenuUI.SetActive(true);
    }

    // --- INTERACTION BUTTON HOOKS ---
    public void RestartGame()
    {
        Debug.Log("Restarting match...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToHome()
    {
        Debug.Log("Returning to main menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Game Application...");
        Application.Quit();
    }
}