using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the state and animations for the NPC stats panel.
/// It finds an Animator on its parent container and triggers animations to show/hide.
/// </summary>
public class NPCStatPage : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshPro component used to display the stats.")]
    public TextMeshProUGUI statsText;

    // Set by the DialogueManager when a dialogue starts.
    [HideInInspector]
    public NpcController currentNPC;

    // The Animator is on the parent container and controls the show/hide animations.
    private Animator containerAnimator;
    // Tracks the logical state of the panel to prevent conflicting calls.
    private bool isPanelVisible = false;

    private void Awake()
    {
        // Get the Animator from the parent "StatsContainer".
        // This is crucial for animating the whole group (panel + button).
        containerAnimator = GetComponentInParent<Animator>();
        if (containerAnimator == null)
        {
            Debug.LogError("[NPCStatPage] Could not find an Animator on the parent container! Animations will not work.", this);
        }

        // Validate mandatory reference.
        if (statsText == null)
        {
            Debug.LogError("[NPCStatPage] Stats Text (TextMeshProUGUI) reference not set in the Inspector!", this);
        }

        // Ensure the panel itself is active so its components can be found.
        // The Animator will handle its visual state (position and transparency).
        gameObject.SetActive(true);
    }

    /// <summary>
    /// The public method for the main "StatsButton" to call.
    /// It checks the panel's state and decides whether to show or hide.
    /// </summary>
    public void TogglePanel()
    {
        if (isPanelVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    private void ShowPanel()
    {
        // Don't show if already showing, if the animator is missing, or no NPC is selected.
        if (isPanelVisible || containerAnimator == null || currentNPC == null)
        {
            if (currentNPC == null) Debug.LogWarning("[NPCStatPage] Cannot show stats panel because currentNPC is null.");
            return;
        }

        isPanelVisible = true;
        LoadStats(); // Load data before showing
        containerAnimator.SetTrigger("Show");
        Debug.Log("[NPCStatPage] Triggering Show animation.");
    }

    private void HidePanel()
    {
        // Don't hide if already hidden or if the animator is missing.
        if (!isPanelVisible || containerAnimator == null) return;

        isPanelVisible = false;
        containerAnimator.SetTrigger("Hide");
        Debug.Log("[NPCStatPage] Triggering Hide animation.");
    }

    private void LoadStats()
    {
        if (statsText == null) return;
        statsText.text = (currentNPC != null) ? currentNPC.GetCurrentStats() : "No NPC selected.";
    }
}