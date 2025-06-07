using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Definition")]
public class Cutscene : ScriptableObject
{
    public List<CutsceneAction> actions = new List<CutsceneAction>();

    [Tooltip("GameObject names of NPCs in the scene that need to be explicitly paused/managed, even if not directly targeted by an action. Player is handled separately if targeted.")]
    public List<string> involvedNPCsOverrideIdentifiers = new List<string>(); 

}