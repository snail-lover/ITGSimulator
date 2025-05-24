using UnityEngine;
using UnityEngine.SceneManagement; // Required for restarting the scene

public class PauseMenu : MonoBehaviour
{
    // Assign this in the Inspector - drag the PausePanel GameObject here
    public GameObject pauseMenuUI;

    // To keep track of the game state
    private bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden at the start of the game
        // (Also disabled in Inspector, but good practice to double-check)
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        // Ensure time scale is normal when the game starts
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Update()
    {
        // Check for the pause input (e.g., Escape key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Toggle pause state
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Method to call when pausing the game
    public void Pause()
    {
        // Show the pause menu UI
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        // Stop time (stops physics, updates not using Time.unscaledDeltaTime, etc.)
        Time.timeScale = 0f;
        // Update the game state flag
        isPaused = true;

        // Optional: Disable player movement/input scripts here
        // FindObjectOfType<PlayerController>()?.enabled = false; // Example
    }

    // Method to call when resuming the game (used by Resume Button)
    public void Resume()
    {
        // Hide the pause menu UI
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        // Resume time
        Time.timeScale = 1f;
        // Update the game state flag
        isPaused = false;

        // Optional: Re-enable player movement/input scripts here
        // FindObjectOfType<PlayerController>()?.enabled = true; // Example
    }

    // Method to call when restarting the game (used by Restart Button)
    public void Restart()
    {
        // Make sure time is back to normal before loading the scene
        Time.timeScale = 1f;
        // Load the current scene again
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // The Start method will handle setting up the menu visibility and isPaused state

         // Optional: If your player is destroyed on scene load, you might not need
         // to re-enable their script here. If they persist, handle it in Start.
    }

    // Method to call when quitting the game (used by Quit Button)
    public void QuitGame()
    {
        Debug.Log("Quitting game..."); // For testing in the editor

        // Quit the application (only works in a built game)
        Application.Quit();

        // If running in the Unity editor, stop playing
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    // Optional: Add methods for other buttons (e.g., Options)
    // public void OpenOptions()
    // {
    //     // Code to open an options menu
    // }
}