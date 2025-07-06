using UnityEngine;
using UnityEngine.SceneManagement; 
using TMPro; 
using System.Collections; 

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the main PausePanel GameObject here.")]
    public GameObject pauseMenuUI;

    [Tooltip("Optional: A TextMeshProUGUI to show a 'Game Saved!' message.")]
    public TextMeshProUGUI saveMessageText;

    // To keep track of the game state
    private bool isPaused = false;

    void Start()
    {
        // Initialize menu and save message visibility, reset time scale and pause state
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (saveMessageText != null)
            saveMessageText.gameObject.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // Toggle pause state when Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        // Show pause menu and stop game time
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        // Hide pause menu and resume game time
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void SaveGame()
    {
        // Save game state using WorldDataManager and show confirmation message
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.SaveGame();
            Debug.Log("PauseMenu: Save Game button clicked.");
            StartCoroutine(ShowSaveMessage());
        }
        else
        {
            Debug.LogError("WorldDataManager instance not found. Cannot save game.");
        }
    }

    public void LoadGame()
    {
        // Load game state and reload current scene to apply loaded data
        if (WorldDataManager.Instance != null)
        {
            Time.timeScale = 1f;
            WorldDataManager.Instance.LoadGame();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Debug.Log("PauseMenu: Load Game button clicked and scene reloaded.");
        }
        else
        {
            Debug.LogError("WorldDataManager instance not found. Cannot load game.");
        }
    }

    private IEnumerator ShowSaveMessage()
    {
        // Display "Game Saved!" message for a short duration
        if (saveMessageText != null)
        {
            saveMessageText.text = "Game Saved!";
            saveMessageText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            saveMessageText.gameObject.SetActive(false);
        }
    }

    public void Restart()
    {
        // Restart the current scene and resume game time
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        // Exit the application (or stop play mode in the editor)
        Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}