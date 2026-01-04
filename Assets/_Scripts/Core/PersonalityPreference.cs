// PersonalityPreference.cs
using UnityEngine;

/// <summary>
/// A data container that defines an NPC's own personality on a single axis.
/// This is NOT an 'ideal' for the player to chase; it is the NPC's own character trait.
/// </summary>
[System.Serializable]
public class PersonalityPreference
{
    [Tooltip("The personality axis this trait applies to.")]
    public PersonalityAxis axis;

    [Tooltip("The NPC's own value on this axis. E.g., for Spontaneity<->Planning, -1 means the NPC is extremely spontaneous, +1 means they are a meticulous planner.")]
    [Range(-1f, 1f)]
    public float npcTraitValue; // Renamed from idealValue

    [Tooltip("How much does this trait define the NPC's character? A higher value means it more heavily influences their worldview and how they judge others.")]
    [Range(0f, 1f)]
    public float importance = 0.5f;
}