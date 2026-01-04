// InitializeBrain.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// This task is designed to be the very first task in a Behavior Tree.
// Its only job is to find the NpcBrain on the same GameObject and
// assign it to a shared variable for all other tasks to use.
[TaskCategory("NPC")]
[TaskDescription("Finds the NpcBrain on this GameObject and sets the SharedNpcBrain variable. This should run first.")]
public class InitializeBrain : Action
{
    [RequiredField] // Make sure this is assigned in the inspector.
    public SharedNpcBrain NpcBrain;

    public override void OnStart()
    {
        // Get the NpcBrain component from the GameObject that is running this tree.
        var brain = GetComponent<NpcBrain>();

        if (brain != null)
        {
            // Assign the found brain to the shared variable.
            NpcBrain.Value = brain;
        }
    }

    public override TaskStatus OnUpdate()
    {
        // Check if the assignment was successful.
        if (NpcBrain.Value != null)
        {
            // Return Success immediately. This task is done.
            return TaskStatus.Success;
        }
        else
        {
            // If we couldn't find the brain, the whole tree should fail.
            Debug.LogError("InitializeBrain task failed: Could not find NpcBrain component on this GameObject.", this.gameObject);
            return TaskStatus.Failure;
        }
    }
}