using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC State", menuName = "NPC/NPC State Data")]
public class NpcConfig: ScriptableObject
{
    [Header("NPC Info (Definitional)")]
    public string npcName = "NPC Name";
    public Sprite npcImage;
    public TextAsset dialogueFile;
    public int npcAge;
    public string npcGender;
    public string npcBloodType;
    public string npcZodiacSign;
    public int npcCockLength;
    public string npcLikes;
    public string npcDislikes;

    [Header("Task Management (Behavior Profile)")]
    [Tooltip("The pool of tasks this NPC can choose from.")]
    public List<string> taskIDPool;

    [Header("Cutscene Settings")]
    [Tooltip("The cutscene asset to play when the final love level is reached.")]
    public Cutscene finalLoveCutsceneAsset;

    [Header("Runtime State (Savable)")]
    [Tooltip("The NPC's current love/affection level. This value will change during gameplay.")]
    public int initialLove = 0;

    // You could add other savable runtime states here, like quest progress, etc.

    [Header("Interaction Settings")]
    [Tooltip("Range at which the player can interact with this NPC")]
    public float interactRange = 2.0f;

    [Header("Movement & Animation")]
    [Tooltip("How quickly the NPC turns to face the task direction upon arrival")]
    public float rotationSpeed = 5f;
    [Tooltip("Name of the boolean parameter in the Animator to indicate walking")]
    public string walkingParameterName = "IsWalking";

    [Header("Task Settings")]
    [Tooltip("Delay in seconds after completing a task before choosing the next one")]
    public float taskCompletionDelay = 2.0f;
}