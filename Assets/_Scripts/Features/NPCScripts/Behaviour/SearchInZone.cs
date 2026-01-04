// SearchInZone.cs
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPC")]
[TaskDescription("Meanders within the current zone for a set duration, looking for a needed activity.")]
public class SearchInZone : Action
{
    public SharedNpcBrain NpcBrain;
    public float searchDuration = 10f; // How long to search for
    public float meanderRadius = 5f;   // How far to wander from the center

    private NpcBrain brain;
    private NpcMovement movement;
    private NpcNeed criticalNeed;
    private float searchStartTime;
    private SearchableZone currentZone;

    public override void OnStart()
    {
        brain = NpcBrain.Value;
        movement = brain.Controller.Movement;
        searchStartTime = Time.time;

        // Find the most critical need again to know what we're looking for
        FindMostCriticalNeed();
        // Find the zone we are currently in
        currentZone = brain.GetZoneForPosition(brain.transform.position);

        Debug.Log($"<color=lightblue>Starting to search in zone '{currentZone?.zoneName}' for {searchDuration} seconds.</color>");
    }

    public override TaskStatus OnUpdate()
    {
        if (brain == null || movement == null || criticalNeed == null || currentZone == null)
        {
            return TaskStatus.Failure;
        }

        // CONDITION 1: Have we run out of time?
        if (Time.time > searchStartTime + searchDuration)
        {
            Debug.LogError($"<color=red>Search failed. Time is up.</color>");
            return TaskStatus.Failure;
        }

        // CONDITION 2: Have we found what we're looking for?
        foreach (var activity in brain.VisibleActivities)
        {
            if (DoesActivitySatisfyNeed(activity, criticalNeed.name))
            {
                Debug.LogWarning($"<color=green>Search successful! Found '{activity.activityID}'.</color>");
                movement.Stop(); // Stop meandering
                return TaskStatus.Success;
            }
        }

        // CONDITION 3: If we're not moving, find a new spot to meander to.
        if (!movement.IsMoving)
        {
            Vector3 randomPoint = FindRandomPointInZone();
            movement.MoveTo(randomPoint);
        }

        // If none of the above, the search continues.
        return TaskStatus.Running;
    }

    private Vector3 FindRandomPointInZone()
    {
        Vector3 randomDirection = Random.insideUnitSphere * meanderRadius;
        randomDirection += currentZone.transform.position;

        // Find the nearest point on the NavMesh to our random spot
        NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, meanderRadius, NavMesh.AllAreas);
        return navHit.position;
    }

    private void FindMostCriticalNeed()
    {
        // Duplicated logic to ensure we always know the most current need
        criticalNeed = null;
        float maxNeed = 0f;
        foreach (var need in brain.RuntimeData.needs.Values)
        {
            if (need.currentValue > maxNeed)
            {
                maxNeed = need.currentValue;
                criticalNeed = need;
            }
        }
    }

    private bool DoesActivitySatisfyNeed(ActivityObject activity, string needName)
    {
        // Duplicated logic to check if an activity is useful
        foreach (var effect in activity.needEffects)
        {
            if (effect.needName == needName && effect.effectValue < 0) return true;
        }
        return false;
    }
}