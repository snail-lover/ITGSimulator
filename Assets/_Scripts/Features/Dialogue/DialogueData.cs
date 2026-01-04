using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueData
{

    public string greetingNodeID;

    [Tooltip("The list of all possible starting topics for this NPC.")]
    public List<DialogueEntryPoint> entryPoints;
    // All dialogue nodes
    public DialogueNode[] nodes;
    
    // Runtime dictionary for quick node lookup (not serialized)
    [NonSerialized] public Dictionary<string, DialogueNode> nodeDictionary;
}

[Serializable]
public class DialogueEntryPoint
{
    [Tooltip("The text the player sees in the topic list (e.g., 'About that quest...')")]
    public string entryText;

    [Tooltip("The category this topic falls into for sorting and grouping.")]
    public DialogueEntryPointType entryType;

    [Tooltip("The Node ID to jump to when this topic is selected.")]
    public string startNodeID;

    [Tooltip("Conditions that must be met for this topic to be available.")]
    public List<WorldStateCondition> worldStateConditions;

    [Tooltip("If set, this topic will only appear if the player has the item with this ID.")]
    public string requiredItemID;
}

[Serializable]
public enum DialogueEntryPointType
{
    Quest,          // Priority 1
    Love,           // Priority 2
    Contextual,     // Priority 3
    General         // Priority 4
}

[Serializable]
public class LoveTier
{
    public int minLove;
    public int maxLove;
    public string startNodeID;
}

[Serializable]
public class WorldStateCondition
{
    [Tooltip("The key of the state to check in the WorldStateManager (e.g., 'CLICKED_ON_DOOR').")]
    public string conditionKey;
    [Tooltip("The required value for this condition to pass (e.g., TRUE).")]
    public bool requiredValue = true;
}

[Serializable]
public class WorldStateChange
{
    [Tooltip("The key of the state to set in the WorldStateManager (e.g., 'SPOKE_TO_SIMON').")]
    public string stateKey;
    [Tooltip("The value to set the state to when this choice is selected.")]
    public bool stateValue = true;
}

[Serializable]
public class DialogueNode
{
    // Unique identifier for this node
    public string nodeID;

    // The main text displayed for this node
    public string text;

    // Possible choices leading out of this node
    public DialogueChoice[] choices;

    // Optional gating: if you want the entire node locked behind an item
    public ItemGate itemGate;
    
    public List<WorldStateCondition> worldStateConditions;
}

[Serializable]
public class DialogueChoice
{
    // Choice text displayed to the player
    public string choiceText;

    // How much love to add or remove (+/-)
    public int lovePointChange;

    // ID of the next node to go to
    public string nextNodeID;

    // If this choice needs a special item
    public ItemGate itemGate;

    // If this choice requires a specific tag on the item
    public ItemTag requiredTag = ItemTag.None;


    // Reference to a Cutscene ScriptableObject to play if this choice is selected
    [Tooltip("If set, this cutscene (by name/path) will play. nextNodeID ignored.")]
    public string triggerCutsceneName;

    [Tooltip("A list of world states to set when this choice is selected.")]
    public List<WorldStateChange> stateChangesOnSelect;

    public List<WorldStateCondition> worldStateConditions;

    [Tooltip("The ID of the item to give to the player when this choice is selected.")]
    public string itemToGiveID;

    [Tooltip("If set to true, selecting this choice will immediately end the dialogue and start a hangout with the NPC.")]
    public bool startsHangout = false; 

}

[Serializable]
public class ItemGate
{
    // For simple checking, use an item name or ID
    public string itemName;

    [Tooltip("The unique ID of the item required by this gate.")]
    public string requiredItemID;

    [Tooltip("If checked, this item will be removed from the player's inventory when the choice is selected.")]
    public bool removeItemOnSelect = false;
}
