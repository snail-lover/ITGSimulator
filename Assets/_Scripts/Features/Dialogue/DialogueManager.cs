using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System; // Required for Action
using Game.Core;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    // The prefab path in the Resources folder
    [Header("UI Setup")]
    [Tooltip("Drag the DialogueUI Prefab here from your project folders.")]
    [SerializeField] private GameObject dialogueUIPrefab;   // was DialogueUI
    private IDialogueUIRoot dialogueUI;                   // interface only
    private GameObject dialogueUIObject;

    [SerializeField] private Sprite questIcon, loveIcon, contextualIcon, generalIcon;

    private IDialogueParticipant currentParticipant;
    private DialogueData currentData;
    private DialogueNode currentNode;
    private Action onDialogueCompleteCallback;

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
        DialogueSignals.ForceCloseRequested += ForceCloseDialogueUI;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DialogueSignals.ForceCloseRequested -= ForceCloseDialogueUI;  // << add
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


    public void StartDialogue(IDialogueParticipant participant, string startNodeID = null, Action onCompleteCallback = null)
    {
        if (isDialogueActive)
        {
            Debug.LogWarning("[DialogueManager] StartDialogue ignored; dialogue already active.");
            return;
        }

        if (!EnsureDialogueUIExists())
        {
            onCompleteCallback?.Invoke();
            return;
        }

        isDialogueActive = true;
        currentParticipant = participant;
        this.onDialogueCompleteCallback = onCompleteCallback;
        LockPlayerMovement();

        TextAsset dialogueFile = participant.GetDialogueDataFile();
        if (dialogueFile == null)
        {
            Debug.LogError("[DialogueManager] Missing dialogue data file.");
            EndDialogue();
            return;
        }

        try
        {
            currentData = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            currentData.nodeDictionary = new Dictionary<string, DialogueNode>();
            foreach (var n in currentData.nodes)
                if (n != null && !string.IsNullOrEmpty(n.nodeID))
                    currentData.nodeDictionary[n.nodeID] = n;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueManager] Parse error: {e.Message}");
            EndDialogue();
            return;
        }

        if (!string.IsNullOrEmpty(startNodeID))
        {
            if (!currentData.nodeDictionary.TryGetValue(startNodeID, out currentNode))
            {
                Debug.LogError($"[DialogueManager] Start node '{startNodeID}' not found.");
                EndDialogue();
                return;
            }
        }
        else
        {
            currentNode = BuildGreetingNode(); // your existing helper
        }

        var view = BuildView(currentNode, isGreeting: string.IsNullOrEmpty(startNodeID));
        dialogueUI.ShowDialogue(view);
    }







    // Create a new helper to centralize showing content on the UI
    private void DisplayNode(DialogueNode node)
    {
        currentNode = node;
        var view = BuildView(node, isGreeting: false);
        dialogueUI.UpdateDialogueContent(view);
    }



    public void ForceCloseDialogueUI()
    {
        Debug.Log("[DialogueManager] Dialogue is being force-closed by an external system.");
        EndDialogue();
    }

    public void EndDialogue()
    {
        if (!isDialogueActive) return;
        isDialogueActive = false;
        UnlockPlayerMovement();
        onDialogueCompleteCallback?.Invoke();

        if (dialogueUI != null)
            dialogueUI.HideDialogue(DestroyDialogueUI);
        else
            DestroyDialogueUI();
    }


    private DialogueNode BuildGreetingNode()
    {
        _questTopics.Clear();
        _loveTopics.Clear();
        _contextualTopics.Clear();
        _generalTopics.Clear();

        // Your existing logic for looping through entry points and filtering them is perfect.
        foreach (var entryPoint in currentData.entryPoints)
        {
            if (IsEntryPointAvailable(entryPoint))
            {
                var topicAsChoice = new DialogueChoice { choiceText = entryPoint.entryText, nextNodeID = entryPoint.startNodeID };
                switch (entryPoint.entryType)
                {
                    case DialogueEntryPointType.Quest: _questTopics.Add(topicAsChoice); break;
                    // ... etc. for all your topic types ...
                    case DialogueEntryPointType.General: _generalTopics.Add(topicAsChoice); break;
                }
            }
        }

        // Your existing logic for combining the topic lists into a final choice list is also perfect.
        var finalTopics = new List<DialogueChoice>();
        finalTopics.AddRange(_questTopics);
        // ... etc. for combining contextual and general topics ...

        // Get the greeting text from the data file.
        string greetingText = "(They look at you expectantly.)";
        if (!string.IsNullOrEmpty(currentData.greetingNodeID) && currentData.nodeDictionary.TryGetValue(currentData.greetingNodeID, out DialogueNode greetingNode))
        {
            greetingText = greetingNode.text;
        }

        // Create a temporary "node" on the fly to hold the greeting text and the topic choices.
        return new DialogueNode { text = greetingText, choices = finalTopics.ToArray() };
    }

    /// <summary>
    /// Handles the logic when a player clicks a dialogue choice button.
    /// This version is simplified to only handle standard, non-cutscene conversations.
    /// </summary>
    public void HandleChoiceSelected(DialogueChoice choice)
    {
        // --- 1. Initial Validation ---
        if (currentParticipant == null || currentData == null || choice == null)
        {
            Debug.LogError("[DialogueManager] HandleChoiceSelected called with invalid state (Participant, Data, or Choice is null). Ending dialogue.");
            EndDialogue();
            return;
        }

        // --- 2. Handle Special "Topic Group" Choices ---
        // These choices don't lead to a real node but open a sub-menu of other topics.
        if (choice.nextNodeID == CONTEXTUAL_GROUP_ID)
        {
            var contextualGroupNode = new DialogueNode { text = "What were you wondering about?", choices = _contextualTopics.ToArray() };
            DisplayNode(contextualGroupNode);
            return; // Stop processing, we've just opened a sub-menu.
        }

        if (choice.nextNodeID == GENERAL_GROUP_ID)
        {
            var generalGroupNode = new DialogueNode { text = "What is your question?", choices = _generalTopics.ToArray() };
            DisplayNode(generalGroupNode);
            return; // Stop processing, we've just opened a sub-menu.
        }

        // --- 3. Apply All Side-Effects of the Choice ---

        // Apply any generic world state flag changes.
        ApplyStateChanges(choice.stateChangesOnSelect);

        // Remove an item from inventory if required by the choice.
        if (choice.itemGate != null && choice.itemGate.removeItemOnSelect && !string.IsNullOrEmpty(choice.itemGate.requiredItemID))
        {
            WorldDataManager.Instance.RemoveItemFromInventory(choice.itemGate.requiredItemID);
        }


        // Trigger a hangout if specified by the choice.
        //if (choice.startsHangout && currentParticipant is IHangoutPartner hangoutPartner)
      //  {
        //    HangoutManager.Instance.StartHangout(hangoutPartner);
            // Starting a hangout immediately ends the current dialogue.
       //     return;
       // }

        // --- 4. Find and Display the Next Node ---
        // Check if the choice leads to a valid next node.
        if (string.IsNullOrEmpty(choice.nextNodeID) || !currentData.nodeDictionary.TryGetValue(choice.nextNodeID, out DialogueNode nextNode) || nextNode == null)
        {
            // If there's no valid next node, the conversation branch ends here.
            EndDialogue();
            return;
        }

        // If we found a valid next node, display it.
        currentNode = nextNode;
        DisplayNode(currentNode);
    }


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

    public IDialogueParticipant GetCurrentParticipant() => currentParticipant;

    private void LockPlayerMovement()
    {
        // Find a better way to get this reference later, but for now this works.
        FindFirstObjectByType<PlayerInput>()?.LockInput();
    }

    private void UnlockPlayerMovement()
    {
        FindFirstObjectByType<PlayerInput>()?.UnlockInput();
    }

    // The final, refactored method in DialogueManager.cs
    private bool EnsureDialogueUIExists()
    {
        if (dialogueUI != null) return true;

        if (dialogueUIPrefab == null)
        {
            Debug.LogError("[DialogueManager] Dialogue UI Prefab not assigned.", this);
            return false;
        }

        var canvasObj = GameObject.FindGameObjectWithTag("MainCanvas");
        if (!canvasObj)
        {
            Debug.LogError("[DialogueManager] No Canvas tagged 'MainCanvas' found.");
            return false;
        }

        dialogueUIObject = Instantiate(dialogueUIPrefab, canvasObj.transform);
        dialogueUI = dialogueUIObject.GetComponent<IDialogueUIRoot>();
        if (dialogueUI == null)
        {
            Debug.LogError("[DialogueManager] Prefab must have a component that implements IDialogueUIRoot.", dialogueUIObject);
            Destroy(dialogueUIObject);
            dialogueUIObject = null;
            return false;
        }

        return true;
    }

    private DialogueViewNode BuildView(DialogueNode node, bool isGreeting)
    {
        var view = new DialogueViewNode
        {
            speakerName = currentParticipant.GetParticipantName(),
            portrait = currentParticipant.GetParticipantPortrait(),
            text = node.text,
            isGreeting = isGreeting
        };

        if (node.choices != null)
        {
            foreach (var c in node.choices)
            {
                if (c == null || !IsChoiceAvailable(c)) continue;

                view.choices.Add(new DialogueViewChoice
                {
                    text = c.choiceText,
                    icon = ChooseIconFor(c, isGreeting),     // optional
                    onClick = () => HandleChoiceSelected(c)     // back to manager
                });
            }
        }
        return view;
    }


    private Sprite ChooseIconFor(DialogueChoice c, bool isGreeting)
    {
        if (!isGreeting) return null;
        if (c.nextNodeID == "__CONTEXTUAL_GROUP__" || c.nextNodeID == "__GENERAL_GROUP__") return null;

        // mirror your grouping logic (uses your cached lists)
        if (_questTopics.Exists(t => t.choiceText == c.choiceText && t.nextNodeID == c.nextNodeID)) return questIcon;
        if (_loveTopics.Exists(t => t.choiceText == c.choiceText && t.nextNodeID == c.nextNodeID)) return loveIcon;
        if (_contextualTopics.Exists(t => t.choiceText == c.choiceText && t.nextNodeID == c.nextNodeID)) return contextualIcon;
        return generalIcon;
    }



    private void DestroyDialogueUI()
    {
        if (dialogueUIObject != null)
        {
            Destroy(dialogueUIObject);  // no .gameObject on the interface
            dialogueUIObject = null;
            dialogueUI = null;
        }
    }

}