// --- START OF FILE VerifyOrForgetActivity.cs ---

using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPC")]
[TaskDescription("Verifies if a remembered activity is still present and at its expected location.")]
public class VerifyOrForgetActivity : Action
{

    public SharedNpcBrain NpcBrain;

    public SharedString InActivityID;

    public SharedGameObject OutActivityObject;

    private NpcBrain brain;

    // A small tolerance to account for NavMeshAgent's stopping distance.
    private const float ARRIVAL_TOLERANCE = 2.0f;

    public override void OnStart()
    {
        brain = NpcBrain.Value;
    }

    public override TaskStatus OnUpdate()
    {
        if (brain == null || string.IsNullOrEmpty(InActivityID.Value))
        {
            Debug.LogError("VerifyOrForgetActivity: Brain or ActivityID is missing.");
            return TaskStatus.Failure;
        }

        string targetID = InActivityID.Value;

        // 1. Ask the ActivityManager for the LIVE object using its ID.
        ActivityObject liveActivity = ActivityManager.Instance.GetActivityByID(targetID);

        // 2. Check if the activity object even exists in the world anymore.
        if (liveActivity == null)
        {
            Debug.LogError($"<color=red>Verification FAILURE: Activity '{targetID}' no longer exists (was destroyed). Forgetting it.</color>");
            ForgetActivity(targetID);
            return TaskStatus.Failure;
        }

        // 3. The object exists. Now check if the NPC is at its target position.
        Vector3 expectedPosition = liveActivity.GetTargetPosition();
        float distanceToTarget = Vector3.Distance(brain.transform.position, expectedPosition);

        // 4. Compare the NPC's distance to the target position against our tolerance.
        if (distanceToTarget <= ARRIVAL_TOLERANCE)
        {
            // SUCCESS! The object exists and we are in the right spot to use it.
            Debug.Log($"<color=green>Verification SUCCESS: Arrived at '{targetID}'s' target location.</color>");
            OutActivityObject.Value = liveActivity.gameObject; // Pass the live object to the next task.
            return TaskStatus.Success;
        }
        else
        {
            // FAILURE! The object exists, but it has MOVED since we started walking towards it.
            // Our memory of its location is stale.
            Debug.LogError($"<color=red>Verification FAILURE: Arrived at stale location for '{targetID}'. The object has moved. Forgetting old position.</color>");
            ForgetActivity(targetID);
            return TaskStatus.Failure;
        }
    }

    private void ForgetActivity(string activityID)
    {
        if (brain.RuntimeData.rememberedActivityLocations.ContainsKey(activityID))
        {
            brain.RuntimeData.rememberedActivityLocations.Remove(activityID);
        }
    }
}