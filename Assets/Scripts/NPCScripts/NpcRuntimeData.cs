// --- START OF FILE NpcRuntimeData.cs ---

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NpcRuntimeData
{
    public string npcStateID;
    public int currentLove;
    public Vector3 lastKnownPosition;
    public Dictionary<string, NpcNeed> needs = new Dictionary<string, NpcNeed>();

    // --- NEW: THE NPC'S MEMORY ---
    [Header("Learned Knowledge")]
    [Tooltip("Stores what need/goal tags the NPC has seen in a given zone.")]
    public Dictionary<string, HashSet<string>> learnedZoneContents;

    [Tooltip("Stores the last known location of specific activities.")]
    public Dictionary<string, Vector3> rememberedActivityLocations;
    // -----------------------------


    public NpcRuntimeData(NpcConfig initialState)
    {
        this.npcStateID = initialState.name;
        this.currentLove = initialState.initialLove;
        this.lastKnownPosition = Vector3.zero;

        // Initialize dictionaries
        this.learnedZoneContents = new Dictionary<string, HashSet<string>>();
        this.rememberedActivityLocations = new Dictionary<string, Vector3>();

        // Initialize needs from the config's decay rates
        needs.Add("Hunger", new NpcNeed("Hunger", initialState.hungerDecay));
        needs.Add("Energy", new NpcNeed("Energy", initialState.energyDecay));
        needs.Add("Bladder", new NpcNeed("Bladder", initialState.bladderDecay));
        needs.Add("Fun", new NpcNeed("Fun", initialState.funDecay));
    }
}

// NpcNeed class remains the same.
[System.Serializable]
public class NpcNeed
{
    public string name;
    [Range(0, 100)]
    public float currentValue;
    public float decayRate;

    public NpcNeed(string name, float decayRate)
    {
        this.name = name;
        this.decayRate = decayRate;
        this.currentValue = 0;
    }

    public void UpdateNeed(float deltaTime)
    {
        currentValue = Mathf.Clamp(currentValue + (decayRate * deltaTime), 0, 100);
    }
}