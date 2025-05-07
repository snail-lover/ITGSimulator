using UnityEngine;
using UnityEngine.UI; // Keep for Button (if you add a close button)
using TMPro;          // Use TextMeshPro

public class NPCStatPage : MonoBehaviour // Make sure this script is on NPC_Stats_Panel
{
    // REMOVED: The button triggering this is now handled by DialogueManager
    // public Button statsButton;

    // Assign these in the Inspector:
    [Header("UI References")]
    [Tooltip("The TextMeshPro component used to display the stats.")]
    public TextMeshProUGUI statsText; // Assign Stats_Text_Display
    [Tooltip("The root panel GameObject.")]
    public GameObject statsPanel; // Assign NPC_Stats_Panel itself

    // Optional Close Button Reference (Assign if you created one)
    [Header("Optional")]
    public Button closeButton;

    // Set by DialogueManager
    [HideInInspector] public BaseNPC currentNPC;

    void Start()
    {
        // Ensure panel reference is this GameObject if not set
        if (statsPanel == null)
        {
            statsPanel = this.gameObject;
        }

        // Validate mandatory references
        if (statsText == null)
        {
            Debug.LogError("[NPCStatPage] Stats Text (TextMeshProUGUI) reference not set in Inspector!", this.gameObject);
        }
        if (statsPanel == null) // Should be set above, but double-check
        {
             Debug.LogError("[NPCStatPage] Stats Panel reference could not be determined!", this.gameObject);
        }


        // Add listener for optional close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideStatsPanel);
        }

        // Hide the stats panel at the start
        if(statsPanel != null)
            statsPanel.SetActive(false);
    }

    // Called by DialogueManager to pre-load or refresh stats
    public void LoadStats()
    {
        if (statsText == null)
        {
            Debug.LogError("[NPCStatPage] Cannot load stats - statsText reference is missing!", this.gameObject);
            return;
        }

        if (currentNPC != null)
        {
            statsText.text = currentNPC.GetStats(); // Get formatted stats string
            // Debug.Log($"[NPCStatPage] Loaded stats for {currentNPC.npcName}");
        }
        else
        {
            statsText.text = "No NPC selected."; // Default text if no NPC
             // Debug.LogWarning("[NPCStatPage] Tried to load stats, but currentNPC is null.");
        }
    }

    // Public method called by DialogueManager's button listener to toggle
    public void ToggleStatsPanel()
    {
        if (statsPanel == null)
        {
            Debug.LogError("[NPCStatPage] Cannot toggle panel, statsPanel reference is missing.", this.gameObject);
            return;
        }

        bool shouldBeActive = !statsPanel.activeSelf;

        if (shouldBeActive)
        {
            // Only show if we have an NPC
            if (currentNPC != null)
            {
                LoadStats(); // Refresh stats just before showing
                statsPanel.SetActive(true);
                // Debug.Log("[NPCStatPage] Showing stats panel.");
            }
            else
            {
                 Debug.LogWarning("[NPCStatPage] Cannot show stats panel, currentNPC is null.");
            }
        }
        else
        {
            statsPanel.SetActive(false); // Hide the stats panel
            // Debug.Log("[NPCStatPage] Hiding stats panel.");
        }
    }

    // Public method called by DialogueManager during EndDialogue OR optional Close Button
    public void HideStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
            // Debug.Log("[NPCStatPage] HideStatsPanel called.");
        }
         else
        {
             Debug.LogError("[NPCStatPage] Cannot hide panel, statsPanel reference is missing.", this.gameObject);
        }
    }
}