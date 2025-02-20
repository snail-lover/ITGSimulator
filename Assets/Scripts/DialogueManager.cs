using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    // UI or references for displaying text, choices, etc.
    // (Hook these up via the Inspector or dynamically in code.)
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private UnityEngine.UI.Text dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Button statsButton; // Reference to the stats button
    [SerializeField] private NPCStatPage npcStatPage; // Reference to the NPCStatPage script
    
    private BaseNPC currentNPC; 
    private DialogueData currentData;
    private DialogueNode currentNode;
    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public BaseNPC GetCurrentNPC()
    {
        return currentNPC?.GetComponent<BaseNPC>();
    }

    public void StartDialogue(BaseNPC npc)
    {
        currentNPC = npc;
        currentData = npc.GetDialogueData();
        if (currentData == null) return;

        // 1. Get NPC's current love
        int lovePoints = npc.currentLove;

        // 2. Find the correct tier
        string startNodeID = FindStartNodeIDForLove(lovePoints, currentData);

        // 3. If found, show that node
        if (!string.IsNullOrEmpty(startNodeID) && currentData.nodeDictionary.TryGetValue(startNodeID, out currentNode))
        {
            dialogueUI.ShowDialogue(this, currentNode);
            statsButton.gameObject.SetActive(true); // Show the stats button
            statsButton.onClick.RemoveAllListeners(); // Remove any existing listeners
            statsButton.onClick.AddListener(OnStatsButtonClick); // Add listener to the stats button

            // Load the stats into the NPCStatPage
            if (npcStatPage != null)
            {
                npcStatPage.currentNPC = currentNPC; // Set the current NPC
                npcStatPage.LoadStats(); // Load the stats
            }
        }
        else
        {
            // If no tier found, or node is missing, we can end or do fallback
            Debug.LogWarning("No valid tier or node found for NPC's current love. Ending dialogue.");
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        dialogueUI.HideDialogue();
        
        if (currentNPC != null)
        {
            currentNPC.isTalking = false; // Re-enable movement input
            currentNPC.ResumeNPC();
        }
        dialoguePanel.SetActive(false);
        currentNPC = null;
        currentData = null;
        currentNode = null;
        statsButton.gameObject.SetActive(false); // Hide the stats button
        statsButton.onClick.RemoveAllListeners(); // Remove listener from the stats button

        // Hide the stats panel
        if (npcStatPage != null)
        {
            npcStatPage.HideStatsPanel();
        }

        // Ensure EndInteraction is called to reset the flag
        Object.FindFirstObjectByType<PointAndClickMovement>().EndInteraction();
    }

    private void OnStatsButtonClick()
    {
        if (npcStatPage != null)
        {
            npcStatPage.ToggleStatsPanel(); // Toggle the stats panel
        }
    }

    private string FindStartNodeIDForLove(int lovePoints, DialogueData data)
    {
        foreach (var tier in data.loveTiers)
        {
            if (lovePoints >= tier.minLove && lovePoints <= tier.maxLove)
            {
                return tier.startNodeID;
            }
        }
        return null; // If no tier matches
    }

    public void HandleChoiceSelected(DialogueChoice choice)
    {
        // Apply the love point change
        currentNPC.currentLove += choice.lovePointChange;

        // Next node?
        if (string.IsNullOrEmpty(choice.nextNodeID) || 
            !currentData.nodeDictionary.ContainsKey(choice.nextNodeID))
        {
            EndDialogue();
        }
        else
        {
            currentNode = currentData.nodeDictionary[choice.nextNodeID];
            dialogueUI.ShowDialogue(this, currentNode);
        }
    }

    private void DisplayNode()
    {
        // Clear previous choice buttons
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }

        // Set main text
        dialogueText.text = currentNode.text;

        // Create choice buttons
        foreach (var choice in currentNode.choices)
        {
            // Check for item gating if love points are at thresholds 3,6,9
            if (RequiresItemToProceed() && !HasRequiredItem(choice))
            {
                // If player doesn't have the required item, skip creating this button or mark it locked
                continue;
            }

            var buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
            var buttonText = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
            buttonText.text = choice.choiceText;

            // Add listener to handle the choice
            buttonObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                ApplyLovePointChange(choice.lovePointChange);
                GoToNextNode(choice.nextNodeID);
            });
        }
    }

    public bool IsChoiceAvailable(DialogueChoice choice)
    {
        // If no item is required, the choice is available
        if (choice.itemGate == null || string.IsNullOrEmpty(choice.itemGate.itemName))
        {
            return true;
        }

        // Check if the player has the required item
        return Inventory.Instance.HasItem(choice.itemGate.itemName);
    }

    private bool RequiresItemToProceed()
    {
        // Example check: if current love is exactly 3,6,9 => must have an item
        int love = currentNPC.currentLove;
        return (love == 3 || love == 6 || love == 9);
    }

    private bool HasRequiredItem(DialogueChoice choice)
    {
        // Example check: does Inventory have the item needed for this choice?
        // Adapt to your actual inventory system
        if (choice.itemGate != null && !Inventory.Instance.HasItem(choice.itemGate.itemName))
        {
            return false;
        }
        return true;
    }

    private void ApplyLovePointChange(int changeAmount)
    {
        // Prevent love from going higher if at 3,6,9 but missing item
        if (RequiresItemToProceed() && changeAmount > 0)
        {
            // If an item is needed and the player doesn't have it, skip
            // or handle how you want (maybe partial block)
            return;
        }
        currentNPC.currentLove += changeAmount;
    }

    private void GoToNextNode(string nextNodeID)
    {
        // If nextNodeID is empty, we end dialogue
        if (string.IsNullOrEmpty(nextNodeID) || !currentData.nodeDictionary.ContainsKey(nextNodeID))
        {
            EndDialogue();
            return;
        }

        currentNode = currentData.nodeDictionary[nextNodeID];
        DisplayNode();
    }
}
