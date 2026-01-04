using UnityEngine;

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
    [Tooltip("How far 'behind' the player (relative to camera) the check point is.")]
    public float dynamicOffsetDistance = 1.0f;
    [Tooltip("Vertical offset from player's pivot for the check point.")]
    public float verticalOffset = 1.0f;

    [Tooltip("Spherecast radius used to detect the first occluder.")]
    public float occlusionSphereCastRadius = 0.5f;

    [Header("Default Hole (sent to walls)")]
    public float defaultHoleRadius = 2.0f;
    [Range(0f, 1f)] public float defaultHoleSoftness = 0.5f;

    [Header("Smoothing")]
    public float occlusionPointSmoothSpeed = 15f;

    [Header("Debug")]
    public bool enableDebugDrawing = true;
    public Color debugRayColorHit = Color.green;
    public Color debugRayColorMiss = Color.red;
    public float debugRayDuration = 0f;
    public Color debugSphereColor = new Color(0, 1, 0, 0.25f);

    private readonly RaycastHit[] _hits = new RaycastHit[16];
    private Vector3 _dynamicTargetCheck; // for debug

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (playerTarget != null && gameCamera != null)
        {
            var camToPlayer = (playerTarget.position - gameCamera.transform.position);
            var flat = camToPlayer; flat.y = 0f; flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : gameCamera.transform.forward;
            _targetGlobalOcclusionPoint = playerTarget.position + Vector3.up * verticalOffset - flat * dynamicOffsetDistance;
            SmoothedGlobalOcclusionPoint = _targetGlobalOcclusionPoint;
        }
        else
        {
            _targetGlobalOcclusionPoint = Vector3.zero;
            SmoothedGlobalOcclusionPoint = _targetGlobalOcclusionPoint;
        }
    }

    void LateUpdate()
    {
        if (playerTarget == null || gameCamera == null)
        {
            IsGlobalOcclusionActive = false;
            UpdateSmoothedPoint();
            return;
        }

        // Calculate the “behind the player” target check position
        Vector3 playerCenter = playerTarget.position;
        Vector3 cameraPos = gameCamera.transform.position;

        Vector3 toPlayer = playerCenter - cameraPos;
        Vector3 flat = toPlayer; flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) flat = gameCamera.transform.forward;
        else flat.Normalize();

        Vector3 behindPlayerOffset = -flat * dynamicOffsetDistance;
        _dynamicTargetCheck = playerCenter + Vector3.up * verticalOffset + behindPlayerOffset;

        // Single, clean spherecast to the dynamic target. Ignore triggers.
        Vector3 dir = (_dynamicTargetCheck - cameraPos);
        float maxDist = dir.magnitude;
        IsGlobalOcclusionActive = false;

        if (maxDist > 0.001f && wallLayer.value != 0)
        {
            int count = Physics.SphereCastNonAlloc(
                cameraPos,
                occlusionSphereCastRadius,
                dir / maxDist,
                _hits,
                maxDist,
                wallLayer,
                QueryTriggerInteraction.Ignore
            );

            if (count > 0)
            {
                // Pick nearest valid hit
                int best = 0;
                for (int i = 1; i < count; i++)
                {
                    if (_hits[i].distance < _hits[best].distance) best = i;
                }

                var hit = _hits[best];
                _targetGlobalOcclusionPoint = hit.point;
                IsGlobalOcclusionActive = true;
                GlobalOcclusionRadius = defaultHoleRadius;
                GlobalOcclusionSoftness = defaultHoleSoftness;

                if (enableDebugDrawing)
                {
                    Debug.DrawLine(cameraPos, hit.point, debugRayColorHit, debugRayDuration);
                    Debug.DrawLine(hit.point, _dynamicTargetCheck, Color.Lerp(debugRayColorHit, Color.gray, 0.5f), debugRayDuration);
                    DrawWireSphere(hit.point, occlusionSphereCastRadius, debugSphereColor, 16, debugRayDuration);
                }
            }
            else
            {
                _targetGlobalOcclusionPoint = _dynamicTargetCheck;
                if (enableDebugDrawing)
                    Debug.DrawLine(cameraPos, _dynamicTargetCheck, debugRayColorMiss, debugRayDuration);
            }
        }
        else
        {
            _targetGlobalOcclusionPoint = _dynamicTargetCheck;
            if (enableDebugDrawing)
                Debug.DrawLine(cameraPos, _dynamicTargetCheck, debugRayColorMiss, debugRayDuration);
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

        if (enableDebugDrawing)
        {
            DrawWireSphere(_dynamicTargetCheck, 0.1f, Color.yellow, 12, debugRayDuration);
            if (IsGlobalOcclusionActive)
                DrawWireSphere(SmoothedGlobalOcclusionPoint, GlobalOcclusionRadius, new Color(0.5f, 0.5f, 1f, 0.1f), 24, debugRayDuration);
        }
    }

    public static void DrawWireSphere(Vector3 center, float radius, Color color, int segments = 16, float duration = 0f)
    {
        if (radius <= 0 || segments <= 0) return;

        // XY
        Vector3 prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
            Debug.DrawLine(prev, next, color, duration); prev = next;
        }
        // XZ
        prev = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
            Debug.DrawLine(prev, next, color, duration); prev = next;
        }
        // YZ
        prev = center + new Vector3(0, radius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * (360f / segments) * Mathf.Deg2Rad;
            Vector3 next = center + new Vector3(0, Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            Debug.DrawLine(prev, next, color, duration); prev = next;
        }
    }
}
