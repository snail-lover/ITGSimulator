using UnityEngine;

public class BaseDialogue : MonoBehaviour
{
    public static BaseDialogue Instance; // Singleton instance
    public GameObject dialogueWindow;   // Assign your dialogue UI panel in the Inspector

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartDialoguePlaceholder(BaseNPC npc)
    {
        Debug.Log($"Dialogue started with {npc.name}");

        // Show the dialogue window
        if (dialogueWindow != null)
        {
            dialogueWindow.SetActive(true);
        }
    }

    public void CloseDialogue()
    {
        Debug.Log("Dialogue exited.");

        // Hide the dialogue window
        if (dialogueWindow != null)
        {
            dialogueWindow.SetActive(false);
        }
    }
}