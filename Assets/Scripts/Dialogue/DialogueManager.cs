using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System; // Required for Action

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // The prefab path in the Resources folder
    private const string DialogueUIPrefabPath = "UI/Dialogue/DialogueUI_Prefab";
    private DialogueUI dialogueUIInstance; // Instance of the instantiated prefab

    private NpcController currentNPC;
    private DialogueData currentData;
    private DialogueNode currentNode;

    private PointAndClickMovement playerMovement;
    private Action currentCutsceneSegmentCompletionCallback;
    private bool isInCutsceneDialogueMode = false;
    private string _currentHighPriorityTriggerID;

    private const int FINAL_CUTSCENE_LOVE_THRESHOLD = 10;

    private bool isDialogueActive = false;

    // These lists will hold the available topics after filtering.
    private List<DialogueChoice> _questTopics = new List<DialogueChoice>();
    private List<DialogueChoice> _loveTopics = new List<DialogueChoice>();
    private List<DialogueChoice> _contextualTopics = new List<DialogueChoice>();
    private List<DialogueChoice> _generalTopics = new List<DialogueChoice>();

    public List<DialogueChoice> GetQuestTopics() => _questTopics;
    public List<DialogueChoice> GetLoveTopics() => _loveTopics;
    public List<DialogueChoice> GetContextualTopics() => _contextualTopics;
    public List<DialogueChoice> GetGeneralTopics() => _generalTopics;

    // Special node IDs for grouped topics
    private const string CONTEXTUAL_GROUP_ID = "__CONTEXTUAL_GROUP__";
    private const string GENERAL_GROUP_ID = "__GENERAL_GROUP__";



    public bool IsDialogueUIVisible => isDialogueActive;

    private void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); return; }

        // We no longer check for DialogueUI here, as it will be loaded on demand
        FindAndCachePlayerMovement();
    }

    public bool IsShowingGreetingDialogue()
    {
        // You could track this with a boolean flag, or determine it based on current state
        // For now, we can assume if we have categorized topics, we're showing greeting
        return _questTopics.Count > 0 || _loveTopics.Count > 0 ||
               _contextualTopics.Count > 0 || _generalTopics.Count > 0;
    }
    private bool IsEntryPointAvailable(DialogueEntryPoint entryPoint)
    {
        // --- GATE 1: Check all World State Conditions ---
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[DialogueManager] WorldDataManager is null, cannot check entry point conditions.");
            return true; // Fail open
        }

        if (entryPoint.worldStateConditions != null)
        {
            foreach (var condition in entryPoint.worldStateConditions)
            {
                if (string.IsNullOrEmpty(condition.conditionKey)) continue;

                if (WorldDataManager.Instance.GetGlobalFlag(condition.conditionKey) != condition.requiredValue)
                {
                    return false; // Condition failed.
                }
            }
        }

        // --- GATE 2: Check the Item Requirement (Corrected Logic) ---
        // Check if an item ID is actually specified.
        if (!string.IsNullOrEmpty(entryPoint.requiredItemID))
        {
            // Now, check if the player possesses the item with that ID.
            if (WorldDataManager.Instance == null || !WorldDataManager.Instance.PlayerHasItem(entryPoint.requiredItemID))
            {
                // The player does NOT have the required item. Block the entry point.
                return false;
            }
        }

        // --- SUCCESS ---
        return true;
    }

    private void FindAndCachePlayerMovement()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerMovement = playerObj.GetComponent<PointAndClickMovement>();
            if (playerMovement == null) Debug.LogError("[DialogueManager] Player missing PointAndClickMovement script!");
        }
        else
        {
            Debug.LogError("[DialogueManager] Player object with tag 'Player' not found!");
        }
    }

    public void StartDialogue(NpcController npc)
    {
        StartDialogue(npc, null, null);
    }

    public void StartDialogue(NpcController npc, string specificStartNodeID)
    {
        StartDialogue(npc, specificStartNodeID, null);
    }

    public void StartDialogue(NpcController npc, string specificStartNodeID = null, string uniqueTriggerID = null)
    {
        if (isDialogueActive) return;

        if (!EnsureDialogueUIExists())
        {
            Debug.LogError("[DialogueManager] Cannot start dialogue, UI failed to create.");
            return;
        }

        currentNPC = npc;
        currentData = npc.Interaction.GetDialogueData();
        if (currentData == null)
        {
            Debug.LogError($"[DialogueManager] Dialogue data for {npc.npcConfig.npcName} is missing.");
            EndDialogue();
            return;
        }

        _currentHighPriorityTriggerID = uniqueTriggerID;

        // --- THIS IS THE FIX ---
        // Mark the dialogue as completed the moment it successfully starts.
        // This prevents re-triggers even if the player quits during the conversation.
        if (!string.IsNullOrEmpty(uniqueTriggerID))
        {
            WorldDataManager.Instance.MarkHighPriorityDialogueAsCompleted(uniqueTriggerID);
            Debug.Log($"[DialogueManager] High-priority dialogue '{uniqueTriggerID}' is starting and has been marked as completed.");
        }
        // --- END OF FIX ---

        LockPlayerMovement();
        isDialogueActive = true;

        DialogueNode finalDisplayNode;
        bool isGreeting = false;

        if (!string.IsNullOrEmpty(specificStartNodeID))
        {
            if (currentData.nodeDictionary.TryGetValue(specificStartNodeID, out DialogueNode targetNode))
            {
                finalDisplayNode = targetNode;
                dialogueUIInstance.isShowingGreetingDialogue = false;
                isGreeting = false;
            }
            else
            {
                Debug.LogError($"[DialogueManager] High-priority dialogue failed. Node ID '{specificStartNodeID}' not found for {npc.npcConfig.npcName}.");
                EndDialogue();
                return;
            }
        }
        else
        {
            // This is the standard greeting/topic logic (no changes needed here)
            _questTopics.Clear();
            _loveTopics.Clear();
            _contextualTopics.Clear();
            _generalTopics.Clear();

            foreach (var entryPoint in currentData.entryPoints)
            {
                if (IsEntryPointAvailable(entryPoint))
                {
                    var topicAsChoice = new DialogueChoice { choiceText = entryPoint.entryText, nextNodeID = entryPoint.startNodeID };
                    switch (entryPoint.entryType)
                    {
                        case DialogueEntryPointType.Quest: _questTopics.Add(topicAsChoice); break;
                        case DialogueEntryPointType.Love: _loveTopics.Add(topicAsChoice); break;
                        case DialogueEntryPointType.Contextual: _contextualTopics.Add(topicAsChoice); break;
                        case DialogueEntryPointType.General: _generalTopics.Add(topicAsChoice); break;
                    }
                }
            }
            var finalTopics = new List<DialogueChoice>();
            finalTopics.AddRange(_questTopics);
            finalTopics.AddRange(_loveTopics);
            if (_contextualTopics.Count == 1) { finalTopics.Add(_contextualTopics[0]); }
            else if (_contextualTopics.Count > 1) { finalTopics.Add(new DialogueChoice { choiceText = "I was wondering about something...", nextNodeID = CONTEXTUAL_GROUP_ID }); }
            bool hasHigherPriorityOptions = (_questTopics.Count + _loveTopics.Count + _contextualTopics.Count) > 0;
            if (_generalTopics.Count > 0)
            {
                if (hasHigherPriorityOptions) { finalTopics.Add(new DialogueChoice { choiceText = "I have a general question...", nextNodeID = GENERAL_GROUP_ID }); }
                else { finalTopics.AddRange(_generalTopics); }
            }
            string greetingText = "(They look at you expectantly.)";
            if (!string.IsNullOrEmpty(currentData.greetingNodeID) && currentData.nodeDictionary.TryGetValue(currentData.greetingNodeID, out DialogueNode greetingNode))
            {
                greetingText = greetingNode.text;
            }

            if (finalTopics.Count > 0)
            {
                finalDisplayNode = new DialogueNode { text = greetingText, choices = finalTopics.ToArray() };
            }
            else
            {
                finalDisplayNode = new DialogueNode { text = greetingText, choices = new DialogueChoice[0] };
            }

            dialogueUIInstance.isShowingGreetingDialogue = true;
            isGreeting = true;
        }

        currentNode = finalDisplayNode;
        dialogueUIInstance.ShowDialogue(this, finalDisplayNode, currentNPC.npcConfig.npcName, currentNPC.npcConfig.npcImage, isGreeting);
        SetupStatsButton();
    }





    // Create a new helper to centralize showing content on the UI
    private void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        dialogueUIInstance.UpdateDialogueContent(this, node, currentNPC.npcConfig.npcName, currentNPC.npcConfig.npcImage);
    }

    public void EndDialogue()
    {
        _currentHighPriorityTriggerID = null;

        isDialogueActive = false;
        Debug.Log($"[DialogueManager] Ending Dialogue. isDialogueActive set to {isDialogueActive}.");
        if (isInCutsceneDialogueMode)
        {
            Debug.LogWarning("[DialogueManager] EndDialogue() called while in cutscene dialogue mode. Forcefully cleaning up. This should ideally be handled by CutsceneManager or segment completion.");
            ForceEndCutsceneDialogue();
            return;
        }

        NpcController npcThatWasInDialogue = currentNPC;
        Debug.Log($"[DialogueManager] Ending Dialogue with {(npcThatWasInDialogue != null ? npcThatWasInDialogue.npcConfig.npcName : "No one (or already ended)")}.");


        if (dialogueUIInstance != null)
        {
            dialogueUIInstance.HideDialogue(DestroyDialogueUI); // Hide UI, then destroy it via callback.
        }

        UnlockPlayerMovement();

        if (npcThatWasInDialogue != null)
        {
            Debug.Log($"[DialogueManager] Notifying {npcThatWasInDialogue.npcConfig.npcName} that dialogue has ended.");
            npcThatWasInDialogue.NpcDialogueEnded();
        }

        if ((object)PointAndClickMovement.currentTarget == npcThatWasInDialogue)
        {
            Debug.Log($"[DialogueManager] Clearing PointAndClickMovement.currentTarget ({((MonoBehaviour)PointAndClickMovement.currentTarget).name}) as dialogue has ended.");
            PointAndClickMovement.currentTarget = null;
        }

        currentNPC = null;
        currentData = null;
        currentNode = null;
        _questTopics.Clear();
        _loveTopics.Clear();
        _contextualTopics.Clear();
        _generalTopics.Clear();
        Debug.Log("--- DIALOGUE MANAGER: EndDialogue COMPLETE ---");
    }

    public void StartDialogueSegment(NpcController npc, TextAsset dialogueFile, string startNodeId, Action onSegmentComplete)
    {
        if (npc == null) { Debug.LogError("[DialogueManager] StartDialogueSegment: NPC is null."); onSegmentComplete?.Invoke(); return; }
        if (dialogueFile == null) { Debug.LogError("[DialogueManager] StartDialogueSegment: DialogueFile is null."); onSegmentComplete?.Invoke(); return; }
        if (string.IsNullOrEmpty(startNodeId)) { Debug.LogError("[DialogueManager] StartDialogueSegment: startNodeId is null or empty."); onSegmentComplete?.Invoke(); return; }

        if (!EnsureDialogueUIExists())
        {
            Debug.LogError("[DialogueManager] Cannot start dialogue segment because UI could not be created.");
            // Don't call CleanUpCutsceneDialogueState as state was never set.
            onSegmentComplete?.Invoke();
            return;
        }

        Debug.Log($"[DialogueManager] Starting Cutscene Dialogue Segment for {npc.npcConfig.npcName}, Node: {startNodeId}.");
        isInCutsceneDialogueMode = true;
        currentNPC = npc;
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
            }
            else
            {
                Debug.LogError($"[DialogueManager] 'nodes' array is null in cutscene dialogue {dialogueFile.name}.");
                throw new System.Exception("'nodes' array is null.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DialogueManager] Error parsing cutscene dialogue {dialogueFile.name}: {e.Message}");
            CleanUpCutsceneDialogueState(true); // Force destroy UI on error
            onSegmentComplete?.Invoke();
            return;
        }

        if (!currentData.nodeDictionary.TryGetValue(startNodeId, out currentNode) || currentNode == null)
        {
            Debug.LogError($"[DialogueManager] Cutscene start node '{startNodeId}' not found in {dialogueFile.name}.");
            CleanUpCutsceneDialogueState(true); // Force destroy UI on error
            onSegmentComplete?.Invoke();
            return;
        }

        // === MODIFIED LOGIC ===
        dialogueUIInstance.AnimateIn();
        dialogueUIInstance.UpdateDialogueContent(this, currentNode, currentNPC.npcConfig.npcName, currentNPC.npcConfig.npcImage);
        // ======================

        dialogueUIInstance.SetStatsButtonVisibility(false);

        isDialogueActive = true;
        dialogueUIInstance.SetFinalCutsceneButtonVisibility(false, null);
    }


    public void HandleChoiceSelected(DialogueChoice choice)
    {

        // The greeting phase ends once we select a choice that is NOT a special group.
        // This check runs before we do anything else with the choice.
        if (dialogueUIInstance != null &&
            choice.nextNodeID != CONTEXTUAL_GROUP_ID &&
            choice.nextNodeID != GENERAL_GROUP_ID)
        {
            dialogueUIInstance.isShowingGreetingDialogue = false;
        }


        // --- 1. Handle Special Dynamic Group Choices ---
        // These choices don't lead to a node in the data file but dynamically
        // generate a new menu. They must be handled first.
        if (choice.nextNodeID == CONTEXTUAL_GROUP_ID)
        {
            Debug.Log("[DialogueManager] Displaying contextual topics sub-menu.");
            var contextualGroupNode = new DialogueNode
            {
                text = "What were you wondering about?", // You can customize this text
                choices = _contextualTopics.ToArray()
            };
            DisplayNode(contextualGroupNode); // Display the temporary, dynamic node
            return; // Stop processing
        }

        if (choice.nextNodeID == GENERAL_GROUP_ID)
        {
            Debug.Log("[DialogueManager] Displaying general topics sub-menu.");
            var generalGroupNode = new DialogueNode
            {
                text = "What is your question?", // You can customize this text
                choices = _generalTopics.ToArray()
            };
            DisplayNode(generalGroupNode);
            return;// Stop processing
        }

        // --- 2. Standard Dialogue/Cutscene Progression (from OLD version) ---

        // Initial validation
        if (currentNPC == null || currentData == null || choice == null)
        {
            Debug.LogError("[DialogueManager] HandleChoiceSelected: NPC, Data, or Choice is null.");
            // Use the appropriate cleanup method based on the current mode
            if (isInCutsceneDialogueMode) { ForceEndCutsceneDialogue(); currentCutsceneSegmentCompletionCallback?.Invoke(); }
            else { EndDialogue(); }
            return;
        }

        // Capture the NPC who spoke for consistent logging and cleanup
        NpcController npcThatSpokeThisLine = currentNPC;
        //Debug.Log($"[DialogueManager] Choice selected: '{choice.choiceText}' -> Next Node: '{choice.nextNodeID}'");

        // Apply side-effects only in normal dialogue mode
        if (!isInCutsceneDialogueMode)
        {
            ApplyLovePointChange(choice.lovePointChange, choice);
            ApplyStateChanges(choice.stateChangesOnSelect);

            // --- FIX ITEM REMOVAL LOGIC ---
            if (choice.itemGate != null && choice.itemGate.removeItemOnSelect && !string.IsNullOrEmpty(choice.itemGate.requiredItemID))
            {
                // Tell WorldDataManager to remove the item using its stable ID.
                WorldDataManager.Instance.RemoveItemFromInventory(choice.itemGate.requiredItemID);
            }

            // --- FIX ITEM GIVING LOGIC ---
            if (!string.IsNullOrEmpty(choice.itemToGiveID))
            {
                // Look up the full item from the database using its ID
                CreateInventoryItem itemSO = ItemDatabase.Instance.GetItemByID(choice.itemToGiveID);
                if (itemSO != null && Inventory.Instance != null)
                {
                    // Give the found item to the player
                    Inventory.Instance.AddItem(itemSO);
                    Debug.Log($"[DialogueManager] Gave item '{itemSO.itemName}' (ID: {choice.itemToGiveID}) to player.");
                }
                else
                {
                    Debug.LogWarning($"[DialogueManager] Tried to give item with ID '{choice.itemToGiveID}', but it was not found in the ItemDatabase or Inventory.Instance is null.");
                }
            }

            UpdateFinalCutsceneButtonState();
        }

        // Handle choices that trigger a new cutscene
        if (!string.IsNullOrEmpty(choice.triggerCutsceneName))
        {
            Debug.Log($"[DialogueManager] Choice '{choice.choiceText}' triggers cutscene: {choice.triggerCutsceneName}");
            Cutscene cutsceneToPlay = Resources.Load<Cutscene>($"Cutscenes/{choice.triggerCutsceneName}");
            if (cutsceneToPlay == null)
            {
                Debug.LogError($"[DialogueManager] Failed to load Cutscene '{choice.triggerCutsceneName}' from Resources.");
                if (!isInCutsceneDialogueMode) EndDialogue();
                // If in a cutscene, let it continue to the next node if one is defined, or end if not.
                // For safety, we can just end the segment.
                else { ForceEndCutsceneDialogue(); currentCutsceneSegmentCompletionCallback?.Invoke(); }
                return;
            }

            if (CutsceneManager.Instance != null)
            {
                // Clean up the current dialogue state before handing off to the CutsceneManager
                if (isInCutsceneDialogueMode)
                {
                    // We're in a cutscene dialogue, and it's triggering a *new* cutscene.
                    // End the current segment first.
                    Action callback = currentCutsceneSegmentCompletionCallback;
                    if (dialogueUIInstance != null) dialogueUIInstance.HideDialogue(DestroyDialogueUI);
                    CleanUpCutsceneDialogueState();
                    npcThatSpokeThisLine?.NpcDialogueEnded();
                    callback?.Invoke(); // Let the current cutscene segment know it's done.
                }
                else
                {
                    // We're in a normal dialogue. End it to start the cutscene.
                    PrepareForCutsceneHandoff();
                    npcThatSpokeThisLine?.NpcDialogueEnded();
                }

                CutsceneManager.Instance.StartCutscene(cutsceneToPlay, npcThatSpokeThisLine);
            }
            else
            {
                Debug.LogError("[DialogueManager] CutsceneManager.Instance is null. Cannot trigger cutscene.");
                if (!isInCutsceneDialogueMode) EndDialogue();
            }
            return; // The cutscene trigger is a terminal action for this method.
        }

        // Check if this choice ends the current cutscene dialogue segment
        bool isEndOfCutsceneSegment = string.IsNullOrEmpty(choice.nextNodeID) || choice.nextNodeID == "CUTSCENE_SEGMENT_END";
        if (isInCutsceneDialogueMode && isEndOfCutsceneSegment)
        {
            Debug.Log($"[DialogueManager] Cutscene dialogue segment ended by choice for {npcThatSpokeThisLine?.name}.");
            Action callback = currentCutsceneSegmentCompletionCallback;
            if (dialogueUIInstance != null) dialogueUIInstance.HideDialogue(DestroyDialogueUI);
            CleanUpCutsceneDialogueState();
            npcThatSpokeThisLine?.NpcDialogueEnded();
            callback?.Invoke();
            return;
        }

        // Find and display the next node, or end the dialogue if the node is invalid/not found.
        if (string.IsNullOrEmpty(choice.nextNodeID) || !currentData.nodeDictionary.TryGetValue(choice.nextNodeID, out DialogueNode nextNode) || nextNode == null)
        {
            //Debug.Log($"[DialogueManager] Dialogue with {npcThatSpokeThisLine?.name} ends here. (Next node ID: '{choice.nextNodeID}' is null, empty, or invalid).");
            if (isInCutsceneDialogueMode)
            {
                // Invalid next node during a cutscene is treated as the end of the segment.
                Action callback = currentCutsceneSegmentCompletionCallback;
                if (dialogueUIInstance != null) dialogueUIInstance.HideDialogue(DestroyDialogueUI);
                CleanUpCutsceneDialogueState();
                npcThatSpokeThisLine?.NpcDialogueEnded();
                callback?.Invoke();
            }
            else
            {
                // End of a normal conversation branch.
                EndDialogue();
            }
            return;
        }

        currentNode = nextNode;
        if (dialogueUIInstance != null)
        {
            // === MODIFIED LOGIC ===
            // We now call UpdateDialogueContent, which does NOT re-trigger the show animation.
            dialogueUIInstance.UpdateDialogueContent(this, currentNode, npcThatSpokeThisLine.npcConfig.npcName, npcThatSpokeThisLine.npcConfig.npcImage);
            // ======================
        }
        else
        {
            Debug.LogError("[DialogueManager] DialogueUI instance missing, cannot show next node.");
            if (isInCutsceneDialogueMode) { ForceEndCutsceneDialogue(); currentCutsceneSegmentCompletionCallback?.Invoke(); }
            else { EndDialogue(); }
        }
    }


    // In DialogueManager.cs, add this new private method

    private void ApplyStateChanges(List<WorldStateChange> stateChanges)
    {
        if (stateChanges == null || stateChanges.Count == 0) return;

        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[DialogueManager] WorldDataManager is null. Cannot apply state changes.");
            return;
        }

        foreach (var change in stateChanges)
        {
            if (!string.IsNullOrEmpty(change.stateKey))
            {
                // The WorldDataManager already logs this, so we don't need to double-log.
                WorldDataManager.Instance.SetGlobalFlag(change.stateKey, change.stateValue);
            }
        }
    }

    private void CleanUpCutsceneDialogueState(bool forceDestroyUI = false)
    {
        Debug.Log($"[DialogueManager] Cleaning up cutscene dialogue state. DM.currentNPC was: {currentNPC?.name}");
        if (forceDestroyUI) DestroyDialogueUI(); // Immediately destroy if needed for an error case.

        currentNPC = null;
        currentData = null;
        currentNode = null;
        isInCutsceneDialogueMode = false;
        currentCutsceneSegmentCompletionCallback = null;
        isDialogueActive = false;
    }

    public bool IsDialogueActiveForCutscene()
    {
        return isInCutsceneDialogueMode;
    }

    public void ForceEndCutsceneDialogue()
    {
        if (isInCutsceneDialogueMode)
        {
            Debug.Log($"[DialogueManager] Forcefully ending cutscene dialogue segment for {currentNPC?.name}.");
            NpcController npcFromCutsceneSegment = currentNPC;

            if (dialogueUIInstance != null) dialogueUIInstance.HideDialogue(DestroyDialogueUI);
            CleanUpCutsceneDialogueState();

            if (npcFromCutsceneSegment != null)
            {
                Debug.Log($"[DialogueManager] Notifying {npcFromCutsceneSegment.name} (due to force end) that its cutscene dialogue segment has ended.");
                npcFromCutsceneSegment.NpcDialogueEnded();
            }
        }
    }

    private void UpdateFinalCutsceneButtonState()
    {
        if (dialogueUIInstance == null || currentNPC == null || isInCutsceneDialogueMode)
        {
            if (dialogueUIInstance != null) dialogueUIInstance.SetFinalCutsceneButtonVisibility(false, null);
            return;
        }

        if (currentNPC.runtimeData.currentLove >= FINAL_CUTSCENE_LOVE_THRESHOLD)
        {
            dialogueUIInstance.SetFinalCutsceneButtonVisibility(true, OnFinalCutsceneButtonClick);
        }
        else
        {
            dialogueUIInstance.SetFinalCutsceneButtonVisibility(false, null);
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

        Debug.Log($"[DialogueManager] FINAL CUTSCENE BUTTON CLICKED for {currentNPC.npcConfig.npcName}. Love: {currentNPC.runtimeData.currentLove}");

    }

    private void PrepareForCutsceneHandoff()
    {
        Debug.Log("[DialogueManager] Preparing for cutscene handoff.");
        if (dialogueUIInstance != null)
        {
            dialogueUIInstance.HideDialogue(DestroyDialogueUI);
        }
    }

    public bool IsChoiceAvailable(DialogueChoice choice)
    {
        if (choice == null) return false;

        // 1. Check if the choice has a required item gate
        if (choice.itemGate != null && !string.IsNullOrEmpty(choice.itemGate.requiredItemID))
        {
            // Ask the WorldDataManager if the player has the item with this specific ID.
            if (WorldDataManager.Instance == null || !WorldDataManager.Instance.PlayerHasItem(choice.itemGate.requiredItemID))
            {
                return false; // Player doesn't have the required item.
            }
        }

        // 2. Check if the choice requires an item with a specific tag.
        if (choice.requiredTag != ItemTag.None)
        {
            if (Inventory.Instance == null || !Inventory.Instance.HasItemWithTag(choice.requiredTag))
            {
                return false;
            }
        }

        // Ensure WorldDataManager exists
        if (WorldDataManager.Instance == null)
        {
            Debug.LogError("[DialogueManager] WorldDataManager instance is null! Cannot check world state conditions.");
            return true; // Fail open: show the choice but log an error
        }

        // 3. Check conditions on the CHOICE itself
        if (choice.worldStateConditions != null)
        {
            foreach (var condition in choice.worldStateConditions)
            {
                if (string.IsNullOrEmpty(condition.conditionKey)) continue;

                bool actualState = WorldDataManager.Instance.GetGlobalFlag(condition.conditionKey);
                bool requiredState = condition.requiredValue;

                if (actualState != requiredState)
                {
                    return false;
                }
            }
            return true;
        }

        // 4. Check conditions on the TARGET NODE (look-ahead)
        if (!string.IsNullOrEmpty(choice.nextNodeID) && currentData.nodeDictionary.TryGetValue(choice.nextNodeID, out DialogueNode nextNode))
        {
            if (nextNode.worldStateConditions != null)
            {
                foreach (var condition in nextNode.worldStateConditions)
                {
                    if (string.IsNullOrEmpty(condition.conditionKey)) continue;
                    bool actualState = WorldDataManager.Instance.GetGlobalFlag(condition.conditionKey);
                    bool requiredState = condition.requiredValue;

                    if (actualState != requiredState)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // If all checks passed, the choice is available
        return true;
    }

    public NpcController GetCurrentNPC() => currentNPC;

    private void SetupStatsButton()
    {
        if (dialogueUIInstance != null && dialogueUIInstance.StatsButton != null)
        {
            // Make the stats button visible for normal dialogue sessions.
            dialogueUIInstance.SetStatsButtonVisibility(true);

            Button uiStatsButton = dialogueUIInstance.StatsButton;
            uiStatsButton.onClick.RemoveAllListeners();

            // Connect the button's click event directly to the TogglePanel method on the NPCStatPage.
            if (dialogueUIInstance.NpcStatPage != null)
            {
                // --- THIS IS THE FIX ---
                // The NPCStatPage needs to know which NPC's stats to display.
                // We assign the DialogueManager's currentNPC to the stat page instance here.
                dialogueUIInstance.NpcStatPage.currentNPC = this.currentNPC;

                // Now that the stat page has the correct NPC, add the listener.
                uiStatsButton.onClick.AddListener(dialogueUIInstance.NpcStatPage.TogglePanel);
            }
        }
    }

    private void ApplyLovePointChange(int changeAmount, DialogueChoice choice)
    {
        if (currentNPC == null) return;
        currentNPC.runtimeData.currentLove += changeAmount;
        //Debug.Log($"[DialogueManager] Applied {changeAmount} love. New total for {currentNPC.npcConfig.npcName}: {currentNPC.runtimeData.currentLove}. Choice: '{choice.choiceText}'");
    }

    private void LockPlayerMovement() { if (playerMovement != null) playerMovement.HardLockPlayerMovement(); else Debug.LogWarning("[DialogueManager] PlayerMovement script not found on Lock."); }

    private void UnlockPlayerMovement() { if (playerMovement != null) playerMovement.HardUnlockPlayerMovement(); else Debug.LogWarning("[DialogueManager] PlayerMovement script not found on Unlock."); }

    private void CleanupFailedDialogueStart()
    {
        Debug.LogError("[DialogueManager] Cleaning up after failed dialogue start.");
        UnlockPlayerMovement();

        DestroyDialogueUI(); // Destroy UI instance if it was created

        currentNPC = null;
        currentData = null;
        currentNode = null;
        isInCutsceneDialogueMode = false;
        currentCutsceneSegmentCompletionCallback = null;
        isDialogueActive = false;
    }

    // --- NEW HELPER METHODS ---

    private bool EnsureDialogueUIExists()
    {
        if (dialogueUIInstance != null) return true;

        Debug.Log("[DialogueManager] DialogueUI instance not found, attempting to instantiate from Resources.");
        var dialogueUIPrefab = Resources.Load<DialogueUI>(DialogueUIPrefabPath);

        if (dialogueUIPrefab == null)
        {
            Debug.LogError($"[DialogueManager] Failed to load DialogueUI prefab from path: 'Resources/{DialogueUIPrefabPath}'. Make sure it exists and the path is correct.");
            return false;
        }

        Canvas mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("[DialogueManager] No Canvas found in the scene to parent the DialogueUI to. Please ensure a Canvas exists.");
            return false;
        }

        dialogueUIInstance = Instantiate(dialogueUIPrefab, mainCanvas.transform);
        if (dialogueUIInstance == null)
        {
            Debug.LogError("[DialogueManager] Failed to instantiate the DialogueUI prefab.");
            return false;
        }

        //Debug.Log("[DialogueManager] Successfully instantiated DialogueUI prefab.");
        return true;
    }

    private void DestroyDialogueUI()
    {
        if (dialogueUIInstance != null)
        {
            //Debug.Log("[DialogueManager] Destroying DialogueUI instance.");
            Destroy(dialogueUIInstance.gameObject);
            dialogueUIInstance = null;
        }
    }
}