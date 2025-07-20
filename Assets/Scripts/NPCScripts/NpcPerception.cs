// --- START OF FILE NpcPerception.cs ---

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SphereCollider))]
public class NpcPerception : MonoBehaviour
{
    // --- COMPONENT REFERENCES ---
    private NpcController _npcController;
    private NpcBrain _brain;
    private NpcInteraction _interaction; // <--- THIS WAS THE MISSING LINE

    [Header("Awareness Settings")]
    public float awarenessRadius = 15f;
    public float perceptionTickRate = 0.25f;

    [Header("Vision Settings")]
    [Range(0, 360)]
    public float visionAngle = 120f;
    public Transform eyeTransform;
    [Tooltip("Select all layers that should interact with vision, including targets (Activities) AND obstacles (Walls).")]
    public LayerMask visionLayerMask;

    [Header("Debug Settings")]
    public bool showDebugGizmos = true;
    public Transform debugTarget;
    [Range(10, 100)]
    public int gizmoRayCount = 50;
    public Color gizmoVisibleColor = new Color(1f, 0.92f, 0.016f, 0.2f);

    private bool _playerWasVisible = false;

    private void Awake()
    {
        _npcController = GetComponentInParent<NpcController>();
        _brain = GetComponentInParent<NpcBrain>();
        _interaction = GetComponentInParent<NpcInteraction>(); // This line will now work correctly
        var sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.radius = awarenessRadius;
        sphereCollider.isTrigger = false; // Note: For OnTriggerEnter, this should be true. For OverlapSphere, false is fine.

        if (eyeTransform == null) eyeTransform = transform;
    }

    private void Start()
    {
        StartCoroutine(PerceptionLoop());
    }

    private IEnumerator PerceptionLoop()
    {
        var wait = new WaitForSeconds(perceptionTickRate);
        while (true)
        {
            ScanForActivities();

            bool playerIsCurrentlyVisible = false;
            if (_npcController != null && _npcController.PlayerTransform != null)
            {
                playerIsCurrentlyVisible = CanSeeTarget(_npcController.PlayerTransform);
            }

            if (playerIsCurrentlyVisible != _playerWasVisible)
            {
                _interaction.UpdatePlayerVisibility(playerIsCurrentlyVisible);
                _playerWasVisible = playerIsCurrentlyVisible;
            }

            yield return wait;
        }
    }

    private void ScanForActivities()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, awarenessRadius, visionLayerMask);
        foreach (var col in colliders)
        {
            ActivityObject activity = col.GetComponentInParent<ActivityObject>();
            if (activity != null && CanSeeTarget(activity.transform))
            {
                _brain.LearnAboutActivity(activity);
            }

            Draggable draggable = col.GetComponentInParent<Draggable>();
            if (draggable != null && draggable == Draggable.CurrentlyDraggedItem && CanSeeTarget(draggable.transform))
            {
                _brain.NoticeDraggedObject(draggable);
            }
        }
    }

    public bool IsTargetInAwarenessRange(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= awarenessRadius;
    }

    public bool CanSeeTarget(Transform target)
    {
        if (target == null || eyeTransform == null) return false;

        Vector3 position = eyeTransform.position;
        Vector3 directionToTarget = (target.position - position).normalized;

        float distanceToTarget = Vector3.Distance(position, target.position);
        if (distanceToTarget > awarenessRadius) return false;
        if (Vector3.Angle(eyeTransform.forward, directionToTarget) > visionAngle / 2) return false;
        if (Physics.Raycast(position, directionToTarget, out RaycastHit hit, distanceToTarget, visionLayerMask))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }
        return true;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || eyeTransform == null) return;

        if (debugTarget != null)
        {
            Gizmos.color = CanSeeTarget(debugTarget) ? Color.green : Color.red;
            Gizmos.DrawLine(eyeTransform.position, debugTarget.position);
        }

#if UNITY_EDITOR
        Handles.color = gizmoVisibleColor;
        Vector3 eyePosition = eyeTransform.position;
        Vector3 forward = eyeTransform.forward;
        float angleStep = visionAngle / gizmoRayCount;
        float startAngle = -visionAngle / 2;
        Vector3 previousRayEndpoint = Vector3.zero;
        for (int i = 0; i <= gizmoRayCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 direction = rotation * forward;
            Vector3 currentRayEndpoint;
            if (Physics.Raycast(eyePosition, direction, out RaycastHit hit, awarenessRadius, visionLayerMask))
            {
                currentRayEndpoint = hit.point;
            }
            else
            {
                currentRayEndpoint = eyePosition + direction * awarenessRadius;
            }
            if (i > 0)
            {
                Handles.DrawAAConvexPolygon(eyePosition, previousRayEndpoint, currentRayEndpoint);
            }
            previousRayEndpoint = currentRayEndpoint;
        }
#endif
    }
}