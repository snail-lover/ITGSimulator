using UnityEngine;

public class BaseDialogue : MonoBehaviour
{
    public static BaseDialogue Instance; // Singleton instance
    public static bool IsDialogueActive { get; private set; } = false; // Flag to indicate if dialogue is active
    public GameObject dialogueWindow;   // Assign your dialogue UI panel in the Inspector
    private BaseNPC currentNPC;

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

        if (dialogueWindow != null)
        {
            dialogueWindow.SetActive(false); // Ensure dialogue window is hidden at start
        }
    }

    /// <summary>
    /// Initiates dialogue with the specified NPC.
    /// </summary>
    /// <param name="npc">The NPC to engage in dialogue.</param>
    public void StartDialogue(BaseNPC npc)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning("Dialogue is already active.");
            return;
        }

        currentNPC = npc;
        IsDialogueActive = true;

        Debug.Log($"Dialogue started with {npc.name}");

        // Show the dialogue window
        if (dialogueWindow != null)
        {
            dialogueWindow.SetActive(true);
            // Optionally, initialize dialogue UI elements here
        }
    }

    /// <summary>
    /// Closes the currently active dialogue.
    /// </summary>
    public void CloseDialogue()
    {
        if (!IsDialogueActive)
        {
            Debug.LogWarning("No active dialogue to close.");
            return;
        }

        Debug.Log("Dialogue exited.");

        if (dialogueWindow != null)
        {
            dialogueWindow.SetActive(false);
        }

        if (currentNPC != null)
        {
            currentNPC.ResumeNPC(); // Reset isTalking and resume NPC tasks
            currentNPC = null;
        }

        IsDialogueActive = false;
    }
}
