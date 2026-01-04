// FollowPlayer.cs 
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class FollowPlayer : Action
{
 
    public SharedNpcBrain NpcBrain;

    private NavMeshAgent agent;
    private Transform playerTransform;
    private float originalStoppingDistance;

    public override void OnStart()
    {
        agent = GetComponent<NavMeshAgent>();
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            playerTransform = playerGo.transform;
        }

        if (agent != null)
        {
            originalStoppingDistance = agent.stoppingDistance;

            var brain = NpcBrain.Value;
            if (brain != null)
            {
                agent.stoppingDistance = brain.followStoppingDistance;
                // --- THIS IS THE CRUCIAL DEBUG LOG ---
                Debug.Log($"[FollowPlayer] OnStart: NavMeshAgent's stoppingDistance has been set to {agent.stoppingDistance} from the NpcBrain.", agent.gameObject);
            }
            else
            {
                // --- ADD THIS FOR BETTER DEBUGGING ---
                Debug.LogError("[FollowPlayer] OnStart: FAILED to get NpcBrain from shared variable! The task cannot set the custom stopping distance.", agent.gameObject);
            }

            agent.isStopped = false;
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (playerTransform == null || agent == null || !agent.isOnNavMesh)
        {
            return TaskStatus.Failure;
        }

        // --- THE ROTATION FIX ---
        // Create a target position on the same horizontal plane as the NPC.
        Vector3 lookAtPosition = playerTransform.position;
        lookAtPosition.y = transform.position.y;
        // This makes the NPC look towards the player but prevents it from tilting up or down.
        transform.LookAt(lookAtPosition);

        // --- THE STOPPING & ORBITING FIX ---
        // Only update the destination if the NPC is outside its stopping distance.
        // This prevents the "orbiting" behavior and allows the agent to actually stop.
        if (Vector3.Distance(transform.position, playerTransform.position) > agent.stoppingDistance)
        {
            agent.SetDestination(playerTransform.position);
        }

        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            // Restore the original stopping distance for other behaviors.
            agent.stoppingDistance = originalStoppingDistance;
            agent.ResetPath();
        }
    }
}