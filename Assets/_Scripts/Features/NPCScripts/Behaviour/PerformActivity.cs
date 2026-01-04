// PerformActivity.cs
using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPC")]
[TaskDescription("Faces the activity, plays an animation for its duration, and applies the need effects upon completion.")]
public class PerformActivity : Action
{
    // --- INPUTS ---
    public SharedNpcBrain NpcBrain;

    public SharedGameObject InActivityTarget;

    // --- Private state for the task ---
    private NpcBrain brain;
    private NpcMovement movement;
    private Animator animator;
    private ActivityObject activity;
    private float taskStartTime;
    private bool hasStartedAction;

    public override void OnStart()
    {
        // Get all our references from the brain
        brain = NpcBrain.Value;
        movement = brain.Controller.Movement;
        animator = brain.NpcAnimator;
        activity = InActivityTarget.Value.GetComponent<ActivityObject>();
        hasStartedAction = false;

        // Safety check
        if (activity == null)
        {
            Debug.LogError("PerformActivity: InActivityTarget is not a valid ActivityObject!");
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (activity == null)
        {
            return TaskStatus.Failure;
        }

        // --- ONE-TIME SETUP on the first update tick ---
        if (!hasStartedAction)
        {
            // Command the NPC to turn and face the correct direction
            movement.FaceDirection(activity.GetTargetRotation() * Vector3.forward, 0.5f);

            // Play the animation, if one is specified
            if (!string.IsNullOrEmpty(activity.animationBoolName))
            {
                animator.SetBool(activity.animationBoolName, true);
            }

            taskStartTime = Time.time;
            hasStartedAction = true;
            Debug.Log($"<color=cyan>Performing activity '{activity.activityID}' for {activity.duration} seconds.</color>");
        }

        // --- CHECK FOR COMPLETION ---
        // Has the duration passed?
        if (Time.time >= taskStartTime + activity.duration)
        {
            Debug.LogWarning($"<color=green>Finished activity '{activity.activityID}'. Applying effects.</color>");
            ApplyNeedEffects();
            return TaskStatus.Success;
        }

        // If we haven't finished, the task is still running.
        return TaskStatus.Running;
    }

    /// <summary>
    /// Applies the need changes from the ActivityObject to the NPC's runtime data.
    /// </summary>
    private void ApplyNeedEffects()
    {
        NpcRuntimeData data = brain.RuntimeData;

        foreach (var effect in activity.needEffects)
        {
            if (data.needs.ContainsKey(effect.needName))
            {
                float oldValue = data.needs[effect.needName].currentValue;
                // Add the effect value (e.g., a negative value lowers the need)
                data.needs[effect.needName].currentValue += effect.effectValue;
                // Clamp the value to ensure it stays between 0 and 100
                data.needs[effect.needName].currentValue = Mathf.Clamp(data.needs[effect.needName].currentValue, 0, 100);

                Debug.Log($"Applied effect for '{effect.needName}'. Value changed from {oldValue:F1} to {data.needs[effect.needName].currentValue:F1}.");
            }
        }
    }

    /// <summary>
    /// This is CRITICAL. It's called when the task ends for any reason (Success, Failure, or Aborted).
    /// We must ensure the animation is turned off.
    /// </summary>
    public override void OnEnd()
    {
        if (animator != null && activity != null && !string.IsNullOrEmpty(activity.animationBoolName))
        {
            animator.SetBool(activity.animationBoolName, false);
        }

        // Good housekeeping: Clear the target variable so the NPC doesn't get stuck thinking about the same object.
        if (InActivityTarget != null)
        {
            InActivityTarget.Value = null;
        }
    }
}