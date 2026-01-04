using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue/Dialogue Graph")]
public class DialogueGraphSO : ScriptableObject
{
    [Header("Greeting")]
    [Tooltip("The Node ID of the single node that should be displayed as a greeting before any topics are shown. This node should have no choices connected to it.")]
    public string greetingNodeID;

    [Header("Dialogue Entry Points")]
    [Tooltip("Define all possible conversation starters here.")]
    public List<EntryPointSaveData> entryPoints = new List<EntryPointSaveData>();

    [Header("Graph Nodes & Connections")]
    public List<DialogueNodeSaveData> nodes = new List<DialogueNodeSaveData>();
    public List<EdgeSaveData> edges = new List<EdgeSaveData>();


    [Serializable]
    public class EntryPointSaveData
    {
        public string entryText = "New Topic";
        public DialogueEntryPointType entryType = DialogueEntryPointType.General;

        [Tooltip("The Node ID of the node this topic should lead to. Must match a Node ID in the graph.")]
        public string startNodeID;

        public List<WorldStateCondition> worldStateConditions = new List<WorldStateCondition>();

        [Tooltip("If set, this topic will only appear if the player has the item with this ID.")]
        public string requiredItemID;
    }

    public DialogueData ToRuntimeData()
    {
        var runtimeData = new DialogueData
        {
            greetingNodeID = this.greetingNodeID,
            // Convert the list of EntryPointSaveData to DialogueEntryPoint
            entryPoints = this.entryPoints.Select(savedPoint => new DialogueEntryPoint
            {
                entryText = savedPoint.entryText,
                entryType = savedPoint.entryType,
                startNodeID = savedPoint.startNodeID,
                // Create a new list to ensure it's a deep copy
                worldStateConditions = new List<WorldStateCondition>(savedPoint.worldStateConditions),
                requiredItemID = savedPoint.requiredItemID
            }).ToList(),
        };

        var nodeGuidToDialogueNode = new Dictionary<string, DialogueNode>();
        var tempRuntimeNodes = new List<DialogueNode>();

        // First pass: create all DialogueNode instances
        foreach (DialogueNodeSaveData savedNode in this.nodes)
        {
            if (savedNode == null) continue;

            DialogueNode runtimeNode = new DialogueNode
            {
                nodeID = savedNode.nodeID,
                text = savedNode.dialogueText,
                itemGate = savedNode.itemGate != null ? new ItemGate
                {
                    itemName = savedNode.itemGate.itemName,
                    requiredItemID = savedNode.itemGate.requiredItemID // Use the string ID
                    // Note: removeItemOnSelect is not on the node gate, only on choices
                } : null,

                worldStateConditions = new List<WorldStateCondition>(savedNode.worldStateConditions),
                choices = new DialogueChoice[savedNode.choices.Count]
            };
            tempRuntimeNodes.Add(runtimeNode);
            if (!string.IsNullOrEmpty(savedNode.nodeGUID))
            {
                nodeGuidToDialogueNode[savedNode.nodeGUID] = runtimeNode;
            }
        }
        runtimeData.nodes = tempRuntimeNodes.ToArray();

        // Second pass: Link choices
        foreach (DialogueNodeSaveData savedNode in this.nodes)
        {
            if (savedNode == null || !nodeGuidToDialogueNode.ContainsKey(savedNode.nodeGUID)) continue;
            DialogueNode sourceRuntimeNode = nodeGuidToDialogueNode[savedNode.nodeGUID];
            for (int i = 0; i < savedNode.choices.Count; i++)
            {
                ChoiceSaveData savedChoice = savedNode.choices[i];
                if (savedChoice == null) continue;

                string targetNodeGUID = FindTargetNodeGUIDForPort(savedChoice.outputPortGUID);
                string nextNodeIDForChoice = !string.IsNullOrEmpty(savedChoice.overrideNextNodeID)
                    ? savedChoice.overrideNextNodeID
                    : null;

                if (string.IsNullOrEmpty(nextNodeIDForChoice) && !string.IsNullOrEmpty(targetNodeGUID) && nodeGuidToDialogueNode.TryGetValue(targetNodeGUID, out DialogueNode targetRuntimeNode))
                {
                    nextNodeIDForChoice = targetRuntimeNode.nodeID;
                }

                sourceRuntimeNode.choices[i] = new DialogueChoice
                {
                    choiceText = savedChoice.choiceText,
                    lovePointChange = savedChoice.lovePointChange,
                    nextNodeID = nextNodeIDForChoice,
                    startsHangout = savedChoice.startsHangout,

                    itemGate = savedChoice.itemGate != null ? new ItemGate
                    {
                        itemName = savedChoice.itemGate.itemName,
                        requiredItemID = savedChoice.itemGate.requiredItemID, // Use the string ID
                        removeItemOnSelect = savedChoice.itemGate.removeItemOnSelect
                    } : null,

                    stateChangesOnSelect = new List<WorldStateChange>(savedChoice.stateChangesOnSelect ?? new List<WorldStateChange>()),
                    worldStateConditions = new List<WorldStateCondition>(savedChoice.worldStateConditions),

                    // --- CHANGE: COPY TAG DATA TO RUNTIME ---
                    requiredTag = savedChoice.requiredTag,
                    itemToGiveID = savedChoice.itemToGiveID
                };
            }
        }
        return runtimeData;
    }

    private string FindTargetNodeGUIDForPort(string outputPortGUID)
    {
        if (string.IsNullOrEmpty(outputPortGUID)) return null;
        foreach (var edge in edges)
        {
            if (edge != null && edge.outputNodePortGUID == outputPortGUID)
            {
                return edge.inputNodeGUID;
            }
        }
        return null;
    }
}

[Serializable]
public class DialogueNodeSaveData
{
    public string nodeGUID;
    public string nodeID;
    [TextArea(3, 5)]
    public string dialogueText;
    public Vector2 position;
    public ItemGate itemGate;
    public List<ChoiceSaveData> choices = new List<ChoiceSaveData>();
    public bool isEntryPoint = false;
    public List<WorldStateCondition> worldStateConditions = new List<WorldStateCondition>();

}

[Serializable]
public class ChoiceSaveData
{
    public string outputPortGUID;
    public string choiceText;
    public int lovePointChange;
    public ItemGate itemGate;
    public string overrideNextNodeID;
    public List<WorldStateChange> stateChangesOnSelect = new List<WorldStateChange>();
    public List<WorldStateCondition> worldStateConditions = new List<WorldStateCondition>();
    public ItemTag requiredTag = ItemTag.None;
    public string itemToGiveID;
    [Tooltip("If true, selecting this choice will end the dialogue and start a hangout.")]
    public bool startsHangout = false;
}

[Serializable]
public class EdgeSaveData
{
    public string outputNodeGUID;
    public string outputNodePortGUID;
    public string inputNodeGUID;
}

