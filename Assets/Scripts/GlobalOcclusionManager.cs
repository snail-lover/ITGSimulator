using UnityEngine;
using System.Linq; // Required for OrderBy

public class GlobalOcclusionManager : MonoBehaviour
{
    public static GlobalOcclusionManager Instance { get; private set; }

    public Vector3 SmoothedGlobalOcclusionPoint { get; private set; }
    private Vector3 _targetGlobalOcclusionPoint;

    public bool IsGlobalOcclusionActive { get; private set; }
    public float GlobalOcclusionRadius { get; private set; }
    public float GlobalOcclusionSoftness { get; private set; }

    [Header("Target & Camera")]
    public Transform playerTarget;
    public Camera gameCamera;

    [Header("Occlusion Check Settings")]
    public LayerMask wallLayer;
    [Tooltip("How far 'behind' the player (relative to camera) the check point is. Also includes vertical offset.")]
    public float dynamicOffsetDistance = 1.0f; // How far back from player center
    [Tooltip("Vertical offset from player's pivot for the check point.")]
    public float verticalOffset = 1.0f;      // The 'up' component

    public float occlusionSphereCastRadius = 0.5f; // Radius of the cast itself
    public float defaultHoleRadius = 2.0f;      // << INCREASED DEFAULT for better coverage
    public float defaultHoleSoftness = 0.5f;

    [Header("Smoothing Settings")]
    public float occlusionPointSmoothSpeed = 15f;

    [Header("Debugging")]
    public bool enableDebugDrawing = true;
    public Color debugRayColorHit = Color.green;
    public Color debugRayColorMiss = Color.red;
    public float debugRayDuration = 0.0f;
    public Color debugSphereColor = new Color(0, 1, 0, 0.25f);

