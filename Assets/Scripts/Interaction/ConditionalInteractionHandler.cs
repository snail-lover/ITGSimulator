// ConditionalInteractionHandler.cs (Updated)

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// NEW: An enum to make it clear in the Inspector what kind of condition we are checking.
public enum ConditionType
{
    WorldState,
    HasItemByID,
    HasItemWithTag
}

// MODIFIED: The InteractionCondition class is now much more powerful.
[System.Serializable]
public class InteractionCondition
{
    [Tooltip("Just for organizing in the Inspector, doesn't affect gameplay.")]
    public string conditionName = "New Condition";

    [Header("Condition To Check")]
    [Tooltip("What type of condition is this? A world flag, or an item in the player's inventory?")]
    public ConditionType checkType = ConditionType.WorldState;

    [Tooltip("The state key from WorldDataManager to check (Used if Check Type is WorldState).")]
    public string requiredStateKey;
    [Tooltip("The value the state must have for this interaction to trigger (Used if Check Type is WorldState).")]
    public bool requiredStateValue = true;

    [Tooltip("The unique ID of the item the player must possess (Used if Check Type is HasItemByID).")]
    public string requiredItemID;
    [Tooltip("The tag the player's item must have (Used if Check Type is HasItemWithTag).")]
    public ItemTag requiredItemTag;


    [Header("Outcome if Condition is Met")]
    [Tooltip("OPTIONAL: A cutscene to play if the condition is met. This will play INSTEAD of the popup message.")]
    public Cutscene cutsceneToPlay;

    [TextArea(3, 10)]
    [Tooltip("The message to display in the popup if this condition is met (and no cutscene is assigned).")]
    public string popupMessage;

    [Tooltip("The sound to play for this specific outcome.")]
    public AudioClip soundEffect;

    [Tooltip("OPTIONAL: If this condition was based on an item, should that item be removed from the inventory?")]
    public bool consumeItemOnSuccess = false;

    [Tooltip("OPTIONAL: A new world state to set after this interaction occurs.")]
    public string stateToSetOnSuccess;
    [Tooltip("The value to set the new state to.")]
    public bool valueToSet = true;
}

// UNCHANGED: DefaultInteraction remains the same.
[System.Serializable]
public class DefaultInteraction
{
    [TextArea(3, 10)]
    [Tooltip("The message to display if no other conditions are met.")]
    public string popupMessage = "Nothing happens.";
    [Tooltip("The sound to play for the default outcome.")]
    public AudioClip soundEffect;

    [Header("Default Outcome State Change")]
    [Tooltip("OPTIONAL: A world state to set when this default interaction occurs.")]
    public string stateToSetOnSuccess;
    [Tooltip("The value to set the new state to.")]
    public bool valueToSet = true;
}

public class ConditionalInteractionHandler : MonoBehaviour, IInteractableAction
{
    [Header("UI Popup")]
    [Tooltip("A UI prefab containing a TextMeshPro component. This will be instantiated on interact.")]
    public GameObject popupPrefab;

    [Header("Conditional Interactions")]
    [Tooltip("The list of possible interactions. Checked top to bottom. The first one that is met will be executed.")]
    public List<InteractionCondition> conditions = new List<InteractionCondition>();

    [Header("Default Interaction")]
    [Tooltip("Used if NONE of the conditions are met.")]
    public DefaultInteraction defaultInteraction;

    // Private references
    private Canvas mainCanvas;
    private GameObject activePopupInstance;
    private AudioSource audioSource;

    // Static reference to manage active popups globally
    private static ConditionalInteractionHandler activeHandler = null;

    private void Awake()
    {
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) { Debug.LogError($"[{gameObject.name}] Cannot find a Canvas in the scene!"); }
        if (popupPrefab == null) { Debug.LogError($"[{gameObject.name}] Missing its Popup Prefab."); }

