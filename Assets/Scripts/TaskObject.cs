using UnityEngine;

public class TaskObject : MonoBehaviour
{
    [Header("Task Definition")]
    public string taskName = "Unnamed Task"; 
    [Tooltip("How long the NPC stays performing the action after arriving (seconds).")]
    public float duration = 3.0f;
    [Tooltip("The Trigger name in the NPC's Animator Controller for this task's action.")]
    public string animationBoolName;

    [Tooltip("(Optional) Precise target spot and orientation for the NPC.")]
    public Transform specificTargetPoint;

    [Header("Debug Options")]
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.cyan; // Changed color for distinction


    public Vector3 GetTargetPosition() /// Gets the target position for the NPC's NavMeshAgent.

    {
        return specificTargetPoint != null ? specificTargetPoint.position : transform.position;
    }

    public Quaternion GetTargetRotation() /// Gets the target rotation for the NPC upon arrival (if specified).
    {
        return specificTargetPoint != null ? specificTargetPoint.rotation : transform.rotation; 
    }

    private void OnDrawGizmos()
    {
        if (showDebugGizmo)
        {
            Vector3 pos = GetTargetPosition();
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(pos, 0.3f);
 
            if (specificTargetPoint != null) // Optionally draw the forward direction if using specificTargetPoint
            {
                Gizmos.DrawLine(pos, pos + specificTargetPoint.forward * 0.5f);
            }
        }
    }
}