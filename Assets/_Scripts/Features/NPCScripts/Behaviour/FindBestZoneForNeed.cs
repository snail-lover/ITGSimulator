// FindBestZoneForNeed.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;

[TaskCategory("NPC")]
[TaskDescription("Finds a zone to search for a need. Checks memory first, then picks a random zone if memory fails.")]
public class FindBestZoneForNeed : Action
{
    public SharedNpcBrain NpcBrain;
    public float criticalThreshold = 50f;
    public SharedGameObject OutZoneTarget;

    private NpcBrain brain;
    private NpcRuntimeData data;
    private NpcNeed criticalNeed;

    public override void OnStart()
    {
        brain = NpcBrain.Value;
        if (brain != null) data = brain.RuntimeData;
    }

    public override TaskStatus OnUpdate()
    {
        if (brain == null || data == null) return TaskStatus.Failure;

        // Find the most critical need
        FindMostCriticalNeed();
        if (criticalNeed == null) return TaskStatus.Failure;

        Debug.Log($"<color=orange>FindBestZone: Looking for a zone that can satisfy '{criticalNeed.name}'.</color>");

        // 1. Check memory first
        SearchableZone targetZone = FindZoneFromMemory(criticalNeed.name);

        // 2. If memory fails, pick a random zone as a last resort
        if (targetZone == null)
        {
            Debug.LogWarning("FindBestZone: No promising zones in memory. Picking a random zone to explore.");
            targetZone = FindRandomZone();
        }

        if (targetZone != null)
        {
            Debug.LogWarning($"<color=green>FindBestZone: Selected zone '{targetZone.zoneName}'. SUCCESS!</color>");
            OutZoneTarget.Value = targetZone.gameObject;
            return TaskStatus.Success;
        }

        Debug.LogError("<color=red>FindBestZone: Could not find any zones to search. FAILURE.</color>");
        return TaskStatus.Failure;
    }

    private void FindMostCriticalNeed()
    {
        criticalNeed = null;
        float maxNeed = 0f;
        foreach (var need in data.needs.Values)
        {
            if (need.currentValue > maxNeed && need.currentValue >= criticalThreshold)
            {
                maxNeed = need.currentValue;
                criticalNeed = need;
            }
        }
    }

    private SearchableZone FindZoneFromMemory(string needName)
    {
        Debug.Log("...checking memory.");
        // data.learnedZoneContents is Dictionary<string, HashSet<string>>
        foreach (var memoryPair in data.learnedZoneContents)
        {
            string zoneName = memoryPair.Key;
            HashSet<string> knownContents = memoryPair.Value;

            if (knownContents.Contains(needName))
            {
                // Found a promising zone name in memory. Now find the actual scene object.
                foreach (var zone in SearchableZone.AllZones)
                {
                    if (zone.zoneName == zoneName)
                    {
                        Debug.Log($"Found promising zone '{zoneName}' in memory.");
                        return zone; // Return the first one we find for simplicity
                    }
                }
            }
        }
        return null; // No promising zones found in memory
    }

    private SearchableZone FindRandomZone()
    {
        if (SearchableZone.AllZones.Count == 0) return null;

        // Get the zone the NPC is currently in
        SearchableZone currentZone = brain.GetZoneForPosition(brain.transform.position);

        List<SearchableZone> potentialZones = new List<SearchableZone>();
        foreach (var zone in SearchableZone.AllZones)
        {
            // Don't pick the zone we are already in, unless it's the only one
            if (zone != currentZone)
            {
                potentialZones.Add(zone);
            }
        }

        if (potentialZones.Count > 0)
        {
            int randomIndex = Random.Range(0, potentialZones.Count);
            return potentialZones[randomIndex];
        }

        // Fallback: If we are in the only zone, just return that one.
        return SearchableZone.AllZones[0];
    }
}