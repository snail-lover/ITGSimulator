using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraFollow : MonoBehaviour
{
    [Header("Core Settings")]
    public Transform target;
    public Vector3 initialOffsetFromTarget = new Vector3(0, 15, -5); // Renamed for clarity
    public float positionSmoothFactor = 10f; // Adjusted from smoothSpeed

    [Header("Rotation Settings")]
    public float yawSpeed = 5f;         // Renamed from rotationSpeed for clarity
    public float pitchSpeed = 3f;       // Speed for vertical rotation
    public float minPitch = 10f;        // Min angle (looking slightly up/level)
    public float maxPitch = 85f;        // Max angle (looking almost straight down)
    private float currentYaw;
    private float currentPitch;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    private float currentZoom;

    [Header("Wall Occlusion Settings")]
    public LayerMask wallLayer;
    public Vector3 targetOcclusionCheckOffset = Vector3.up * 1.0f;
    public float occlusionSphereCastRadius = 0.5f;
    private HashSet<WallTransparency> currentlyAffectedWalls = new HashSet<WallTransparency>();

    public bool isManuallyControlled = false;

    // For jitter reduction (if you implemented it from previous advice)
    private Vector3 smoothedTargetPos;
    public float targetPositionSmoothFactor = 15f;

    // --- Added fields for transition and manual control ---
    private Coroutine _transitionCoroutine;
    private Vector3 _manualTargetPosition;
    private Quaternion _manualTargetRotation;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false;
            return;
        }

        // 1. Set camera's initial absolute position based on target and initialOffsetFromTarget
        transform.position = target.position + initialOffsetFromTarget;

        // 2. Initialize smoothed target position (if using this feature)
        smoothedTargetPos = target.position;

        // 3. Calculate initial zoom (distance from camera to target's pivot)
        currentZoom = Vector3.Distance(transform.position, target.position);
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom); // Ensure initial zoom is valid

        // 4. Calculate initial pitch and yaw for the camera to look at the target spot
        // The direction from the camera to the point it should look at:
        Vector3 directionToLookPoint = (target.position + targetOcclusionCheckOffset) - transform.position;
        Quaternion initialLookRotation = Quaternion.LookRotation(directionToLookPoint);
        currentYaw = initialLookRotation.eulerAngles.y;
        currentPitch = initialLookRotation.eulerAngles.x;

        // 5. Clamp initial pitch to be within defined bounds
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Initialize HashSet for wall occlusion
        currentlyAffectedWalls = new HashSet<WallTransparency>();
    }

    void LateUpdate()
    {
        if (target == null) return; // Ensure target hasn't been destroyed

        // Smooth the target's position that the camera will use (optional, for jitter reduction)
        smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, target.position, targetPositionSmoothFactor * Time.deltaTime);

        if (isManuallyControlled)
        {
            // If a transition is active, it handles position/rotation.
            // If no transition, but still manually controlled, camera stays put or is controlled by other means.
            // For the computer screen, after transition, it will stay put until SetManualControl(false)
            if (_transitionCoroutine == null && isManuallyControlled)
            {
                // Optionally, force the camera to stay at the manual target position/rotation:
                // transform.position = _manualTargetPosition;
                // transform.rotation = _manualTargetRotation;
            }
            return; // IMPORTANT: If manually controlled, don't do the default follow logic.
        }

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
        HandleWallOcclusion(); // Ensure this uses smoothedTargetPos if target smoothing is active
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
            currentYaw += mouseX * yawSpeed;

            float mouseY = Input.GetAxis("Mouse Y");
            currentPitch -= mouseY * pitchSpeed; // Subtract to invert (mouse up = camera tilts up)
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }
    }

    void UpdateCameraPosition()
    {
        // 1. Calculate the desired rotation based on currentYaw and currentPitch
        Quaternion desiredRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // 2. Calculate the offset from the target's pivot point:
        //    Start with a vector pointing straight back, rotate it, then scale by zoom.
        //    (Camera's forward is -Z, so Vector3.back is (0,0,-1))
        Vector3 desiredOffsetFromPivot = desiredRotation * new Vector3(0, 0, -currentZoom);

        // 3. Determine the camera's desired position (orbiting around the smoothed target's pivot)
        Vector3 desiredPosition = smoothedTargetPos + desiredOffsetFromPivot;

        // 4. Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothFactor * Time.deltaTime);

        // 5. Always look at the target (or a point slightly above its pivot, using smoothedTargetPos)
        transform.LookAt(smoothedTargetPos + targetOcclusionCheckOffset);
    }

    void HandleWallOcclusion()
    {
        if (wallLayer == 0 || occlusionSphereCastRadius <= 0f) return;

        // Use smoothedTargetPos for consistency if you're smoothing the target
        Vector3 targetCheckPosition = smoothedTargetPos + targetOcclusionCheckOffset;
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
                    wallsHitThisFrame.Add(wall);
                }
            }
        }

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
                // wallToStopFading.DeactivateFadeZone(); // Example call
            }
        }
        currentlyAffectedWalls = wallsHitThisFrame;
    }

    // --- Added: Transition to view coroutine and helpers ---
    public IEnumerator TransitionToViewCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }
        _manualTargetPosition = targetPosition;
        _manualTargetRotation = targetRotation;
        _transitionCoroutine = StartCoroutine(DoTransition(targetPosition, targetRotation, duration));
        yield return _transitionCoroutine; // Allow the calling coroutine to wait for this one
    }

    private IEnumerator DoTransition(Vector3 endPosition, Quaternion endRotation, float duration)
    {
        // Ensure manual control is active during transition.
        // The calling script should set SetManualControl(true) *before* calling this.
        // isManuallyControlled = true; // This will be set by the ComputerInteractable

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (!isManuallyControlled) // If manual control was released externally
            {
                Debug.Log("[CameraFollow] Transition interrupted by manual control release.");
                _transitionCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            t = t * t * (3f - 2f * t); // SmoothStep easing

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        if (isManuallyControlled) // Only snap to final if still manually controlled
        {
            transform.position = endPosition;
            transform.rotation = endRotation;
        }
        _transitionCoroutine = null;
    }

    // --- Modified: SetManualControl to handle transition coroutine ---
    public void SetManualControl(bool manual)
    {
        // if (isManuallyControlled == manual && _transitionCoroutine == null) return; // No change

        isManuallyControlled = manual;
        if (!manual && _transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
            Debug.Log("[CameraFollow] Manual control released, transition stopped. Camera will resume normal follow.");
        }
        else if (manual && _transitionCoroutine != null)
        {
            // If we are setting manual control TRUE and a transition is ALREADY running for manual control,
            // we might want to stop the old one and let a new one (if any) take over.
            // For now, this scenario implies the new TransitionToViewCoroutine will handle stopping the old one.
        }
        Debug.Log($"[CameraFollow] Manual Control set to: {manual}");
    }
}