// --- START OF FILE ActivityObject.cs ---

using UnityEngine;
using System.Collections.Generic;
using System;

// A Rigidbody is essential for any physics-based interactions like dragging.
[RequireComponent(typeof(Rigidbody))]
public class ActivityObject : MonoBehaviour
{
    [Header("Activity Definition")]
    [Tooltip("A unique identifier for this activity (e.g., 'CookAtStove', 'DragCube'). MUST BE UNIQUE.")]
    public string activityID = "UnnamedActivityID";
    [Tooltip("How long the NPC will perform this activity.")]
    public float duration = 5.0f;
    [Tooltip("The name of the animation boolean to set in the Animator.")]
    public string animationBoolName;

    [Tooltip("The effects this activity has on an NPC's needs.")]
    public List<NeedEffect> needEffects;

    [Header("Personality System")]
    [Tooltip("Tags that describe this activity for the goal system (e.g., 'Academic', 'Fitness', 'Social').")]
    public List<string> activityTags;

    [Tooltip("(Optional) Precise target spot and orientation for the NPC relative to this object.")]
    public Transform specificTargetPoint;

    [Header("Debug Options")]
    public bool showDebugGizmo = true;
    public Color gizmoColor = Color.cyan;

    // --- Event to notify listeners (like NPCs) that this object has been moved ---
    public static event Action<ActivityObject> OnActivityMoved;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // --- CRITICAL VALIDATION ---
        if (string.IsNullOrEmpty(activityID) || activityID == "UnnamedActivityID")
        {
            Debug.LogError($"ActivityObject on '{gameObject.name}' has an invalid or default activityID! Please assign a unique ID.", this);
            this.enabled = false;
            return;
        }

        // Register with the manager so it's globally findable.
        if (ActivityManager.Instance != null)
        {
            ActivityManager.Instance.RegisterActivity(this);
        }
        else
        {
            Debug.LogError($"ActivityObject '{activityID}' cannot find ActivityManager.Instance in the scene! Ensure an ActivityManager exists and is active.", this);
        }
    }

    private void OnDestroy()
    {
        // When destroyed, it must be removed from the manager to prevent errors.
        if (ActivityManager.Instance != null)
        {
            ActivityManager.Instance.UnregisterActivity(this);
        }
    }

    /// <summary>
    /// This method MUST be called by your player's drag-and-drop script
    /// IMMEDIATELY AFTER the player releases the object. It re-enables physics
    /// and notifies the AI system of the move.
    /// </summary>
    public void OnDrop()
    {
        // Ensure the rigidbody is no longer kinematic so it can settle.
        rb.isKinematic = false;

        Debug.Log($"Activity '{activityID}' was dropped. Broadcasting its new position.");
        // Broadcast the event to any interested listeners (like the NpcController).
        OnActivityMoved?.Invoke(this);
    }

    /// <summary>
    /// This method MUST be called by your player's drag-and-drop script
    /// WHEN the player first picks up the object. It disables physics
    /// to allow for smooth dragging.
    /// </summary>
    public void OnPickup()
    {
        // When being dragged, the object should not be affected by physics.
        rb.isKinematic = true;
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

// The NeedEffect class remains the same.
[System.Serializable]
public class NeedEffect
{
    public string needName;
    [Tooltip("How much this activity changes the need. Negative values fulfill the need.")]
    public float effectValue;
}