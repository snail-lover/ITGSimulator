// MoveToZone.cs (With Hyper-Specific Debugging)
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPC")]
[TaskDescription("Moves the NPC to the center of a specified SearchableZone.")]
public class MoveToZone : Action
{
    public SharedNpcBrain NpcBrain;
    public SharedGameObject InZoneTarget;

    private NpcMovement movement;
    private NavMeshAgent agent;

    public override void OnStart()
    {
        // --- PRE-FLIGHT CHECK ---
        // This will tell us exactly where the chain is broken.

        // Check 1: Is the variable from the Behavior Tree itself valid?
        if (NpcBrain.Value == null)
        {
            Debug.LogError("<color=red>FATAL ERROR in MoveToZone: The 'NpcBrain' variable passed from the Behavior Tree is NULL. Check the linkage in the Behavior Designer Inspector on THIS task.</color>");
            return;
        }

        // Check 2: Is the target GameObject valid?
        if (InZoneTarget.Value == null)
        {
            Debug.LogError("<color=red>FATAL ERROR in MoveToZone: The 'InZoneTarget' variable is NULL. This means FindBestZoneForNeed failed to set it.</color>");
            return;
        }

        // Check 3: Get the components from the brain and check them individually.
        movement = NpcBrain.Value.Controller.Movement;
        agent = NpcBrain.Value.Agent;

        if (agent == null)
        {
            Debug.LogError("<color=red>COMPONENT MISSING: The NpcBrain has a null 'Agent' reference. Check that NpcController.Awake() is successfully finding the NavMeshAgent component.</color>");
        }

        if (movement == null)
        {
            Debug.LogError("<color=red>COMPONENT MISSING: The NpcBrain's Controller has a null 'Movement' reference. Check that NpcController.Awake() is successfully finding the NpcMovement component.</color>");
        }

        // If either component is missing, we cannot proceed.
        if (movement == null || agent == null)
        {
            return;
        }

        // --- END OF PRE-FLIGHT CHECK ---

        // If all checks passed, we can safely command the move.
        Vector3 destination = InZoneTarget.Value.transform.position;
        Debug.Log($"<color=lightblue>MoveToZone: Commanding move to '{InZoneTarget.Value.name}' at {destination}.</color>");
        movement.MoveTo(destination);
    }

    public override TaskStatus OnUpdate()
    {
        // No changes needed here, but we add a safety check.
        if (movement == null || agent == null) return TaskStatus.Failure;

        if (agent.pathPending)
        {
            return TaskStatus.Running;
        }

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            return TaskStatus.Running;
        }

        if (!movement.IsMoving && !agent.pathPending)
        {
            Debug.LogWarning($"<color=green>MoveToZone: Arrived at destination. SUCCESS!</color>");
            return TaskStatus.Success;
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (movement != null && movement.IsMoving)
        {
            movement.Stop();
        }
    }
}