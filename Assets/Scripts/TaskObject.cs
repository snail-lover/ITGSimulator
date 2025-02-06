using UnityEngine;

public class TaskObject : MonoBehaviour
{
    [Header("Task Settings")]
    public string taskName;  // Name of the task (e.g., "Wash Hands", "Teach Class")
    public string action;    // Action to perform (e.g., "Wash", "Teach")
    public float duration = 3.0f; // Time it takes to complete this task (seconds)

    [Header("Optional Task Settings")]
    public Animator taskAnimator; // Animator to trigger animations, if applicable
    public string animationTrigger; // Name of the animation trigger to play

    [Header("Debug Options")]
    public bool showDebugGizmo = true; // Toggle debug visualization in the Scene view
    public Color gizmoColor = Color.yellow; // Color of the debug sphere in the Scene view

    /// <summary>
    /// Called by NPCs when they start interacting with this task.
    /// </summary>
    /// 

   public void Update()
    {
        //Debug.Log("TaskObject: " + duration);
    }
    public void PerformTask()
    {
        //Debug.Log($"Performing task: {taskName}. Action: {action}");

        // Trigger the optional animation if an Animator is set
        if (taskAnimator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            Debug.Log($"Triggering animation: {animationTrigger}");
            taskAnimator.SetTrigger(animationTrigger);
        }
    }

    /// <summary>
    /// Draws a gizmo to visualize the task location in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (showDebugGizmo)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.3f); // Draw a small sphere at the task's position
        }
    }
}
