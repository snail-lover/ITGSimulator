using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; 

public class WallTransparency : MonoBehaviour
{
    
    [Tooltip("How long the fade in/out animation should take for the overall wall material.")]
    public float fadeDuration = 0.0f;

    [Tooltip("The target alpha value for the wall material (around the hole) when faded (e.g., 0.3 for 30% visible). The hole itself will be more transparent via shader.")]
    public float targetAlpha = 1.0f;

    [Header("Partial Fade Settings")]
    [Tooltip("Radius of the transparent 'hole' in world units. This is used if GOM is not active but this wall is.")]
    public float localHoleRadius = 1.5f; 
    [Tooltip("Softness of the hole's edge. 0 = hard edge, 1 = very soft edge (fades out over the entire radius).")]
    [Range(0.01f, 1.0f)]
    public float partialFadeSoftness = 0.5f; 
    public float holeOpenCloseDuration = 0.2f;
    private float currentShaderOcclusionRadius = 0f;
    private Coroutine holeAnimationCoroutine;

    [Header("URP Shader Property Names")]
    public string colorPropertyName = "_BaseColor";
    public string surfaceTypePropertyName = "_Surface";
    public string blendModePropertyName = "_Blend";


    private Renderer wallRenderer;
    private Material materialInstance;
    private Color baseMaterialRGB;
    private float originalMaterialAlpha;

    private Coroutine transitionCoroutine;
    private bool isCurrentlyFadedOut = false;

    // Shader Property IDs
    private int _propIdColor;
    private int _propIdSurfaceType;
    private int _propIdBlendMode;
    private int _propIdSrcBlend;
    private int _propIdDstBlend;
    private int _propIdZWrite;

    private static readonly int PropIdOcclusionPoint = Shader.PropertyToID("_OcclusionPoint");
    private static readonly int PropIdOcclusionRadius = Shader.PropertyToID("_OcclusionRadius");
    private static readonly int PropIdOcclusionSoftness = Shader.PropertyToID("_OcclusionSoftness");
    private static readonly int PropIdOcclusionActive = Shader.PropertyToID("_OcclusionActive");


    // New fields
    private Camera gameCamera;
    private Transform playerTarget;
    private Vector3 playerTargetOffset;
    private LayerMask selfLayerMask;

    void Awake()
    {
        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer == null) { enabled = false; return; }
        if (wallRenderer.sharedMaterial == null) { enabled = false; return; }

        materialInstance = new Material(wallRenderer.sharedMaterial);
        wallRenderer.material = materialInstance;

        _propIdColor = Shader.PropertyToID(colorPropertyName);
        _propIdSurfaceType = Shader.PropertyToID(surfaceTypePropertyName);
        _propIdBlendMode = Shader.PropertyToID(blendModePropertyName);
        _propIdSrcBlend = Shader.PropertyToID("_SrcBlend");
        _propIdDstBlend = Shader.PropertyToID("_DstBlend");
        _propIdZWrite = Shader.PropertyToID("_ZWrite");

        if (materialInstance.HasProperty(_propIdColor))
        {
            Color initialColor = materialInstance.GetColor(_propIdColor);
            baseMaterialRGB = new Color(initialColor.r, initialColor.g, initialColor.b, 1f);
            originalMaterialAlpha = initialColor.a;
        }
        else { enabled = false; return; }

        if (!materialInstance.HasProperty(PropIdOcclusionPoint) ||
            !materialInstance.HasProperty(PropIdOcclusionRadius) ||
            !materialInstance.HasProperty(PropIdOcclusionSoftness) ||
            !materialInstance.HasProperty(PropIdOcclusionActive))
        {
            Debug.LogError($"Material on '{gameObject.name}' is missing required partial fade shader properties.", this);
        }

