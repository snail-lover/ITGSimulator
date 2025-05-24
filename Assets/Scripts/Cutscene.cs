// Cutscene.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Definition")]
public class Cutscene : ScriptableObject
{
    public List<CutsceneAction> actions = new List<CutsceneAction>();

    [Tooltip("GameObject names of NPCs in the scene that need to be explicitly paused/managed, even if not directly targeted by an action. Player is handled separately if targeted.")]
    public List<string> involvedNPCsOverrideIdentifiers = new List<string>(); // <<< THIS IS THE LINE THAT WAS MISSING OR MISNAMED

    // You can add other global cutscene settings here if needed in the future,
    // e.g., bool canPlayerSkip = true;
    // e.g., AudioClip backgroundMusic;
}