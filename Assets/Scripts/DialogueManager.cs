using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System; // Required for Action

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private Button statsButton;
    [SerializeField] private NPCStatPage npcStatPage;

    private BaseNPC currentNPC;
    private DialogueData currentData;
    private DialogueNode currentNode;

    private PointAndClickMovement playerMovement;
    private Action currentCutsceneSegmentCompletionCallback;
    private bool isInCutsceneDialogueMode = false;

    private const int FINAL_CUTSCENE_LOVE_THRESHOLD = 10;

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
        if (npc == null) { Debug.LogError("[DialogueManager] StartDialogue called with null NPC."); return; }
        if (currentNPC != null && currentNPC != npc) 
        { 
            Debug.LogWarning($"[DialogueManager] Starting dialogue with {npc.npcName} while already in dialogue with {currentNPC.name}. Ending previous."); 
            EndDialogue(); // End previous dialogue before starting new one
        }
        else if (currentNPC == npc) // Trying to start dialogue with the same NPC again
        {
            Debug.Log($"[DialogueManager] Already in dialogue with {npc.npcName}. Ignoring StartDialogue call.");
            return;
        }


        Debug.Log($"[DialogueManager] Starting Dialogue with {npc.npcName}");
        currentNPC = npc;
        currentData = npc.GetDialogueData();

        if (currentData == null || currentData.nodeDictionary == null || currentData.nodeDictionary.Count == 0) 
        { 
            Debug.LogError($"[DialogueManager] Dialogue data for {npc.npcName} is null or dictionary empty. Cannot start dialogue.");
            CleanupFailedDialogueStart(); 
            return; 
        }

        LockPlayerMovement();

        string startNodeID = FindStartNodeIDForLove(currentNPC.currentLove, currentData);
        if (string.IsNullOrEmpty(startNodeID) || !currentData.nodeDictionary.TryGetValue(startNodeID, out currentNode) || currentNode == null)
        {
            Debug.LogError($"[DialogueManager] Failed to find valid start node '{startNodeID ?? "null/empty"}' for {npc.npcName}.");
            CleanupFailedDialogueStart();
            return;
        }
        Debug.Log($"[DialogueManager] Successfully found starting node: {currentNode.nodeID} for {npc.npcName}");

        if (dialogueUI == null)
        {
            Debug.LogError("[DialogueManager] DialogueUI reference is missing.");
            CleanupFailedDialogueStart();
            return;
        }

        string speakerName = currentNPC.npcName;
        Sprite portrait = currentNPC.npcImage;

        dialogueUI.ShowDialogue(this, currentNode, speakerName, portrait);

        SetupStatsButton(); 
        UpdateFinalCutsceneButtonState();

        if (npcStatPage != null)
        {
            npcStatPage.currentNPC = currentNPC; 
            npcStatPage.LoadStats();             
        } else {
             Debug.LogWarning("[DialogueManager] NPCStatPage reference is missing.");
        }
        Debug.Log($"[DialogueManager] StartDialogue for {npc.npcName} completed successfully.");
    }

    public void EndDialogue()
    {
        // If in cutscene mode, this should not be the primary way to end dialogue.
        // CutsceneManager should manage its dialogue segments.
        if (isInCutsceneDialogueMode)
        {
            Debug.LogWarning("[DialogueManager] EndDialogue() called while in cutscene dialogue mode. Forcefully cleaning up. This should ideally be handled by CutsceneManager or segment completion.");
            ForceEndCutsceneDialogue(); // Clean up cutscene-specific dialogue state
            // Do not proceed with normal EndDialogue logic (resuming NPC, unlocking player etc.) as CutsceneManager handles that.
            return;
        }

        // Proceed with normal EndDialogue logic
        if (currentNPC == null && currentNode == null && !isInCutsceneDialogueMode) 
        { 
             // If not in cutscene mode and no NPC/Node, ensure player is unlocked if they were locked
            UnlockPlayerMovement(); 
            return; 
        }
        Debug.Log($"[DialogueManager] Ending Dialogue with {(currentNPC != null ? currentNPC.npcName : "Unknown")}.");

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        if (statsButton != null)
        {
            statsButton.onClick.RemoveAllListeners(); 
        }
        if (npcStatPage != null)
        {
            npcStatPage.HideStatsPanel(); 
        }

        if (currentNPC != null)
        {
            currentNPC.ResumeNPC(); // This should allow NPC to resume its tasks
        }
        UnlockPlayerMovement(); // Unlock player movement

        // Clear state for normal dialogue
        currentNPC = null;
        currentData = null;
        currentNode = null;
        Debug.Log("--- DIALOGUE MANAGER: EndDialogue COMPLETE ---");
    }

    public void StartDialogueSegment(BaseNPC npc, TextAsset dialogueFile, string startNodeId, Action onSegmentComplete)
    {
        if (npc == null) { Debug.LogError("[DialogueManager] StartDialogueSegment: NPC is null."); onSegmentComplete?.Invoke(); return; }
        if (dialogueFile == null) { Debug.LogError("[DialogueManager] StartDialogueSegment: DialogueFile is null."); onSegmentComplete?.Invoke(); return; }
        if (string.IsNullOrEmpty(startNodeId)) { Debug.LogError("[DialogueManager] StartDialogueSegment: startNodeId is null or empty."); onSegmentComplete?.Invoke(); return; }

        Debug.Log($"[DialogueManager] Starting Cutscene Dialogue Segment for {npc.npcName}, Node: {startNodeId}");

        isInCutsceneDialogueMode = true; // Set mode first
        currentNPC = npc; // The NPC speaking in the cutscene
        currentCutsceneSegmentCompletionCallback = onSegmentComplete;

        try
        {
            currentData = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            if (currentData == null) throw new System.Exception("Parsed dialogue data is null.");
            
            currentData.nodeDictionary = new Dictionary<string, DialogueNode>(); 
            if (currentData.nodes != null)
            {
                foreach (var node in currentData.nodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.nodeID))
                    {
                        if (!currentData.nodeDictionary.ContainsKey(node.nodeID))
                            currentData.nodeDictionary.Add(node.nodeID, node);
                        else
                            Debug.LogWarning($"[DialogueManager] Duplicate nodeID '{node.nodeID}' in cutscene dialogue {dialogueFile.name}.");
                    }
                }
            } else {
                 Debug.LogError($"[DialogueManager] 'nodes' array is null in cutscene dialogue {dialogueFile.name}.");
                 throw new System.Exception("'nodes' array is null.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueManager] Error parsing cutscene dialogue {dialogueFile.name}: {e.Message}");
            CleanUpCutsceneDialogueState(); // Clean up if parsing fails
            onSegmentComplete?.Invoke(); // Notify failure
            return;
        }

        if (!currentData.nodeDictionary.TryGetValue(startNodeId, out currentNode) || currentNode == null)
        {
            Debug.LogError($"[DialogueManager] Cutscene start node '{startNodeId}' not found in {dialogueFile.name}.");
            CleanUpCutsceneDialogueState();
            onSegmentComplete?.Invoke();
            return;
        }

        if (dialogueUI == null) 
        { 
            Debug.LogError("[DialogueManager] DialogueUI is null in StartDialogueSegment.");
            CleanUpCutsceneDialogueState(); 
            onSegmentComplete?.Invoke(); 
            return; 
        }

        dialogueUI.ShowDialogue(this, currentNode, currentNPC.npcName, currentNPC.npcImage);
        dialogueUI.SetFinalCutsceneButtonVisibility(false, null); // Ensure final cutscene button is hidden
    }

    public void HandleChoiceSelected(DialogueChoice choice)
    {
        if (currentNPC == null || currentData == null || choice == null) 
        { 
            Debug.LogError("[DialogueManager] HandleChoiceSelected: NPC, Data, or Choice is null.");
            if (isInCutsceneDialogueMode) { ForceEndCutsceneDialogue(); currentCutsceneSegmentCompletionCallback?.Invoke(); }
            else { EndDialogue(); }
            return; 
        }
        Debug.Log($"[DialogueManager] Choice selected: '{choice.choiceText}' -> Next Node: '{choice.nextNodeID}'");

        if (!isInCutsceneDialogueMode) // Only apply love changes in normal dialogue
        {
            ApplyLovePointChange(choice.lovePointChange, choice);
            UpdateFinalCutsceneButtonState(); 
        }
        if (!string.IsNullOrEmpty(choice.triggerCutsceneName))
        {
            Debug.Log($"[DialogueManager] Choice '{choice.choiceText}' triggers cutscene by name: {choice.triggerCutsceneName}");
            Cutscene cutsceneToPlay = Resources.Load<Cutscene>($"Cutscenes/{choice.triggerCutsceneName}");
            if (cutsceneToPlay == null)
            {
                Debug.LogError($"[DialogueManager] Failed to load Cutscene '{choice.triggerCutsceneName}' from Resources. Make sure it exists and the path is correct.");
                if (!isInCutsceneDialogueMode) EndDialogue();
                return;
            }



            if (CutsceneManager.Instance != null)
            {
                // Store current NPC as it might be the instigator or involved.
                // The CutsceneManager will handle pausing/resuming based on its own logic.
                BaseNPC instigatorNPC = currentNPC;

                // End the current dialogue UI *before* starting the cutscene.
                // The cutscene might want to use the DialogueManager immediately.
                if (isInCutsceneDialogueMode)
                {
                    // If this choice in a cutscene dialogue triggers another cutscene,
                    // end the current segment properly.
                    Action callback = currentCutsceneSegmentCompletionCallback;
                    if (dialogueUI != null) dialogueUI.HideDialogue();
                    CleanUpCutsceneDialogueState();
                    // Note: The callback here might be tricky if the new cutscene
                    // needs to happen *before* this segment is considered fully "complete".
                    // For now, we assume the segment completes, then the new cutscene plays.
                    callback?.Invoke();
                }
                else
                {
                    // For normal dialogue, prepare for handoff similar to OnFinalCutsceneButtonClick
                    PrepareForCutsceneHandoff(); // Hides UI, clears some state
                }

                CutsceneManager.Instance.StartCutscene(cutsceneToPlay, instigatorNPC);
            }
            else
            {
                Debug.LogError($"[DialogueManager] CutsceneManager.Instance is null. Cannot trigger cutscene by name: '{choice.triggerCutsceneName}'. (Loaded cutscene object was: {(cutsceneToPlay != null ? cutsceneToPlay.name : "null/not loaded")})");
                // Fallback: proceed with nextNodeID if cutscene manager is missing? Or just end?
                // For now, let's assume if a cutscene is set, it's crucial.
                // If not in cutscene mode, maybe EndDialogue() is appropriate.
                if (!isInCutsceneDialogueMode) EndDialogue();
            }
            return; // Do not proceed to nextNodeID logic if a cutscene is triggered
        }

        bool isEndOfCutsceneSegment = string.IsNullOrEmpty(choice.nextNodeID) || choice.nextNodeID == "CUTSCENE_SEGMENT_END";

        if (isInCutsceneDialogueMode && isEndOfCutsceneSegment)
        {
            Debug.Log($"[DialogueManager] Cutscene dialogue segment ended by choice '{choice.choiceText}'.");
            Action callback = currentCutsceneSegmentCompletionCallback;
            if (dialogueUI != null) dialogueUI.HideDialogue(); 
            CleanUpCutsceneDialogueState(); 
            callback?.Invoke(); 
            return; 
        }

        if (string.IsNullOrEmpty(choice.nextNodeID) || !currentData.nodeDictionary.TryGetValue(choice.nextNodeID, out DialogueNode nextNode) || nextNode == null)
        {
            Debug.Log($"[DialogueManager] Next node ID ('{choice.nextNodeID ?? "null"}') invalid or missing for current node '{currentNode?.nodeID}'.");
            if (isInCutsceneDialogueMode) 
            {
                Action callback = currentCutsceneSegmentCompletionCallback;
                if (dialogueUI != null) dialogueUI.HideDialogue();
                CleanUpCutsceneDialogueState();
                callback?.Invoke();
            }
            else
            {
                EndDialogue(); 
            }
        }
        else
        {
            currentNode = nextNode;
            if (dialogueUI != null)
            {
                dialogueUI.ShowDialogue(this, currentNode, currentNPC.npcName, currentNPC.npcImage);
            }
            else
            {
                Debug.LogError("[DialogueManager] DialogueUI reference missing, cannot show next node.");
                if (isInCutsceneDialogueMode) { ForceEndCutsceneDialogue(); currentCutsceneSegmentCompletionCallback?.Invoke(); } 
                else { EndDialogue(); }
            }
        }
    }

    private void CleanUpCutsceneDialogueState()
    {
        // currentNPC is managed by CutsceneManager during cutscenes, so don't null it here.
        // currentData was specific to the segment, so it's fine to null.
        currentData = null; 
        currentNode = null;
        isInCutsceneDialogueMode = false;
        currentCutsceneSegmentCompletionCallback = null;
        Debug.Log("[DialogueManager] Cleaned up cutscene dialogue state.");
    }

    public bool IsDialogueActiveForCutscene()
    {
        return isInCutsceneDialogueMode;
    }

    public void ForceEndCutsceneDialogue()
    {
        if (isInCutsceneDialogueMode)
        {
            Debug.Log("[DialogueManager] Forcefully ending cutscene dialogue segment.");
            if (dialogueUI != null) dialogueUI.HideDialogue();
            CleanUpCutsceneDialogueState();
            // Don't invoke callback here as this is a force stop from outside
        }
    }

    private void UpdateFinalCutsceneButtonState()
    {
        if (dialogueUI == null || currentNPC == null || isInCutsceneDialogueMode) // Don't show if in cutscene dialogue
        {
            if(dialogueUI != null) dialogueUI.SetFinalCutsceneButtonVisibility(false, null); 
            return;
        }

        if (currentNPC.currentLove >= FINAL_CUTSCENE_LOVE_THRESHOLD)
        {
            dialogueUI.SetFinalCutsceneButtonVisibility(true, OnFinalCutsceneButtonClick);
        }
        else
        {
            dialogueUI.SetFinalCutsceneButtonVisibility(false, null);
        }
    }

    private void OnFinalCutsceneButtonClick()
    {
        if (currentNPC == null)
        {
            Debug.LogError("[DialogueManager] Final Cutscene button clicked, but currentNPC is null!");
            return;
        }
        if (CutsceneManager.Instance != null && CutsceneManager.Instance.IsCutscenePlaying)
        {
            Debug.LogWarning("[DialogueManager] Final Cutscene button clicked while a cutscene is already playing. Ignoring.");
            return;
        }
        // isInCutsceneDialogueMode check is also good here.

        Debug.Log($"[DialogueManager] FINAL CUTSCENE BUTTON CLICKED for {currentNPC.npcName}. Love: {currentNPC.currentLove}");

        BaseNPC npcForCutscene = currentNPC; // Store reference

        // Important: Prepare DialogueManager for the handoff before the NPC tries to start the cutscene.
        // The cutscene might immediately want to use DialogueManager for a segment.
        PrepareForCutsceneHandoff();

        if (npcForCutscene != null)
        {
            // The NPC's method will be responsible for finding its specific cutscene asset
            // and then calling CutsceneManager.Instance.StartCutscene(...)
            npcForCutscene.TriggerFinalCutscene();
        }
        else
        {
            Debug.LogError("[DialogueManager] npcForCutscene was null after assignment. This shouldn't happen.");
            // Potentially re-enable dialogue UI if handoff prepared but cutscene won't start
        }
    }

    private void PrepareForCutsceneHandoff()
    {
        Debug.Log("[DialogueManager] Preparing for cutscene handoff.");
        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue(); // Hides main dialogue UI, including choices and cutscene button
        }
        if (statsButton != null)
        {
            statsButton.onClick.RemoveAllListeners();
        }
        if (npcStatPage != null)
        {
            npcStatPage.HideStatsPanel();
        }
        // Player movement is locked by CutsceneManager.StartCutscene
        // NPC is paused by CutsceneManager.StartCutscene
        // currentNPC is kept for CutsceneManager to use; currentData and currentNode for normal dialogue are implicitly no longer relevant.
    }

    public bool IsChoiceAvailable(DialogueChoice choice)
    {
        if (choice == null) return false;
        // Check 1: No item gate at all OR itemName is empty/null
        if (choice.itemGate == null || string.IsNullOrEmpty(choice.itemGate.itemName)) return true;

        // Check 2: Inventory instance exists
        if (Inventory.Instance == null)
        {
            Debug.LogError("[DialogueManager] Inventory instance is null! Cannot check item gate.");
            return false; // Cannot make choice if inventory system is broken
        }

        // Check 3: The actual item check
        bool hasItem = Inventory.Instance.HasItem(choice.itemGate.itemName);
        if (!hasItem)
        {
            Debug.Log($"[DialogueManager] Choice '{choice.choiceText}' gated by item '{choice.itemGate.itemName}', which player does NOT have.");
        }
        else
        {
            Debug.Log($"[DialogueManager] Choice '{choice.choiceText}' gated by item '{choice.itemGate.itemName}', which player HAS.");
        }
        return hasItem;
    }

    public BaseNPC GetCurrentNPC() => currentNPC;

    private string FindStartNodeIDForLove(int lovePoints, DialogueData data)
    {
        if (data == null) return null;
        if (data.loveTiers != null && data.loveTiers.Length > 0)
        {
            // Iterate in reverse in case tiers overlap, to pick the "highest" matching tier
            for (int i = data.loveTiers.Length - 1; i >= 0; i--)
            {
                var tier = data.loveTiers[i];
                if (tier != null && lovePoints >= tier.minLove && lovePoints <= tier.maxLove && !string.IsNullOrEmpty(tier.startNodeID)) 
                { 
                    return tier.startNodeID; 
                }
            }
        }
        return data.startNodeID; // Fallback to general start node
    }

    private void SetupStatsButton()
    {
        if (statsButton != null)
        {
            statsButton.onClick.RemoveAllListeners();
            statsButton.onClick.AddListener(OnStatsButtonClick);
        }
        // Removed redundant log and error, covered by Awake check
    }

    private void OnStatsButtonClick()
    {
        Debug.Log("[DialogueManager] Stats Button clicked.");
        if (npcStatPage != null)
        {
            npcStatPage.ToggleStatsPanel();
        }
        else
        {
            Debug.LogWarning("[DialogueManager] Stats Button clicked, but NPCStatPage reference is missing.");
        }
    }

    private void ApplyLovePointChange(int changeAmount, DialogueChoice choice)
    {
        if (currentNPC == null) return;
        currentNPC.currentLove += changeAmount;
        Debug.Log($"[DialogueManager] Applied {changeAmount} love. New total for {currentNPC.npcName}: {currentNPC.currentLove}. Choice: '{choice.choiceText}'");
    }

    // This method seems unused based on current logic flow, consider removing if not needed.
    // private bool RequiresItemToProceed(int currentLove) { return (currentLove == 3 || currentLove == 6 || currentLove == 9); }

    private void LockPlayerMovement() { if (playerMovement != null) playerMovement.LockMovement(); else Debug.LogWarning("[DialogueManager] PlayerMovement script not found on Lock."); }

    private void UnlockPlayerMovement() { if (playerMovement != null) playerMovement.UnlockMovement(); else Debug.LogWarning("[DialogueManager] PlayerMovement script not found on Unlock."); }

    private void CleanupFailedDialogueStart()
    {
        Debug.LogError("[DialogueManager] Cleaning up after failed dialogue start.");
        UnlockPlayerMovement(); // Ensure player is not stuck
        
        // Clear state
        currentNPC = null;
        currentData = null;
        currentNode = null;
        isInCutsceneDialogueMode = false; // Should not be true here, but reset just in case
        currentCutsceneSegmentCompletionCallback = null;

        if (dialogueUI != null) dialogueUI.HideDialogue();
        if (npcStatPage != null) npcStatPage.HideStatsPanel();
    }
}