        SetMaterialToOpaque();
        materialInstance.SetFloat(PropIdOcclusionActive, 0f);

    }

    void Start() 
    {
        if (GlobalOcclusionManager.Instance != null)
        {
            gameCamera = GlobalOcclusionManager.Instance.gameCamera;
            playerTarget = GlobalOcclusionManager.Instance.playerTarget;
            playerTargetOffset = Vector3.up * GlobalOcclusionManager.Instance.verticalOffset;
        }
        else
        {
            Debug.LogWarning("GlobalOcclusionManager.Instance not found by WallTransparency. Independent occlusion checks might fail.", this);
            // Optionally disable this script or parts of its functionality
            playerTargetOffset = Vector3.up * 1.0f;

        }
        selfLayerMask = 1 << gameObject.layer;
    }


    private void SetMaterialToOpaque()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType)) return;
        materialInstance.SetFloat(_propIdSurfaceType, 0f);
        materialInstance.SetInt(_propIdSrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
        materialInstance.SetInt(_propIdDstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
        materialInstance.SetInt(_propIdZWrite, 1);
        SetMaterialAlpha(originalMaterialAlpha);
    }

    private void SetMaterialToTransparentFade()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType) || !materialInstance.HasProperty(_propIdBlendMode)) return;
        materialInstance.SetFloat(_propIdSurfaceType, 1f);
        materialInstance.SetFloat(_propIdBlendMode, 1f);
        materialInstance.SetInt(_propIdSrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
        materialInstance.SetInt(_propIdDstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        materialInstance.SetInt(_propIdZWrite, 0);
    }

    void Update()
    {
        if (materialInstance == null) return; // Early exit if material is not set up

        bool gomExistsAndValid = GlobalOcclusionManager.Instance != null && gameCamera != null && playerTarget != null;
        bool isGloballyOccluding = gomExistsAndValid && GlobalOcclusionManager.Instance.IsGlobalOcclusionActive;

        Vector3 localHitPoint = Vector3.zero;
        bool thisWallIsIndependentlyOccluding = false;

        if (gomExistsAndValid) // Only perform independent check if GOM refs are valid
        {
            Vector3 targetCheckPosition = playerTarget.position + playerTargetOffset;
            Vector3 cameraPosition = gameCamera.transform.position;
            RaycastHit hitInfo;
            // Ensure Linecast doesn't hit starting inside a collider if camera is too close
            float linecastStartOffset = 0.01f;
            Vector3 linecastStart = cameraPosition + (targetCheckPosition - cameraPosition).normalized * linecastStartOffset;

            if (Physics.Linecast(linecastStart, targetCheckPosition, out hitInfo, selfLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.collider.gameObject == this.gameObject)
                {
                    thisWallIsIndependentlyOccluding = true;
                    localHitPoint = hitInfo.point; // Store the local hit point
                }
            }
        }


        if (isGloballyOccluding) // GOM is active, use its parameters
        {
            ActivateProcedure(
                GlobalOcclusionManager.Instance.SmoothedGlobalOcclusionPoint,
                GlobalOcclusionManager.Instance.GlobalOcclusionRadius,
                GlobalOcclusionManager.Instance.GlobalOcclusionSoftness
            );
        }
        else if (thisWallIsIndependentlyOccluding) // GOM is NOT active, but THIS wall is occluding
        {
            ActivateProcedure(
                localHitPoint,      // Use local hit point
                this.localHoleRadius, // Use this wall's configured radius for local occlusion
                this.partialFadeSoftness // Use this wall's softness
            );
        }
        else // Neither GOM is active, nor is this wall independently occluding
        {
            DeactivateProcedure();
        }
    }

    private void ActivateProcedure(Vector3 occlusionPoint, float occlusionRadius, float occlusionSoftness)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (holeAnimationCoroutine != null) StopCoroutine(holeAnimationCoroutine);

        if (isCurrentlyFadedOut == false || materialInstance.GetFloat(_propIdSurfaceType) == 0f)
        {
            SetMaterialToTransparentFade();
        }

        transitionCoroutine = StartCoroutine(AnimateMaterialAlphaTo(targetAlpha));
        // Use the passed-in radius for the animation target
        holeAnimationCoroutine = StartCoroutine(AnimateHoleRadiusTo(occlusionRadius));

        isCurrentlyFadedOut = true;
        materialInstance.SetVector(PropIdOcclusionPoint, occlusionPoint); // Use passed-in point
        // Radius is animated by AnimateHoleRadiusTo, which sets PropIdOcclusionRadius
        materialInstance.SetFloat(PropIdOcclusionSoftness, occlusionSoftness); // Use passed-in softness
        materialInstance.SetFloat(PropIdOcclusionActive, 1f);
    }

    private void DeactivateProcedure()
    {
        if (isCurrentlyFadedOut)
        {
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            if (holeAnimationCoroutine != null) StopCoroutine(holeAnimationCoroutine);

            bool gomStillInactive = GlobalOcclusionManager.Instance == null || !GlobalOcclusionManager.Instance.IsGlobalOcclusionActive;

            transitionCoroutine = StartCoroutine(AnimateMaterialAlphaTo(originalMaterialAlpha, () => {
                if (currentShaderOcclusionRadius <= 0.01f && (GlobalOcclusionManager.Instance == null || !GlobalOcclusionManager.Instance.IsGlobalOcclusionActive))
                {
                    FinalizeDeactivation();
                }
            }));

            holeAnimationCoroutine = StartCoroutine(AnimateHoleRadiusTo(0f, () => {
                Color currentColor = materialInstance.GetColor(_propIdColor);
                if (Mathf.Approximately(currentColor.a, originalMaterialAlpha) && (GlobalOcclusionManager.Instance == null || !GlobalOcclusionManager.Instance.IsGlobalOcclusionActive))
                {
                    FinalizeDeactivation();
                }
            }));
        }
    }

    private void FinalizeDeactivation()
    {
        bool canFinalize = GlobalOcclusionManager.Instance == null || !GlobalOcclusionManager.Instance.IsGlobalOcclusionActive;
        if (canFinalize &&
            materialInstance != null && // Add null check for materialInstance
            Mathf.Approximately(materialInstance.GetColor(_propIdColor).a, originalMaterialAlpha) &&
            currentShaderOcclusionRadius <= 0.01f)
        {
            if (isCurrentlyFadedOut)
            {
                SetMaterialToOpaque();
                materialInstance.SetFloat(PropIdOcclusionActive, 0f);
                isCurrentlyFadedOut = false;
                transitionCoroutine = null;
                holeAnimationCoroutine = null;
            }
        }
    }
    private IEnumerator AnimateHoleRadiusTo(float targetRadius, System.Action onComplete = null)
    {
        float startRadius = currentShaderOcclusionRadius;
        float time = 0f;
        Coroutine thisCoroutineInstance = holeAnimationCoroutine;

        if (holeOpenCloseDuration <= 0f)
        {
            if (materialInstance != null) materialInstance.SetFloat(PropIdOcclusionRadius, targetRadius);
            currentShaderOcclusionRadius = targetRadius;
            onComplete?.Invoke();
            if (holeAnimationCoroutine == thisCoroutineInstance) holeAnimationCoroutine = null;
            yield break;
        }

        while (time < holeOpenCloseDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / holeOpenCloseDuration);
            float interpolatedRadius = Mathf.Lerp(startRadius, targetRadius, normalizedTime);

            if (materialInstance != null) materialInstance.SetFloat(PropIdOcclusionRadius, interpolatedRadius);
            currentShaderOcclusionRadius = interpolatedRadius;
            yield return null;
        }

        if (materialInstance != null) materialInstance.SetFloat(PropIdOcclusionRadius, targetRadius);
        currentShaderOcclusionRadius = targetRadius;
        onComplete?.Invoke();
        if (holeAnimationCoroutine == thisCoroutineInstance) holeAnimationCoroutine = null;
    }

    private void SetMaterialAlpha(float alpha)
    {
        if (materialInstance != null && materialInstance.HasProperty(_propIdColor))
        {
            Color newColor = baseMaterialRGB; // Use the stored RGB
            newColor.a = Mathf.Clamp01(alpha); // Set the new alpha
            materialInstance.SetColor(_propIdColor, newColor);
        }
    }


    private IEnumerator AnimateMaterialAlphaTo(float newTargetAlpha, System.Action onComplete = null)
    {
        if (materialInstance == null || !materialInstance.HasProperty(_propIdColor)) yield break;

        float currentAlpha = materialInstance.GetColor(_propIdColor).a;
        float time = 0f;
        Coroutine thisCoroutineInstance = transitionCoroutine;

        if (fadeDuration <= 0f)
        {
            SetMaterialAlpha(newTargetAlpha);
            onComplete?.Invoke();
            if (transitionCoroutine == thisCoroutineInstance) transitionCoroutine = null;
            yield break;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / fadeDuration);
            float interpolatedAlpha = Mathf.Lerp(currentAlpha, newTargetAlpha, normalizedTime);
            SetMaterialAlpha(interpolatedAlpha);
            yield return null;
        }

        SetMaterialAlpha(newTargetAlpha);
        onComplete?.Invoke();
        if (transitionCoroutine == thisCoroutineInstance) transitionCoroutine = null;
    }
    // SetMaterialAlpha and OnDestroy same
}