using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // Name of the main game scene to load
    public string gameSceneName = "YourGameSceneName";

    // Called when the Start Game button is pressed
    public void StartGame()
    {   
        Debug.Log("Starting game, loading scene: " + gameSceneName);

        // Load the specified game scene if the name is set
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene Name is not set in MainMenuManager!");
        }
    }

    // Called when the Quit Game button is pressed
    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked.");
        Application.Quit();

#if UNITY_EDITOR
        // Stop play mode if running in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}