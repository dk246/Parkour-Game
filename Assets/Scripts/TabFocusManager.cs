using UnityEngine;

public class TabFocusManager : MonoBehaviour
{
    [Header("UI Panel")]
    public GameObject clickToStartPanel;

    [Header("Settings")]
    public bool showPanelOnStart = true;

    private bool isPaused = false;

    void Start()
    {
        // Show panel at start of scene
        if (showPanelOnStart)
        {
            ShowPausePanel();
        }
        else
        {
            // Hide panel if we don't want it at start
            if (clickToStartPanel != null)
            {
                clickToStartPanel.SetActive(false);
            }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // This is called when player switches tabs or windows
        if (!hasFocus)
        {
            ShowPausePanel();
        }
    }

    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                ShowPausePanel();
            }
        }

        // Check if game is paused and player clicks to resume
        if (isPaused && Input.GetMouseButtonDown(0))
        {
            ResumeGame();
        }
    }

    void ShowPausePanel()
    {
        if (clickToStartPanel != null)
        {
            clickToStartPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        isPaused = true;


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResumeGame()
    {
        // Hide panel
        if (clickToStartPanel != null)
        {
            clickToStartPanel.SetActive(false);
        }

        // Resume game
        Time.timeScale = 1f;
        isPaused = false;

        // Lock cursor back for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}