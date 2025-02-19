using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueData
{
    // The NPC's starting love value
    public int startingLove;

        // Array of love tiers
    public LoveTier[] loveTiers;

    // ID of the first node to show
    public string startNodeID;

    // All dialogue nodes
    public DialogueNode[] nodes;

    // Runtime dictionary for quick node lookup (not serialized)
    [NonSerialized] public Dictionary<string, DialogueNode> nodeDictionary;
}

[Serializable]
public class LoveTier
{
    public int minLove;
    public int maxLove;
    public string startNodeID;
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
}

[Serializable]
public class ItemGate
{
    // For simple checking, use an item name or ID
    public string itemName;
    
    // Optionally reference a prefab or scriptable
    public GameObject requiredItem;
}
