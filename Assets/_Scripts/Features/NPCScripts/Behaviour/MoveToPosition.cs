// MoveToPosition.cs
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPC")]
[TaskDescription("Moves the NPC to a specific Vector3 position. Reads the position from a SharedVector3 variable.")]
public class MoveToPosition : Action
{
    // --- INPUTS ---
    public SharedNpcBrain NpcBrain;
    public SharedVector3 InTargetPosition;

    // --- Private state ---
    private NpcMovement movement;
    private NavMeshAgent agent;

    public override void OnStart()
    {
        // Get our component references
        movement = NpcBrain.Value.Controller.Movement;
        agent = NpcBrain.Value.Agent;

        if (movement == null || agent == null)
        {
            Debug.LogError("MoveToPosition: NpcMovement or NavMeshAgent component is missing!");
            return;
        }

        // Get the destination from our input variable
        Vector3 destination = InTargetPosition.Value;

        Debug.Log($"<color=lightblue>MoveToPosition: Commanding move to stale memory location {destination}.</color>");
        movement.MoveTo(destination);
    }

    public override TaskStatus OnUpdate()
    {
        // Safety check
        if (movement == null || agent == null)
        {
            return TaskStatus.Failure;
        }

        // The agent is still calculating the path. We must wait.
        if (agent.pathPending)
        {
            return TaskStatus.Running;
        }

        // The agent has a path and is still moving.
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            return TaskStatus.Running;
        }

        // If we are no longer calculating a path and we are not moving, we have arrived.
        if (!agent.pathPending && !movement.IsMoving)
        {
            Debug.LogWarning($"<color=green>MoveToPosition: Arrived at stale memory location. SUCCESS!</color>");
            return TaskStatus.Success;
        }

        // Default to running if we haven't met a conclusive condition.
        return TaskStatus.Running;
    }

    /// <summary>
    /// If the task is aborted by a higher-priority behavior, make sure the NPC stops walking.
    /// </summary>
    public override void OnEnd()
    {
        if (movement != null && movement.IsMoving)
        {
            movement.Stop();
        }
    }
}