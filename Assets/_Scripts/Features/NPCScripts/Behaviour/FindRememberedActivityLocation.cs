// FindRememberedActivityLocation.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Linq;

[TaskCategory("NPC")]
[TaskDescription("Finds the location of a remembered activity that can satisfy a critical need.")]
public class FindRememberedActivityLocation : Action
{
    // --- INPUTS ---
    public SharedNpcBrain NpcBrain;
    public float criticalThreshold = 50f;

    // --- OUTPUTS ---

    public SharedString OutActivityID;
    public SharedVector3 OutStalePosition;

    private NpcBrain brain;
    private NpcRuntimeData data;

    public override void OnStart()
    {
        brain = NpcBrain.Value;
        if (brain != null) data = brain.RuntimeData;
    }

    public override TaskStatus OnUpdate()
    {
        if (brain == null || data == null) return TaskStatus.Failure;

        NpcNeed criticalNeed = FindMostCriticalNeed();
        if (criticalNeed == null) return TaskStatus.Failure;

        // --- Search Memory ONLY ---
        foreach (var memoryPair in data.rememberedActivityLocations)
        {
            string activityID = memoryPair.Key;
            Vector3 stalePosition = memoryPair.Value.position;

            // To know if this memory is useful, we need to check the Activity's properties.
            // We look it up in the manager, but ONLY to read its data, not its live position.
            ActivityObject activityData = ActivityManager.Instance.GetActivityByID(activityID);

            if (activityData != null && DoesActivitySatisfyNeed(activityData, criticalNeed.name))
            {
                // We found a promising memory!
                Debug.Log($"<color=cyan>Found promising memory: Activity '{activityID}' at stale location {stalePosition}.</color>");

                // Set the outputs for the next tasks
                OutActivityID.Value = activityID;
                OutStalePosition.Value = stalePosition;
                return TaskStatus.Success;
            }
        }

        // If we get here, no memory was found for the current need.
        return TaskStatus.Failure;
    }

    // Helper methods (same as before)
    private NpcNeed FindMostCriticalNeed()
    {
        return data.needs.Values.Where(n => n.currentValue >= criticalThreshold)
                                .OrderByDescending(n => n.currentValue)
                                .FirstOrDefault();
    }

    private bool DoesActivitySatisfyNeed(ActivityObject activity, string needName)
    {
        foreach (var effect in activity.needEffects)
        {
            if (effect.needName == needName && effect.effectValue < 0) return true;
        }
        return false;
    }
}