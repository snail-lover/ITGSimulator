// NpcPerception.cs (Corrected and Refactored for Behavior Designer)

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

        GetComponent<SphereCollider>().radius = awarenessRadius;
        // This should be a trigger to detect objects entering the radius.
        GetComponent<SphereCollider>().isTrigger = true;

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
            // --- MODIFIED LOGIC ---
            // 1. Clear the brain's short-term perception from the last tick.
            _brain.ClearCurrentPerception();

            // 2. Scan the environment and populate the brain's perception data.
            ScanForObjects();

            // 3. Handle player visibility for interaction prompts (this is fine).
            HandlePlayerVisibility();

            yield return wait;
        }
    }

    /// <summary>
    /// Scans for all relevant objects (Activities, Draggables) and updates the NpcBrain's
    /// real-time perception data.
    /// </summary>
    private void ScanForObjects()
    {
        // --- NEW, MORE DIRECT LOGIC FOR DRAGGED OBJECT ---
        // First, check if a dragged item exists globally.
       // if (Draggable.CurrentlyDraggedItem != null)
            // {
            // If it does, check if we can see it.
            //  if (CanSeeTarget(Draggable.CurrentlyDraggedItem.transform))
            //  {
            // If we can, update the brain. This is much more direct.
            //  _brain.VisibleDraggedObject = Draggable.CurrentlyDraggedItem;
            // For debugging, confirm this now works:
            //  Debug.Log($"<color=cyan>[Perception] Successfully saw dragged object '{_brain.VisibleDraggedObject.name}' and sent it to the brain.</color>");
            //  //}
            //}


            // --- ORIGINAL LOGIC FOR ACTIVITIES (This is still good) ---
            Collider[] colliders = Physics.OverlapSphere(transform.position, awarenessRadius, visionLayerMask);
        foreach (var col in colliders)
        {
            // Check for Activity Objects
            ActivityObject activity = col.GetComponentInParent<ActivityObject>();
            if (activity != null && CanSeeTarget(activity.transform))
            {
                _brain.LearnAboutActivity(activity);
                _brain.VisibleActivities.Add(activity);
            }
        }
    }

    private void HandlePlayerVisibility()
    {
        bool playerIsCurrentlyVisible = false;
        if (_npcController != null && _npcController.PlayerTransform != null)
        {
            playerIsCurrentlyVisible = CanSeeTarget(_npcController.PlayerTransform);
        }

        if (playerIsCurrentlyVisible != _playerWasVisible)
        {
            _playerWasVisible = playerIsCurrentlyVisible;
        }
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
            // The ray hit something. Return true only if the thing it hit is the target
            // or a child of the target. This prevents seeing through walls.
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        // Raycast hit nothing, meaning there's a clear line of sight.
        return true;
    }

    // --- (OnDrawGizmos remains the same) ---
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || eyeTransform == null) return;

        //if (Draggable.CurrentlyDraggedItem != null)
        //{
        // Check for visibility and set the color accordingly.
        //if (CanSeeTarget(Draggable.CurrentlyDraggedItem.transform))
        //{
        //Gizmos.color = Color.green; // We can see it!
        //}
        //else
        //{
        //Gizmos.color = Color.red; // We can't see it.
        //}
        // Draw the line from the eye to the dragged item's position.
        //Gizmos.DrawLine(eyeTransform.position, Draggable.CurrentlyDraggedItem.transform.position);
        //}

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