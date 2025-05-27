// In an "Editor" folder or subfolder
using UnityEngine;
using System.Collections.Generic; // <--- ENSURE THIS IS PRESENT (it was in your snippet)
using System;

[CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue/Dialogue Graph")]
public class DialogueGraphSO : ScriptableObject
{
    [Header("Graph Metadata")]
    public string entryPointNodeGUID;
    public int startingLove;
    public List<LoveTier> loveTiers = new List<LoveTier>();

    [Header("Graph Nodes & Connections")]
    public List<DialogueNodeSaveData> nodes = new List<DialogueNodeSaveData>();
    public List<EdgeSaveData> edges = new List<EdgeSaveData>();

    public DialogueData ToRuntimeData()
    {
        var runtimeData = new DialogueData
        {
            startingLove = this.startingLove,
            loveTiers = this.loveTiers.ToArray(),
            // nodes array will be populated below
        };

        var nodeGuidToDialogueNode = new Dictionary<string, DialogueNode>();
        var tempRuntimeNodes = new List<DialogueNode>(); // Use a list first

        // First pass: create all DialogueNode instances and map GUIDs to them
        foreach (DialogueNodeSaveData savedNode in this.nodes)
        {
            if (savedNode == null) continue;

            DialogueNode runtimeNode = new DialogueNode
            {
                nodeID = savedNode.nodeID,
                text = savedNode.dialogueText,
                // Deep copy itemGate if it's a class and can be null
                itemGate = savedNode.itemGate != null ? new ItemGate { itemName = savedNode.itemGate.itemName, requiredItem = savedNode.itemGate.requiredItem } : null,
                choices = new DialogueChoice[savedNode.choices.Count] // Initialize choices array
            };
            tempRuntimeNodes.Add(runtimeNode);
            if (!string.IsNullOrEmpty(savedNode.nodeGUID)) // Ensure GUID is valid
            {
                nodeGuidToDialogueNode[savedNode.nodeGUID] = runtimeNode;
            }

            if (savedNode.nodeGUID == this.entryPointNodeGUID)
            {
                runtimeData.startNodeID = savedNode.nodeID;
            }
        }
        runtimeData.nodes = tempRuntimeNodes.ToArray(); // Assign to the final array

        // Second pass: Link choices using the nodeID
        foreach (DialogueNodeSaveData savedNode in this.nodes)
        {
            if (savedNode == null || !nodeGuidToDialogueNode.ContainsKey(savedNode.nodeGUID)) continue;

            DialogueNode sourceRuntimeNode = nodeGuidToDialogueNode[savedNode.nodeGUID];
            for (int i = 0; i < savedNode.choices.Count; i++)
            {
                ChoiceSaveData savedChoice = savedNode.choices[i];
                if (savedChoice == null) continue;

                string targetNodeGUID = FindTargetNodeGUIDForPort(savedChoice.outputPortGUID);
                string nextNodeIDForChoice = null;

                if (!string.IsNullOrEmpty(targetNodeGUID) && nodeGuidToDialogueNode.TryGetValue(targetNodeGUID, out DialogueNode targetRuntimeNode))
                {
                    nextNodeIDForChoice = targetRuntimeNode.nodeID;
                }
                else if (!string.IsNullOrEmpty(savedChoice.overrideNextNodeID))
                {
                    nextNodeIDForChoice = savedChoice.overrideNextNodeID;
                }

                sourceRuntimeNode.choices[i] = new DialogueChoice
                {
                    choiceText = savedChoice.choiceText,
                    lovePointChange = savedChoice.lovePointChange,
                    nextNodeID = nextNodeIDForChoice,
                    // Deep copy itemGate
                    itemGate = savedChoice.itemGate != null ? new ItemGate { itemName = savedChoice.itemGate.itemName, requiredItem = savedChoice.itemGate.requiredItem } : null,
                    triggerCutsceneName = (savedChoice.triggerCutscene != null) ? savedChoice.triggerCutscene.name : null
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
    [TextArea(3, 5)] // Make text area a bit more manageable in inspector
    public string dialogueText;
    public Vector2 position;
    public ItemGate itemGate;
    public List<ChoiceSaveData> choices = new List<ChoiceSaveData>();
    public bool isEntryPoint = false;

}

[Serializable]
public class ChoiceSaveData
{
    public string outputPortGUID;
    public string choiceText;
    public int lovePointChange;
    public ItemGate itemGate;
    public string overrideNextNodeID;
    public Cutscene triggerCutscene; // Reference to the Cutscene SO
}

[Serializable]
public class EdgeSaveData
{
    public string outputNodeGUID;
    public string outputNodePortGUID;
    public string inputNodeGUID;
}