    private RaycastHit[] _allHitsCache = new RaycastHit[10]; // Cache to avoid GC alloc
    private int _numHits = 0;
    private Vector3 _currentDynamicTargetCheckPosition; // For debugging

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            if (playerTarget != null && gameCamera != null)
            {
                // Initial calculation for target point
                Vector3 directionFromCameraToPlayer = (playerTarget.position - gameCamera.transform.position).normalized;
                directionFromCameraToPlayer.y = 0; // Keep it horizontal for the "behind" calculation
                _targetGlobalOcclusionPoint = playerTarget.position + Vector3.up * verticalOffset - directionFromCameraToPlayer * dynamicOffsetDistance;
                SmoothedGlobalOcclusionPoint = _targetGlobalOcclusionPoint;
            }
            else
            {
                _targetGlobalOcclusionPoint = Vector3.one * 10000f; // Far away
                SmoothedGlobalOcclusionPoint = _targetGlobalOcclusionPoint;
            }
        }
    }

    void LateUpdate()
    {
        if (playerTarget == null || gameCamera == null)
        {
            IsGlobalOcclusionActive = false;
            if (playerTarget != null && gameCamera != null)
            {
                Vector3 directionFromCameraToPlayer = (playerTarget.position - gameCamera.transform.position).normalized;
                directionFromCameraToPlayer.y = 0;
                _targetGlobalOcclusionPoint = playerTarget.position + Vector3.up * verticalOffset - directionFromCameraToPlayer * dynamicOffsetDistance;
            }
            if (enableDebugDrawing) DrawDebugInfo(null, false);
            UpdateSmoothedPoint(); // Still update smoothed point even if inactive
            return;
        }

        // --- Calculate Dynamic Target Check Position ---
        Vector3 playerCenter = playerTarget.position;
        Vector3 cameraPos = gameCamera.transform.position;

        // Direction from camera TO player, projected onto XZ plane (horizontal)
        Vector3 dirCamToPlayerHorizontal = playerCenter - cameraPos;
        dirCamToPlayerHorizontal.y = 0;
        dirCamToPlayerHorizontal.Normalize();

        // The point "behind" the player is in the opposite direction of dirCamToPlayerHorizontal
        Vector3 behindPlayerOffset = -dirCamToPlayerHorizontal * dynamicOffsetDistance;

        // Final target check position
        _currentDynamicTargetCheckPosition = playerCenter + Vector3.up * verticalOffset + behindPlayerOffset;
        // --- End Calculate Dynamic Target Check Position ---

        Vector3 castOrigin = cameraPos;
        Vector3 directionToDynamicTarget = _currentDynamicTargetCheckPosition - castOrigin;
        float distanceToDynamicTarget = directionToDynamicTarget.magnitude;

        _numHits = Physics.SphereCastNonAlloc(
            castOrigin,
            occlusionSphereCastRadius,
            directionToDynamicTarget.normalized,
            _allHitsCache,
            distanceToDynamicTarget,
            wallLayer
        );

        if (_numHits > 0)
        {
            // Find the closest hit
            RaycastHit closestHit = _allHitsCache[0];
            for (int i = 1; i < _numHits; i++)
            {
                if (_allHitsCache[i].distance < closestHit.distance)
                {
                    closestHit = _allHitsCache[i];
                }
            }
            // Alternative using Linq if you prefer:
            // RaycastHit closestHit = _allHitsCache.Take(_numHits).OrderBy(h => h.distance).First();

            IsGlobalOcclusionActive = true;
            _targetGlobalOcclusionPoint = closestHit.point;
            GlobalOcclusionRadius = defaultHoleRadius;
            GlobalOcclusionSoftness = defaultHoleSoftness;

            if (enableDebugDrawing) DrawDebugInfo(closestHit, true);
        }
        else
        {
            IsGlobalOcclusionActive = false;
            _targetGlobalOcclusionPoint = _currentDynamicTargetCheckPosition;
            if (enableDebugDrawing) DrawDebugInfo(null, false);
        }

        UpdateSmoothedPoint();
    }

    void UpdateSmoothedPoint()
    {
        SmoothedGlobalOcclusionPoint = Vector3.Lerp(
            SmoothedGlobalOcclusionPoint,
            _targetGlobalOcclusionPoint,
            Time.deltaTime * occlusionPointSmoothSpeed
        );

        if (enableDebugDrawing && playerTarget != null)
        {
            DrawWireSphere(SmoothedGlobalOcclusionPoint, 0.15f, Color.cyan, 12, debugRayDuration);
            if (IsGlobalOcclusionActive)
            {
                DrawWireSphere(SmoothedGlobalOcclusionPoint, GlobalOcclusionRadius, new Color(0.5f, 0.5f, 1f, 0.1f), 24, debugRayDuration);
            }
        }
    }

    void DrawDebugInfo(RaycastHit? hitInfo, bool didHit)
    {
        if (playerTarget == null || gameCamera == null) return;

        // Use _currentDynamicTargetCheckPosition for drawing the target of the ray
        Vector3 targetCheckPositionForDebug = _currentDynamicTargetCheckPosition;
        Vector3 cameraPosition = gameCamera.transform.position;

        if (didHit && hitInfo.HasValue)
        {
            RaycastHit hit = hitInfo.Value;
            Debug.DrawLine(cameraPosition, hit.point, debugRayColorHit, debugRayDuration);
            Debug.DrawLine(hit.point, targetCheckPositionForDebug, Color.Lerp(debugRayColorHit, Color.gray, 0.5f), debugRayDuration);
            DrawWireSphere(hit.point, 0.1f, Color.white, 16, debugRayDuration);
            DrawWireSphere(hit.point, occlusionSphereCastRadius, debugSphereColor, 16, debugRayDuration);

            // Debug all hits from SphereCastNonAlloc
            for (int i = 0; i < _numHits; i++)
            {
                DrawWireSphere(_allHitsCache[i].point, 0.05f, Color.magenta, 8, debugRayDuration);
            }
        }
        else
        {
            Debug.DrawLine(cameraPosition, targetCheckPositionForDebug, debugRayColorMiss, debugRayDuration);
        }
        // Also draw the dynamic target check position itself
        DrawWireSphere(targetCheckPositionForDebug, 0.1f, Color.yellow, 12, debugRayDuration);
    }

    // Helper to draw a wire sphere (unchanged)
    public static void DrawWireSphere(Vector3 center, float radius, Color color, int segments = 16, float duration = 0.0f)
    {
        if (radius <= 0 || segments <= 0) return;

        Vector3_[] points = new Vector3_[segments + 1];
        float angleStep = 360.0f / segments;

        // XY Plane (around Z axis)
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            points[i] = new Vector3_(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, center.z);
        }
        for (int i = 0; i < segments; i++) Debug.DrawLine(points[i], points[i + 1], color, duration);

        // XZ Plane (around Y axis)
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            points[i] = new Vector3_(center.x + Mathf.Cos(angle) * radius, center.y, center.z + Mathf.Sin(angle) * radius);
        }
        for (int i = 0; i < segments; i++) Debug.DrawLine(points[i], points[i + 1], color, duration);

        // YZ Plane (around X axis)
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            points[i] = new Vector3_(center.x, center.y + Mathf.Cos(angle) * radius, center.z + Mathf.Sin(angle) * radius);
        }
        for (int i = 0; i < segments; i++) Debug.DrawLine(points[i], points[i + 1], color, duration);
    }
}

// Minimal Vector3 struct to avoid ambiguity with UnityEngine.Vector3 in the helper
struct Vector3_
{
    public float x, y, z;
    public Vector3_(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    public static implicit operator UnityEngine.Vector3(Vector3_ v) => new UnityEngine.Vector3(v.x, v.y, v.z);
    public static implicit operator Vector3_(UnityEngine.Vector3 v) => new Vector3_(v.x, v.y, v.z);
}