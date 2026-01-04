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
        // Initialize menu and save message visibility, reset time scale and pause tat
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
        // Use the new method that only loads data into memory
        if (WorldDataManager.Instance.LoadGameFromFile())
        {
            // Now, load the scene index that we just retrieved from the save file
            int sceneToLoad = WorldDataManager.Instance.saveData.playerState.currentFloorIndex;

            Debug.Log($"PauseMenu: Load command received. Loading scene index: {sceneToLoad}");

            // This will trigger the OnSceneLoaded event in WorldDataManager automatically
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Failed to load game file from Pause Menu.");
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