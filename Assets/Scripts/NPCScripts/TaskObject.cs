using UnityEngine;

public class TaskObject : MonoBehaviour
{
    [Header("Task Definition")]
    [Tooltip("A unique identifier for this task (e.g., 'CookAtStove', 'ReadOnCouch'). This is how NPCs will find it.")]
    public string taskID = "UnnamedTaskID"; // CHANGED: Was taskName, now is the crucial ID.
    public float duration = 3.0f;
    public string animationBoolName;

    [Tooltip("(Optional) Precise target spot and orientation for the NPC.")]
    public Transform specificTargetPoint;

    [Header("Debug Options")]
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.cyan;

    // NEW: Register with the TaskManager
    private void Awake()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.RegisterTask(this);
        }
        else
        {
            // This error is less likely now, but good to keep. It would mean the TaskManager itself is missing or disabled.
            Debug.LogError($"TaskObject '{taskID}' cannot find TaskManager.Instance in the scene! Ensure a TaskManager exists and is active.", this);
        }
    }

    public Vector3 GetTargetPosition()
    {
        return specificTargetPoint != null ? specificTargetPoint.position : transform.position;
    }

    public Quaternion GetTargetRotation()
    {
        return specificTargetPoint != null ? specificTargetPoint.rotation : transform.rotation;
    }

    private void OnDrawGizmos()
    {
        if (showDebugGizmo)
        {
            Gizmos.color = gizmoColor;
            Vector3 pos = GetTargetPosition();
            Gizmos.DrawSphere(pos, 0.3f);

            Quaternion rot = GetTargetRotation();
            Gizmos.DrawLine(pos, pos + (rot * Vector3.forward) * 0.5f);
        }
    }
}