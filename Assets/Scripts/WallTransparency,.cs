// --- START OF FILE WallTransparency.cs ---

using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // Required for BlendMode enum

public class WallTransparency : MonoBehaviour
{
    [Tooltip("How long the fade in/out animation should take for the overall wall material.")]
    public float fadeDuration = 0.25f;

    [Tooltip("The target alpha value for the wall material (around the hole) when faded (e.g., 0.3 for 30% visible). The hole itself will be more transparent via shader.")]
    public float targetAlpha = 0.3f;

    [Header("Partial Fade Settings")]
    [Tooltip("Radius of the transparent 'hole' in world units.")]
    public float partialFadeRadius = 1.5f;
    [Tooltip("Softness of the hole's edge. 0 = hard edge, 1 = very soft edge (fades out over the entire radius).")]
    [Range(0.01f, 1.0f)]
    public float partialFadeSoftness = 0.5f;
    public float holeOpenCloseDuration = 0.2f; // Duration for hole to grow/shrink
    private float currentShaderOcclusionRadius = 0f;
    private Coroutine holeAnimationCoroutine;

    [Header("URP Shader Property Names (Default for Lit/Unlit)")]
    public string colorPropertyName = "_BaseColor";
    public string surfaceTypePropertyName = "_Surface";
    public string blendModePropertyName = "_Blend";

    private Renderer wallRenderer;
    private Material materialInstance;
    private Color baseMaterialRGB;
    private float originalMaterialAlpha;

    private Coroutine transitionCoroutine;
    private bool isCurrentlyFadedOut = false; // Tracks if partial fade is active

    // Shader Property IDs
    private int _propIdColor;
    private int _propIdSurfaceType;
    private int _propIdBlendMode;
    private int _propIdSrcBlend;
    private int _propIdDstBlend;
    private int _propIdZWrite;

    // Shader Property IDs for Partial Fade
    private static readonly int PropIdOcclusionPoint = Shader.PropertyToID("_OcclusionPoint");
    private static readonly int PropIdOcclusionRadius = Shader.PropertyToID("_OcclusionRadius");
    private static readonly int PropIdOcclusionSoftness = Shader.PropertyToID("_OcclusionSoftness");
    private static readonly int PropIdOcclusionActive = Shader.PropertyToID("_OcclusionActive");

    void Awake()
    {
        wallRenderer = GetComponent<Renderer>();
        if (wallRenderer == null)
        {
            Debug.LogError("WallTransparency script needs a Renderer component.", this);
            enabled = false;
            return;
        }

        if (wallRenderer.sharedMaterial == null)
        {
            Debug.LogError($"WallTransparency on '{gameObject.name}' requires a Material to be assigned. Ensure it's a material using your custom partial fade shader.", this);
            enabled = false;
            return;
        }

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
        else
        {
            Debug.LogError($"Material on '{gameObject.name}' (Shader: {materialInstance.shader.name}) " +
                           $"does not have the color property '{colorPropertyName}'. Fading will not work.", this);
            enabled = false;
            return;
        }

        // Check if shader has the required partial fade properties
        if (!materialInstance.HasProperty(PropIdOcclusionPoint) ||
            !materialInstance.HasProperty(PropIdOcclusionRadius) ||
            !materialInstance.HasProperty(PropIdOcclusionSoftness) ||
            !materialInstance.HasProperty(PropIdOcclusionActive))
        {
            Debug.LogError($"Material on '{gameObject.name}' (Shader: {materialInstance.shader.name}) " +
                           $"is missing one or more required partial fade shader properties (_OcclusionPoint, _OcclusionRadius, _OcclusionSoftness, _OcclusionActive). " +
                           "Ensure you're using a compatible custom shader.", this);
            // It might still work for basic alpha, but partial fade won't. Consider `enabled = false;`
        }

        SetMaterialToOpaque();
        materialInstance.SetFloat(PropIdOcclusionActive, 0f); // Ensure partial fade is off initially
    }

