using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 

public class MainMenuManager : MonoBehaviour
{
    // Public string to hold the name of your main game scene
    public string gameSceneName = "YourGameSceneName"; // IMPORTANT: Change this to actual game scene name

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

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}