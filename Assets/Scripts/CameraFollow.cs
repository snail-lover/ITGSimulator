// --- START OF FILE CameraFollow.cs ---

using UnityEngine;
using System.Collections.Generic; // Required for HashSet and List

public class CameraFollow : MonoBehaviour
{
    [Header("Core Settings")]
    public Transform target; // The target to follow
    public Vector3 offset = new Vector3(0, 15, -5); // Original offset definition
    public float smoothSpeed = 0.125f; // Smoothing factor (acts as fraction per frame without Time.deltaTime)

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    private float currentYaw;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f; // Adjust for top-down limits
    public float maxZoom = 20f;
    private float currentZoom;

    [Header("Wall Occlusion Settings")]
    public LayerMask wallLayer; // Assign the "Wall" layer you created in the Inspector
    public Vector3 targetOcclusionCheckOffset = Vector3.up * 1.0f; // Check slightly above the player's base
    private HashSet<WallTransparency> currentlyFadedWalls = new HashSet<WallTransparency>(); // Track walls currently faded

    void Start()
    {
        // --- From your requested script ---
        currentZoom = offset.magnitude; // Set initial zoom based on original offset distance

        // --- From fading logic (Initialization) ---
        currentlyFadedWalls = new HashSet<WallTransparency>();

        // --- Good practice initializations ---
        currentYaw = transform.eulerAngles.y; // Initialize yaw based on camera's starting rotation
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false; // Disable script if no target
        }
    }

    void LateUpdate() // Good for camera updates after player movement
    {
        if (target == null) return;

        // --- From your requested script ---
        HandleZoom();
        HandleRotation();
        UpdateCameraPosition(); // Calculate and move camera first (using your specific logic)

        // --- From fading logic ---
        HandleWallOcclusion();  // Then handle fading based on the camera's final position
    }

    // --- HandleZoom method from your requested script ---
    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom); // Clamp the zoom
    }

    // --- HandleRotation method from your requested script ---
    void HandleRotation()
    {
        if (Input.GetMouseButton(2)) // Rotate with middle mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * rotationSpeed;
        }
    }

    // --- UpdateCameraPosition method from your requested script ---
    void UpdateCameraPosition()
    {
        // Adjust the rotation to tilt the camera downward
        Quaternion rotation = Quaternion.Euler(45f, currentYaw, 0f); // 45° down, yaw rotates around the Y-axis

        // Calculate offset using offset.y for height and -currentZoom for distance along camera's Z
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom);

        // Calculate the desired world position
        Vector3 desiredPosition = target.position + rotatedOffset;

        // Smoothly move to the desired position using your original Lerp
        // Note: Without Time.deltaTime, smoothSpeed acts as a percentage (0.125 = 12.5% closer each frame)
        // This can feel different depending on frame rate.
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Make the camera look directly at the target's pivot point
        transform.LookAt(target);
    }

    // --- HandleWallOcclusion method (from previous working version) ---
    void HandleWallOcclusion()
    {
        // No changes needed here compared to the previous working version.
        // It uses the final transform.position calculated by UpdateCameraPosition.

        if (wallLayer == 0) return; // Skip if layer mask is not set

        Vector3 targetCheckPosition = target.position + targetOcclusionCheckOffset;
        Vector3 cameraPosition = transform.position; // Use the camera's current position
        Vector3 directionToTarget = targetCheckPosition - cameraPosition;
        float distanceToTarget = directionToTarget.magnitude;

        // Optional: Visualize the ray
        // Debug.DrawRay(cameraPosition, directionToTarget.normalized * distanceToTarget, Color.magenta);

        HashSet<WallTransparency> wallsHitThisFrame = new HashSet<WallTransparency>();
        RaycastHit[] hits = Physics.RaycastAll(cameraPosition, directionToTarget.normalized, distanceToTarget, wallLayer);

        foreach (RaycastHit hit in hits)
        {
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
                 currentlyFadedWalls.Remove(wallToFade);
            }
        }

        currentlyFadedWalls.UnionWith(wallsHitThisFrame);

        // Cleanup null references (if walls get destroyed)
        currentlyFadedWalls.RemoveWhere(wall => wall == null);
    }
}
// --- END OF FILE CameraFollow.cs ---