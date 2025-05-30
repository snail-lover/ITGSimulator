// --- START OF FILE CameraFollow.cs ---

using UnityEngine;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour
{
    [Header("Core Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 15, -5);
    public float smoothSpeed = 0.125f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    private float currentYaw;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    private float currentZoom;

    [Header("Wall Occlusion Settings")]
    public LayerMask wallLayer;
    public Vector3 targetOcclusionCheckOffset = Vector3.up * 1.0f;
    public float occlusionSphereCastRadius = 0.5f;
    private HashSet<WallTransparency> currentlyAffectedWalls = new HashSet<WallTransparency>(); // Renamed for clarity

    public bool isManuallyControlled = false;

    void Start()
    {
        currentZoom = offset.magnitude;
        currentYaw = transform.eulerAngles.y;
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false;
            return;
        }
        // Initialize with a new HashSet
        currentlyAffectedWalls = new HashSet<WallTransparency>();
    }

    void LateUpdate()
    {
        if (isManuallyControlled || target == null) return;

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
        HandleWallOcclusion();
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(2)) // Middle mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * rotationSpeed;
        }
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(45f, currentYaw, 0f); // Assuming 45 is your desired pitch
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom);
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position + targetOcclusionCheckOffset);
    }

    void HandleWallOcclusion()
    {
        if (wallLayer == 0 || occlusionSphereCastRadius <= 0f) return;

        Vector3 targetCheckPosition = target.position + targetOcclusionCheckOffset;
        Vector3 cameraPosition = transform.position;
        Vector3 directionToTarget = targetCheckPosition - cameraPosition;
        float distanceToTarget = directionToTarget.magnitude;

        HashSet<WallTransparency> wallsHitThisFrame = new HashSet<WallTransparency>();
        RaycastHit[] hits = Physics.SphereCastAll(
            cameraPosition,
            occlusionSphereCastRadius,
            directionToTarget.normalized,
            distanceToTarget,
            wallLayer
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != null)
            {
                WallTransparency wall = hit.collider.GetComponent<WallTransparency>();
                if (wall != null)
                {
                    float hitDistance = hit.distance; // Distance from camera to the hit point on the wall
                                                      // Example: Smaller hole if camera is very close to the wall surface
                                                      // float desiredRadius = Mathf.Lerp(maxHoleRadius, minHoleRadius, Mathf.InverseLerp(0, closeDistanceThreshold, hitDistance));
                                                      // desiredRadius = Mathf.Clamp(desiredRadius, minHoleRadius, maxHoleRadius);

                    // Or, simpler: a fixed radius for now, and animate it in WallTransparency
                    wall.ActivatePartialFade(hit.point); // Keep current call, animation is in WallTransparency
                    wallsHitThisFrame.Add(wall);
                }
            }
        }

        // Find walls that were affected last frame but not this frame
        List<WallTransparency> wallsToDeactivate = new List<WallTransparency>();
        foreach (WallTransparency previouslyAffectedWall in currentlyAffectedWalls)
        {
            if (previouslyAffectedWall != null && !wallsHitThisFrame.Contains(previouslyAffectedWall))
            {
                wallsToDeactivate.Add(previouslyAffectedWall);
            }
        }

        foreach (var wallToStopFading in wallsToDeactivate)
        {
            if (wallToStopFading != null)
            {
                // Tell the wall to deactivate its fade zone
                wallToStopFading.DeactivatePartialFade();
            }
        }

        // Update the set of currently affected walls
        currentlyAffectedWalls = wallsHitThisFrame;
    }

    public void SetManualControl(bool manual)
    {
        isManuallyControlled = manual;
        Debug.Log($"[CameraFollow] Manual Control set to: {manual}");
    }
}
// --- END OF FILE CameraFollow.cs ---