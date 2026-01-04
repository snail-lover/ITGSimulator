using System.Collections.Generic;
using UnityEngine;
using BehaviorDesigner.Runtime;

[System.Serializable]
public class PersonalityGoal
{
    public string goalName;
    public List<string> associatedTags;
    [Range(0, 1)]
    public float priority = 0.5f; // Add a priority slider
}


[CreateAssetMenu(fileName = "New NPC State", menuName = "NPC/NPC State Data")]
public class NpcConfig: ScriptableObject
{
    [Header("NPC Info (Definitional)")]
    public string npcName = "NPC Name";
    public Sprite npcImage;
    public TextAsset dialogueFile;

    [Header("Attraction Value System")]
    [Tooltip("Defines the NPC's own personality. Attraction is calculated based on the resonance between the player's personality and the NPC's own traits defined here.")]
    public List<PersonalityPreference> valueSystem;

    [Header("Behavior Trees")] 
    [Tooltip("The Behavior Tree to use for the NPC's normal, autonomous actions.")]
    public ExternalBehavior autonomousBehaviorTree;

    [Tooltip("The Behavior Tree to use when the NPC is in a 'Hangout' with the player.")]
    public ExternalBehavior hangoutBehaviorTree;

    [Header("Task Management (Behavior Profile)")]
    [Tooltip("The pool of tasks this NPC can choose from.")]
    public List<string> taskIDPool;

    //[Header("Cutscene Settings")]
    //[Tooltip("The cutscene asset to play when the final love level is reached.")]
    //public Cutscene finalLoveCutsceneAsset;

    [Header("Runtime State (Savable)")]
    [Tooltip("The NPC's current love/affection level. This value will change during gameplay.")]
    public int initialLove = 0;


    [Header("Personality & Goals")] // A new header for clarity
    [Tooltip("The long-term goals and personality drivers for this NPC.")]
    public List<PersonalityGoal> personalityGoals; // This will be our list of goals

    [Header("Needs")]
    [Tooltip("How fast the Hunger need increases per second.")]
    public float hungerDecay = 0.5f;
    [Tooltip("How fast the Energy need increases per second.")]
    public float energyDecay = 0.2f;
    [Tooltip("How fast the Bladder need increases per second.")]
    public float bladderDecay = 0.8f;
    [Tooltip("How fast the Fun need increases per second.")] // ADD THIS LINE
    public float funDecay = 0.4f; // ADD THIS LINE


    // You could add other savable runtime states here, like quest progress, etc.

    [Header("Interaction Settings")]
    [Tooltip("Range at which the player can interact with this NPC")]
    public float interactRange = 2.0f;

    [Header("Movement & Animation")]
    [Tooltip("How quickly the NPC turns to face the task direction upon arrival")]
    public float rotationSpeed = 5f;
    [Tooltip("Name of the float parameter in the Animator to indicate walking")]
    public string speedParameterName = "Speed";

    [Header("Task Settings")]
    [Tooltip("Delay in seconds after completing a task before choosing the next one")]
    public float taskCompletionDelay = 2.0f;

    //[Header("High Priority Dialogue")]
    //[Tooltip("A list of dialogues this NPC can initiate with the player if the conditions are met.")]
    //public List<HighPriorityDialogueTrigger> highPriorityDialogues;
    [Tooltip("The name of the animation TRIGGER to play when the NPC decides to approach the player for dialogue.")]
    public string noticePlayerAnimationTrigger = "NoticePlayer";
    [Tooltip("How long to wait (in seconds) for the 'notice' animation to play before starting to walk.")]
    public float noticePlayerAnimationDuration = 1.5f;
    [Tooltip("The sound effect to play when the NPC notices the player.")]
    public AudioClip noticePlayerSound;
}