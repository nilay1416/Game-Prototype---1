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
    public GameObject controlsMenuUI;

    [Header("Scene Navigation Link")]
    public string homeSceneName = "HomeScene";

    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isControlsOpen = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (endGameMenuUI != null) endGameMenuUI.SetActive(false);
        if (controlsMenuUI != null) controlsMenuUI.SetActive(false);

        Time.timeScale = 1f;
        UpdateScoreUI();

        SetCursorState(true);
    }

    void Update()
    {
        // --- WEBGL BROWSER INTERACTIVE POINTER LOCK TRIGGER ---
        // If the game is active, and the browser drops cursor containment, 
        // click back inside the gameplay frame to re-engage 360 camera freedom.
        if (!isPaused && !isGameOver && !isControlsOpen)
        {
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorState(true);
            }
        }
        // ------------------------------------------------------

        if (isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
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

    private void SetCursorState(bool lockState)
    {
        if (lockState)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            pauseMenuUI.transform.SetAsLastSibling();
        }
        Time.timeScale = 0f;
        SetCursorState(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        SetCursorState(true);
    }

    public void OpenControlsMenu()
    {
        isControlsOpen = true;
        if (controlsMenuUI != null)
        {
            controlsMenuUI.SetActive(true);
            controlsMenuUI.transform.SetAsLastSibling();
        }
        Time.timeScale = 0f;
        SetCursorState(false);
    }

    public void CloseControlsMenu()
    {
        isControlsOpen = false;
        if (controlsMenuUI != null) controlsMenuUI.SetActive(false);
        Time.timeScale = 1f;
        SetCursorState(true);
    }

    void WinGameLoop()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        if (endGameMenuUI != null)
        {
            endGameMenuUI.SetActive(true);
            endGameMenuUI.transform.SetAsLastSibling();
        }
        SetCursorState(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        CheckpointManager.ResetToFirstCheckpoint();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToHome()
    {
        Time.timeScale = 1f;
        SetCursorState(false);
        SceneManager.LoadScene(homeSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}