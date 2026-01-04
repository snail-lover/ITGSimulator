// --- START OF FILE NpcRuntimeData.cs ---

using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct NpcMemoryEntry
{
    public Vector3 position;
    public float lastUpdatedTime;
}


[System.Serializable]
public class NpcRuntimeData
{
    public string npcStateID;
    public int currentLove;
    public Vector3 lastKnownPosition;
    public Dictionary<string, NpcNeed> needs = new Dictionary<string, NpcNeed>();

    [Header("Learned Knowledge")]
    [Tooltip("Stores what need/goal tags the NPC has seen in a given zone.")]
    public Dictionary<string, HashSet<string>> learnedZoneContents;

    [Tooltip("Stores the last known location and update time of specific activities.")]
    // The dictionary now stores the new NpcMemoryEntry struct instead of just a Vector3.
    public Dictionary<string, NpcMemoryEntry> rememberedActivityLocations;

    public NpcRuntimeData()
    {
        // Initialize dictionaries to prevent null reference errors.
        this.learnedZoneContents = new Dictionary<string, HashSet<string>>();
        this.rememberedActivityLocations = new Dictionary<string, NpcMemoryEntry>();
        this.needs = new Dictionary<string, NpcNeed>();
    }


    /// <summary>
    /// This is the new constructor. It no longer knows about NpcConfig.
    /// It takes simple, primitive data types as arguments.
    /// </summary>
    public NpcRuntimeData(string id, int initialLove, float hunger, float energy, float bladder, float fun)
    {
        this.npcStateID = id;
        this.currentLove = initialLove;
        this.lastKnownPosition = Vector3.zero;

        // Initialize dictionaries
        this.learnedZoneContents = new Dictionary<string, HashSet<string>>();
        this.rememberedActivityLocations = new Dictionary<string, NpcMemoryEntry>();

        // Initialize needs from the passed-in decay rates
        this.needs = new Dictionary<string, NpcNeed>
        {
            { "Hunger", new NpcNeed("Hunger", hunger) },
            { "Energy", new NpcNeed("Energy", energy) },
            { "Bladder", new NpcNeed("Bladder", bladder) },
            { "Fun", new NpcNeed("Fun", fun) }
        };
    }
}

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