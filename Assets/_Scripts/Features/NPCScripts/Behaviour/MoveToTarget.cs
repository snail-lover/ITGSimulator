// MoveToTarget.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// This Action task commands the NPC to move to a target and waits until it arrives.
// It returns Running while moving and Success upon arrival.
[TaskCategory("NPC")]
[TaskDescription("Moves the NPC to the specified target GameObject. Returns Running while moving and Success on arrival.")]
public class MoveToTarget : Action
{
    // --- INPUTS ---
    public SharedNpcBrain NpcBrain;

    public SharedGameObject InTargetObject;

    // --- PRIVATE REFERENCES ---
    private NpcMovement movement;
    private ActivityObject targetActivity;

    // OnStart is called once when the task begins. We issue the move command here.
    public override void OnStart()
    {
        // Get the NpcMovement component from the brain.
        movement = NpcBrain.Value.Controller.Movement;

        // Get the ActivityObject component from our target GameObject.
        targetActivity = InTargetObject.Value.GetComponent<ActivityObject>();

        if (movement == null || targetActivity == null)
        {
            Debug.LogError("MoveToTarget: Movement or ActivityObject component is missing.", this.gameObject);
            return;
        }

        // --- THE COMMAND ---
        // Get the precise destination from the ActivityObject and tell the body to move.
        Vector3 destination = targetActivity.GetTargetPosition();
        movement.MoveTo(destination);
    }

    // OnUpdate is called every frame. We use it to check if we have arrived.
    public override TaskStatus OnUpdate()
    {
        if (movement == null)
        {
            return TaskStatus.Failure;
        }

        // --- THE MONITORING ---
        // The NpcMovement component has a simple boolean that tells us if it's still moving.
        if (movement.IsMoving)
        {
            // If we are still moving, the task is not yet complete.
            return TaskStatus.Running;
        }
        else
        {
            // If we are no longer moving, we have arrived. The task is a success.
            return TaskStatus.Success;
        }
    }

    // OnEnd is called when the task concludes (either by Success, Failure, or being aborted).
    // It's good practice to ensure the NPC stops moving if the task is aborted from a higher-priority branch.
    public override void OnEnd()
    {
        if (movement != null && movement.IsMoving)
        {
            movement.Stop();
        }
    }
}