// IsNeedCritical.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// This is an Action task. It performs an action and returns Success or Failure.
public class IsNeedCritical : Action
{
    // This will be linked to the "NpcBrain" variable we created in the editor.
    public SharedNpcBrain NpcBrain;

    // We can add public variables that show up in the Behavior Designer Inspector.
    public float criticalThreshold = 80f;

    public override TaskStatus OnUpdate()
    {
        // Get the actual NpcBrain object from the SharedVariable wrapper.
        NpcBrain brain = NpcBrain.Value;

        if (brain == null)
        {
            Debug.LogError("IsNeedCritical Task: NpcBrain is null!");
            return TaskStatus.Failure;
        }

        // Loop through all needs stored in the brain's runtime data.
        foreach (var need in brain.RuntimeData.needs.Values)
        {
            //Debug.Log($"Checking need '{need.name}': Current value is {need.currentValue}");
            if (need.currentValue >= criticalThreshold)
            {
                Debug.LogWarning($"CRITICAL NEED FOUND: {need.name}! Task SUCCEEDS.");
                // We found a critical need! This task is a success.
                // We could also store which need was found on the blackboard/BT variables
                // for the next task to use.
                return TaskStatus.Success;
            }
        }

        Debug.LogWarning("No critical needs found. Task FAILS.");

        // If we get here, no needs were critical. This task fails.
        return TaskStatus.Failure;
    }
}