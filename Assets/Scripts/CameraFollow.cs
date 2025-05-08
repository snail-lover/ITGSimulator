// --- START OF FILE CameraFollow.cs ---

using UnityEngine;
using System.Collections.Generic; // Required for HashSet and List

public class CameraFollow : MonoBehaviour
{
    [Header("Core Settings")]
    public Transform target; // The target to follow
    public Vector3 offset = new Vector3(0, 15, -5); // Original offset definition
    public float smoothSpeed = 0.125f; // Smoothing factor

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
    public float occlusionSphereCastRadius = 0.5f; // <--- NEW: Radius for the spherecast
    private HashSet<WallTransparency> currentlyFadedWalls = new HashSet<WallTransparency>();

    void Start()
    {
        currentZoom = offset.magnitude;
        currentlyFadedWalls = new HashSet<WallTransparency>();
        currentYaw = transform.eulerAngles.y;
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

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
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * rotationSpeed;
        }
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(45f, currentYaw, 0f);
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom);
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target);
    }

    void HandleWallOcclusion()
    {
        if (wallLayer == 0) return; // Skip if layer mask is not set
        if (occlusionSphereCastRadius <= 0f) // If radius is zero or negative, it might not work as expected or just be a raycast
        {
            // Optionally, you could fall back to a simple RaycastAll or log a warning
            // For now, we'll assume a positive radius is intended for SphereCast
            // Debug.LogWarning("Occlusion SphereCast Radius is 0 or negative. Wall occlusion might not work as intended.");
        }

        Vector3 targetCheckPosition = target.position + targetOcclusionCheckOffset;
        Vector3 cameraPosition = transform.position;
        Vector3 directionToTarget = targetCheckPosition - cameraPosition;
        float distanceToTarget = directionToTarget.magnitude;

        // Optional: Visualize the central ray of the spherecast
        // Debug.DrawRay(cameraPosition, directionToTarget.normalized * distanceToTarget, Color.magenta);
        // Note: Visualizing the actual sphere sweep is more complex, often done by drawing spheres at start/end points in OnDrawGizmos.

        HashSet<WallTransparency> wallsHitThisFrame = new HashSet<WallTransparency>();

        // --- MODIFIED LINE ---
        // Old: RaycastHit[] hits = Physics.RaycastAll(cameraPosition, directionToTarget.normalized, distanceToTarget, wallLayer);
        RaycastHit[] hits = Physics.SphereCastAll(
            cameraPosition,                       // Origin of the sphere
            occlusionSphereCastRadius,            // Radius of the sphere
            directionToTarget.normalized,         // Direction of the sweep
            distanceToTarget,                     // Max distance of the sweep
            wallLayer                             // Layer mask to interact with
        );
        // --- END OF MODIFIED LINE ---

        foreach (RaycastHit hit in hits)
        {
            // The rest of the logic remains the same, as SphereCastAll also returns RaycastHit objects
            if (hit.collider != null)
            {
                WallTransparency wall = hit.collider.GetComponent<WallTransparency>();
                if (wall != null)
                {
                    wall.FadeOut();
                    wallsHitThisFrame.Add(wall);
                }
            }
        }

        // --- The rest of your wall fading logic (no changes needed here) ---
        List<WallTransparency> wallsToFadeIn = new List<WallTransparency>();
        foreach (WallTransparency previouslyFadedWall in currentlyFadedWalls)
        {
            if (previouslyFadedWall != null && !wallsHitThisFrame.Contains(previouslyFadedWall))
            {
                wallsToFadeIn.Add(previouslyFadedWall);
            }
        }

        foreach (var wallToFade in wallsToFadeIn)
        {
            if (wallToFade != null)
            {
                 wallToFade.FadeIn();
            }
        }
        // Important: Clear and re-populate currentlyFadedWalls correctly
        currentlyFadedWalls.Clear(); // Clear all old ones
        foreach(var wall in wallsHitThisFrame) // Add all walls hit this frame
        {
            currentlyFadedWalls.Add(wall);
        }


        // Cleanup null references (if walls get destroyed), though the clear/re-add above handles most of this
        // currentlyFadedWalls.RemoveWhere(wall => wall == null); // Can still be useful if walls are destroyed mid-fade
    }
}
// --- END OF FILE CameraFollow.cs ---