    private void SetMaterialToOpaque()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType)) return;
        materialInstance.SetFloat(_propIdSurfaceType, 0f); // 0 for Opaque
        materialInstance.SetInt(_propIdSrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
        materialInstance.SetInt(_propIdDstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
        materialInstance.SetInt(_propIdZWrite, 1);
        SetMaterialAlpha(originalMaterialAlpha);
    }

    private void SetMaterialToTransparentFade()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType) || !materialInstance.HasProperty(_propIdBlendMode)) return;
        materialInstance.SetFloat(_propIdSurfaceType, 1f); // Transparent
        materialInstance.SetFloat(_propIdBlendMode, 1f);   // <<<<< 1 for Premultiply
        materialInstance.SetInt(_propIdSrcBlend, (int)UnityEngine.Rendering.BlendMode.One); // <<<<< Source Blend for Premultiply
        materialInstance.SetInt(_propIdDstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // Standard
        materialInstance.SetInt(_propIdZWrite, 0); // Still disable ZWrite for transparency
    }

    public void ActivatePartialFade(Vector3 occlusionWorldPoint)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (holeAnimationCoroutine != null) StopCoroutine(holeAnimationCoroutine);

        if (!isCurrentlyFadedOut)
        {
            SetMaterialToTransparentFade();
            transitionCoroutine = StartCoroutine(AnimateMaterialAlphaTo(targetAlpha));
        }
        isCurrentlyFadedOut = true;

        materialInstance.SetVector(PropIdOcclusionPoint, occlusionWorldPoint);
        materialInstance.SetFloat(PropIdOcclusionSoftness, partialFadeSoftness);
        materialInstance.SetFloat(PropIdOcclusionActive, 1f);

        holeAnimationCoroutine = StartCoroutine(AnimateHoleRadiusTo(partialFadeRadius));
    }

    public void DeactivatePartialFade()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (holeAnimationCoroutine != null) StopCoroutine(holeAnimationCoroutine);

        holeAnimationCoroutine = StartCoroutine(AnimateHoleRadiusTo(0f, () => {
            // Optional: actions after hole is fully closed
        }));

        transitionCoroutine = StartCoroutine(AnimateMaterialAlphaTo(originalMaterialAlpha, () => {
            if (currentShaderOcclusionRadius <= 0.01f)
            {
                materialInstance.SetFloat(PropIdOcclusionActive, 0f);
                SetMaterialToOpaque();
                isCurrentlyFadedOut = false;
            }
        }));
    }   

    private IEnumerator AnimateHoleRadiusTo(float targetRadius, System.Action onComplete = null)
    {
        float startRadius = currentShaderOcclusionRadius;
        float time = 0f;

        if (holeOpenCloseDuration <= 0f)
        {
            materialInstance.SetFloat(PropIdOcclusionRadius, targetRadius);
            currentShaderOcclusionRadius = targetRadius;
            onComplete?.Invoke();
            yield break;
        }

        while (time < holeOpenCloseDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / holeOpenCloseDuration);
            float interpolatedRadius = Mathf.Lerp(startRadius, targetRadius, normalizedTime);

            materialInstance.SetFloat(PropIdOcclusionRadius, interpolatedRadius);
            currentShaderOcclusionRadius = interpolatedRadius;
            yield return null;
        }

        materialInstance.SetFloat(PropIdOcclusionRadius, targetRadius);
        currentShaderOcclusionRadius = targetRadius;
        holeAnimationCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateMaterialAlphaTo(float newTargetAlpha, System.Action onComplete = null)
    {
        if (!materialInstance.HasProperty(_propIdColor)) yield break;

        float currentAlpha = materialInstance.GetColor(_propIdColor).a;
        float time = 0f;

        if (fadeDuration <= 0f)
        {
            SetMaterialAlpha(newTargetAlpha);
            transitionCoroutine = null;
            onComplete?.Invoke();
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
        transitionCoroutine = null;
        onComplete?.Invoke();
    }

    private void SetMaterialAlpha(float alpha)
    {
        if (materialInstance != null && materialInstance.HasProperty(_propIdColor))
        {
            Color newColor = baseMaterialRGB;
            newColor.a = Mathf.Clamp01(alpha);
            materialInstance.SetColor(_propIdColor, newColor);
        }
    }

    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
// --- END OF FILE WallTransparency.cs ---