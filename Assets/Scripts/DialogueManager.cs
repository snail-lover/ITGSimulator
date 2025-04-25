using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class DialogueManager : MonoBehaviour
{

    public static DialogueManager Instance { get; private set; }

    //UI References (Assign in Inspector)
    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI; 
    [SerializeField] private Button statsButton;   
    [SerializeField] private NPCStatPage npcStatPage; 

    private BaseNPC currentNPC;
    private DialogueData currentData;
    private DialogueNode currentNode;

    private PointAndClickMovement playerMovement;
    private void Awake()
    {

        if (Instance == null) { Instance = this; } else { Destroy(gameObject); return; }

        if (dialogueUI == null) Debug.LogError("[DialogueManager] DialogueUI reference not set!");
        if (statsButton == null) Debug.LogError("[DialogueManager] Stats Button reference not set!");
        if (npcStatPage == null) Debug.LogError("[DialogueManager] NPC Stat Page reference not set!");


        FindAndCachePlayerMovement();
        if (npcStatPage != null) npcStatPage.HideStatsPanel(); 
    }

    private void FindAndCachePlayerMovement()
    {

         GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) {
            playerMovement = playerObj.GetComponent<PointAndClickMovement>();
            if (playerMovement == null) Debug.LogError("[DialogueManager] Player missing PointAndClickMovement script!");
        } else {
            Debug.LogError("[DialogueManager] Player object with tag 'Player' not found!");
        }
    }


    public void StartDialogue(BaseNPC npc)
    {
        //Null checks, check if already in dialogue
         if (npc == null) { Debug.LogError("[DialogueManager] StartDialogue called with null NPC."); return; }
        if (currentNPC != null) { Debug.LogWarning($"[DialogueManager] Starting dialogue with {npc.name} while already in dialogue with {currentNPC.name}. Ending previous."); EndDialogue(); }


        Debug.Log($"[DialogueManager] Starting Dialogue with {npc.npcName}"); // Use npcName
        currentNPC = npc;
        currentData = npc.GetDialogueData();

        //Check for null currentData/nodeDictionary - keep as is
         if (currentData == null || currentData.nodeDictionary == null) { /* ... error handling ... */ CleanupFailedDialogueStart(); return; }


        LockPlayerMovement();

        string startNodeID = FindStartNodeIDForLove(currentNPC.currentLove, currentData);
        if (!currentData.nodeDictionary.TryGetValue(startNodeID, out currentNode) || currentNode == null)
        {
            Debug.LogError($"[DialogueManager] Failed to find valid start node '{startNodeID}'.");
            CleanupFailedDialogueStart();
            return;
        }

        Debug.Log($"[DialogueManager] Successfully found starting node: {currentNode.nodeID}");

        //Show Dialogue UI with New Info
        if (dialogueUI == null)
        {
            Debug.LogError("[DialogueManager] DialogueUI reference is missing.");
            CleanupFailedDialogueStart();
            return;
        }

        // Get required info from NPC
        string speakerName = currentNPC.npcName;
        Sprite portrait = currentNPC.npcImage;

        Debug.Log("[DialogueManager] Calling dialogueUI.ShowDialogue...");
        dialogueUI.ShowDialogue(this, currentNode, speakerName, portrait);

        SetupStatsButton(); // Setup listener etc.

        if (npcStatPage != null)
        {
            npcStatPage.currentNPC = currentNPC; // Set NPC for stats page
            npcStatPage.LoadStats();             // Pre-load stats but don't show panel yet
        } else {
             Debug.LogWarning("[DialogueManager] NPCStatPage reference is missing.");
        }

        Debug.Log($"[DialogueManager] StartDialogue for {npc.npcName} completed successfully.");
    }

    public void EndDialogue()
    {
        //Prevent ending if not in dialogue
        if (currentNPC == null && currentNode == null) { /* Debug.LogWarning("[DialogueManager] EndDialogue called but no dialogue active."); */ UnlockPlayerMovement(); return; }

        Debug.Log($"[DialogueManager] Ending Dialogue with {(currentNPC != null ? currentNPC.npcName : "Unknown")}.");

        // 1. Hide Dialogue UI (calls fade out)
        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        // 2. Hide Stats Button & Panel
        if (statsButton != null)
        {
            // statsButton.gameObject.SetActive(false);
            statsButton.onClick.RemoveAllListeners(); // Still clean up listeners
        }
        if (npcStatPage != null)
        {
            npcStatPage.HideStatsPanel(); // Hide the separate stats panel
        }

        // 3. Tell the NPC to Resume
        if (currentNPC != null)
        {
            currentNPC.ResumeNPC();
        }

        // 4. Unlock Player Movement
        UnlockPlayerMovement();

        // 5. Clear Dialogue State
        currentNPC = null;
        currentData = null;
        currentNode = null;
        Debug.Log("--- DIALOGUE MANAGER: EndDialogue COMPLETE ---");
    }

    public void HandleChoiceSelected(DialogueChoice choice)
    {
        //Null checks for state
        if (currentNPC == null || currentData == null || choice == null) { Debug.LogError("..."); EndDialogue(); return; }

        Debug.Log($"[DialogueManager] Choice selected: '{choice.choiceText}' -> Next Node: '{choice.nextNodeID}'");


        ApplyLovePointChange(choice.lovePointChange, choice);

        if (string.IsNullOrEmpty(choice.nextNodeID) || !currentData.nodeDictionary.TryGetValue(choice.nextNodeID, out DialogueNode nextNode))
        {
            Debug.Log($"[DialogueManager] Next node ID ('{choice.nextNodeID ?? "null"}') invalid or missing. Ending dialogue.");
            EndDialogue();
        }
        else
        {
            currentNode = nextNode;
            if (dialogueUI != null)
            {
                //Pass speaker info again when showing the next node
                dialogueUI.ShowDialogue(this, currentNode, currentNPC.npcName, currentNPC.npcImage);
            }
            else
            {
                Debug.LogError("[DialogueManager] DialogueUI reference missing, cannot show next node.");
                EndDialogue();
            }
        }
    }

    public bool IsChoiceAvailable(DialogueChoice choice)
    {
        if (choice == null || choice.itemGate == null || string.IsNullOrEmpty(choice.itemGate.itemName)) return true;
        if (Inventory.Instance == null) { Debug.LogError("[DialogueManager] Inventory instance is null!"); return false; }
        return Inventory.Instance.HasItem(choice.itemGate.itemName);
    }

    public BaseNPC GetCurrentNPC() => currentNPC;

    private string FindStartNodeIDForLove(int lovePoints, DialogueData data)
    {
         if (data == null || data.loveTiers == null) { /*...*/ return data?.startNodeID; }
         foreach (var tier in data.loveTiers) {
             if (tier != null && lovePoints >= tier.minLove && lovePoints <= tier.maxLove) { return tier.startNodeID; }
         }
         return data.startNodeID; 
    }

    private void SetupStatsButton()
    {
        if (statsButton != null)
        {
            statsButton.onClick.RemoveAllListeners();
            statsButton.onClick.AddListener(OnStatsButtonClick);
            Debug.Log("[DialogueManager] Stats button listener configured.");
        }
         else
        {
            Debug.LogError("[DialogueManager] Stats Button reference is missing in Inspector!");
        }
    }

    private void OnStatsButtonClick()
    {
         Debug.Log("[DialogueManager] Stats Button clicked.");
        if (npcStatPage != null)
        {
            npcStatPage.ToggleStatsPanel(); // Tell the stats page script to handle toggling
        }
        else
        {
            Debug.LogWarning("[DialogueManager] Stats Button clicked, but NPCStatPage reference is missing.");
        }
    }

    private void ApplyLovePointChange(int changeAmount, DialogueChoice choice) { 
        if (currentNPC == null) return;
        currentNPC.currentLove += changeAmount;
        Debug.Log($"[DialogueManager] Applied {changeAmount} love. New total for {currentNPC.npcName}: {currentNPC.currentLove}");
    }

    private bool RequiresItemToProceed(int currentLove) { return (currentLove == 3 || currentLove == 6 || currentLove == 9); }

    private void LockPlayerMovement() { /* ... existing logic ... */ if (playerMovement != null) playerMovement.LockMovement(); else Debug.LogWarning("..."); }

    private void UnlockPlayerMovement() { /* ... existing logic ... */ if (playerMovement != null) playerMovement.EndInteraction(); else Debug.LogWarning("..."); }

    private void CleanupFailedDialogueStart()
    {
        Debug.LogError("[DialogueManager] Cleaning up after failed dialogue start.");
        UnlockPlayerMovement();
        currentNPC = null;
        currentData = null;
        currentNode = null;
        if (dialogueUI != null) dialogueUI.HideDialogue(); // Use HideDialogue for fading
        if (npcStatPage != null) npcStatPage.HideStatsPanel();
    }
}
