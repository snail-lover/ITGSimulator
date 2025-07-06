using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Definition")]
public class Cutscene : ScriptableObject
{
    // Ordered list of actions that define the cutscene sequence
    public List<CutsceneAction> actions = new List<CutsceneAction>();

    // Identifiers for NPCs in the scene that should be managed during the cutscene
    // (Player is handled separately if targeted by an action)
    [Tooltip("GameObject names of NPCs in the scene that need to be explicitly paused/managed, even if not directly targeted by an action. Player is handled separately if targeted.")]
    public List<string> involvedNPCsOverrideIdentifiers = new List<string>(); 
}