using UnityEngine;

// Mark this class as [System.Serializable] so Unity knows how to save/load it
// as part of a larger save file (like a GameSave object).
[System.Serializable]
public class NpcRuntimeData
{
    // A reference to know which NPC this data belongs to. We use the asset name as a unique ID.
    public string npcStateID;

    // All the data that was previously being modified in the ScriptableObject
    public int currentLove;

    public Vector3 lastKnownPosition;


    /// <summary>
    /// Creates a new runtime data instance based on the initial state defined in the ScriptableObject.
    /// </summary>
    public NpcRuntimeData(NpcConfig initialState)
    {
        this.npcStateID = initialState.name;
        this.currentLove = initialState.initialLove;

        // We initialize position to zero. We'll handle this on load.
        this.lastKnownPosition = Vector3.zero;
    }

}