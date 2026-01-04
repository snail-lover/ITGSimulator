using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using UnityEngine.AI; 

[TaskCategory("NPC")]
[TaskDescription("Finds a random point within a certain radius on the NavMesh.")]
public class FindRandomPoint : Action
{
    public SharedNpcBrain NpcBrain;
    public SharedFloat Radius = 10f; // The max distance to wander

    // We will store the found point in this variable to pass to the MoveTo task
    public SharedVector3 StoreResult;

    public override TaskStatus OnUpdate()
    {
        if (NpcBrain.Value == null) return TaskStatus.Failure;

        Vector3 randomDirection = Random.insideUnitSphere * Radius.Value;
        randomDirection += NpcBrain.Value.transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, Radius.Value, NavMesh.AllAreas))
        {
            // We found a valid point on the NavMesh
            StoreResult.Value = hit.position;
            return TaskStatus.Success;
        }

        // Could not find a valid point. Fail so the tree tries again.
        return TaskStatus.Failure;
    }
}