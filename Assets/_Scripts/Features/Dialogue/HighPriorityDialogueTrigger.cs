using UnityEngine;

/// <summary>
/// A data structure that defines the conditions and content for an NPC-initiated
/// high-priority dialogue with the player.
/// </summary>
[System.Serializable]
public class HighPriorityDialogueTrigger
{
    [Tooltip("A description for the designer to remember what this trigger is for.")]
    public string description;

    [Tooltip("The key of the global flag in WorldDataManager that must be 'true' for this dialogue to trigger.")]
    public string triggeringFlag;

    [Tooltip("The ID of the node in the NPC's dialogue file to start the conversation with.")]
    public string dialogueNodeID;

    [Tooltip("How close the NPC needs to get to the player to initiate the dialogue.")]
    public float approachDistance = 2.5f;

    [Tooltip("A unique ID for this specific trigger. Used to ensure it only happens once. Example: 'NPCName_QuestName_Start'")]
    public string uniqueTriggerID;
}