        // Get or add an AudioSource for playing sounds
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }
    }

    private void Update()
    {
        if (activeHandler == this && activePopupInstance != null && Input.GetMouseButtonDown(0))
        {
            DismissPopup();
        }
    }

    /// <summary>
    /// This is the new entry point called by Interactable.cs when the player is in range.
    /// </summary>
    public void ExecuteAction()
    {
        EvaluateAndExecute();
    }

    /// <summary>
    /// Called by Interactable.cs if the interaction is cancelled.
    /// </summary>
    public void ResetAction()
    {
        DismissPopup();
    }

    /// <summary>
    /// REWRITTEN: This method now evaluates world states AND inventory conditions.
    /// </summary>
    public bool EvaluateAndExecute()
    {
        if (activeHandler != null && activeHandler != this)
        {
            activeHandler.DismissPopup();
        }

        if (WorldDataManager.Instance == null || Inventory.Instance == null)
        {
            Debug.LogError($"[{gameObject.name}] Cannot evaluate: WorldDataManager or Inventory not found!");
            return false;
        }

        // --- NEW LOGIC: Loop through conditions and check based on type ---
        foreach (var condition in conditions)
        {
            bool conditionMet = false;
            switch (condition.checkType)
            {
                case ConditionType.WorldState:
                    if (!string.IsNullOrEmpty(condition.requiredStateKey))
                    {
                        conditionMet = WorldDataManager.Instance.GetGlobalFlag(condition.requiredStateKey) == condition.requiredStateValue;
                    }
                    break;

                case ConditionType.HasItemByID:
                    if (!string.IsNullOrEmpty(condition.requiredItemID))
                    {
                        conditionMet = Inventory.Instance.HasItem(condition.requiredItemID);
                    }
                    break;

                case ConditionType.HasItemWithTag:
                    if (condition.requiredItemTag != ItemTag.None)
                    {
                        conditionMet = Inventory.Instance.HasItemWithTag(condition.requiredItemTag);
                    }
                    break;
            }

            if (conditionMet)
            {
                // We found a matching condition, execute its outcome and stop checking.
                ExecuteConditionalInteraction(condition);
                return true; // Indicates a specific interaction was successful.
            }
        }

        // If we get here, no specific conditions were met. Run the default interaction.
        ExecuteDefaultInteraction();
        return false; // Indicates the default interaction was used.
    }

    /// <summary>
    /// NEW: Handles the outcome of a successful condition check.
    /// It prioritizes playing a cutscene over showing a popup.
    /// </summary>
    private void ExecuteConditionalInteraction(InteractionCondition condition)
    {
        // --- 1. Handle the primary outcome (Cutscene > Popup) ---
        if (condition.cutsceneToPlay != null)
        {
            // If a cutscene is assigned, play it. This is the highest priority outcome.
            CutsceneManager.Instance.StartCutscene(condition.cutsceneToPlay);
        }
        else
        {
            // If no cutscene, show the popup message.
            ShowPopup(condition.popupMessage);
        }

        // --- 2. Play Sound Effect ---
        if (condition.soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(condition.soundEffect);
        }

        // --- 3. Set World State ---
        if (!string.IsNullOrEmpty(condition.stateToSetOnSuccess))
        {
            WorldDataManager.Instance.SetGlobalFlag(condition.stateToSetOnSuccess, condition.valueToSet);
        }

        // --- 4. Consume Item (if applicable) ---
        if (condition.consumeItemOnSuccess)
        {
            if (condition.checkType == ConditionType.HasItemByID && !string.IsNullOrEmpty(condition.requiredItemID))
            {
                Inventory.Instance.RemoveItemByID(condition.requiredItemID);
            }
            else if (condition.checkType == ConditionType.HasItemWithTag && condition.requiredItemTag != ItemTag.None)
            {
                // Find the first item with the tag and remove it.
                CreateInventoryItem itemToRemove = Inventory.Instance.GetFirstItemWithTag(condition.requiredItemTag);
                if (itemToRemove != null)
                {
                    Inventory.Instance.RemoveItemByID(itemToRemove.id);
                }
            }
        }
    }

    /// <summary>
    /// NEW: The logic for the default interaction, moved into its own method for clarity.
    /// </summary>
    private void ExecuteDefaultInteraction()
    {
        ShowPopup(defaultInteraction.popupMessage);

        if (defaultInteraction.soundEffect != null && this.audioSource != null)
        {
            this.audioSource.PlayOneShot(defaultInteraction.soundEffect);
        }

        if (!string.IsNullOrEmpty(defaultInteraction.stateToSetOnSuccess))
        {
            WorldDataManager.Instance.SetGlobalFlag(defaultInteraction.stateToSetOnSuccess, defaultInteraction.valueToSet);
        }
    }

    // The rest of the file (ShowPopup, DismissPopup, OnDestroy) remains unchanged.

    private void ShowPopup(string message)
    {
        if (activePopupInstance != null) { Destroy(activePopupInstance); }

        activePopupInstance = Instantiate(popupPrefab, mainCanvas.transform);
        TextMeshProUGUI textComponent = activePopupInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (textComponent != null)
        {
            textComponent.text = message;
            activePopupInstance.SetActive(true);

            WorldDataManager.Instance.SetGlobalFlag("UI_IS_ACTIVE", true);
            activeHandler = this;
        }
        else
        {
            Debug.LogError($"Popup prefab '{popupPrefab.name}' is missing a TextMeshProUGUI component.", popupPrefab);
            Destroy(activePopupInstance);
        }
    }

    private void DismissPopup()
    {
        if (activePopupInstance != null)
        {
            Destroy(activePopupInstance);
        }

        WorldDataManager.Instance.SetGlobalFlag("UI_IS_ACTIVE", false);
        activeHandler = null;
    }

    private void OnDestroy()
    {
        if (activeHandler == this)
        {
            DismissPopup();
        }
    }
}