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
    private HashSet<WallTransparency> currentlyFadedWalls = new HashSet<WallTransparency>();

    public bool isManuallyControlled = false; // Flag to disable automatic follow

    void Start()
    {
        currentZoom = offset.magnitude;
        currentlyFadedWalls = new HashSet<WallTransparency>();
        currentYaw = transform.eulerAngles.y;
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false; // Or handle this more gracefully
        }
    }

    void LateUpdate()
    {
        // --- MODIFIED ---
        if (isManuallyControlled || target == null) return; // If manually controlled or no target, do nothing

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
        Quaternion rotation = Quaternion.Euler(45f, currentYaw, 0f); // Assuming 45 is your desired pitch
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom); // Use offset.y for consistent height component of offset
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position + targetOcclusionCheckOffset); // Look at the target check point for better framing
    }


    void HandleWallOcclusion()
    {
        if (wallLayer == 0) return;
        if (occlusionSphereCastRadius <= 0f) return;

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
            }
        }
        currentlyFadedWalls.Clear();
        foreach (var wall in wallsHitThisFrame)
        {
            currentlyFadedWalls.Add(wall);
        }
    }

    // --- NEW PUBLIC METHOD ---
    public void SetManualControl(bool manual)
    {
        isManuallyControlled = manual;
        if (!manual)
        {
            // When releasing control, you might want to smoothly transition back
            // or snap. For now, it will just resume following from its current position.
            // You could also re-initialize currentYaw and currentZoom based on the current
            // camera transform if the cutscene changed them significantly.
            // For example:
            // currentYaw = transform.eulerAngles.y;
            // Vector3 offsetFromTarget = transform.position - target.position;
            // currentZoom = offsetFromTarget.magnitude; // This might not be perfect with your rotated offset
        }
        Debug.Log($"[CameraFollow] Manual Control set to: {manual}");
    }
}