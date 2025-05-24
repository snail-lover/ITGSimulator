using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using UnityEngine.UI; // If you ever need to interact with UI components directly (e.g., sliders, input fields)

public class MainMenuManager : MonoBehaviour
{
    // Public string to hold the name of your main game scene
    public string gameSceneName = "YourGameSceneName"; // IMPORTANT: Change this to your actual game scene name

    public void StartGame()
    {
        Debug.Log("Starting game, loading scene: " + gameSceneName);
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene Name is not set in MainMenuManager!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked.");
        Application.Quit();

        // If running in the Unity Editor, Application.Quit() might not close the editor directly.
        // This line helps stop play mode in the editor.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}