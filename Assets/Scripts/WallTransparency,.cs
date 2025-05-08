// --- START OF FILE WallTransparency.cs ---

using UnityEngine;
using System.Collections;
using UnityEngine.Rendering; // Required for BlendMode enum

public class WallTransparency : MonoBehaviour
{
    [Tooltip("How long the fade in/out animation should take.")]
    public float fadeDuration = 0.0f;

    [Tooltip("The target alpha value when the wall is faded out (e.g., 0.3 for 30% visible).")]
    public float targetAlpha = 0.0f;

    [Header("URP Shader Property Names (Default for Lit/Unlit)")]
    [Tooltip("Shader property for main color and alpha.")]
    public string colorPropertyName = "_BaseColor";
    [Tooltip("Shader property to switch between Opaque (0) and Transparent (1).")]
    public string surfaceTypePropertyName = "_Surface";
    [Tooltip("Shader property for blend mode when transparent (0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply).")]
    public string blendModePropertyName = "_Blend";
    // These are less likely to change for standard alpha blend but can be exposed if needed
    // public string srcBlendPropertyName = "_SrcBlend";
    // public string dstBlendPropertyName = "_DstBlend";
    // public string zWritePropertyName = "_ZWrite";


    private Renderer wallRenderer;
    private Material materialInstance;
    private Color baseMaterialRGB;
    private float originalMaterialAlpha; // Alpha when fully "visible" (could be 1.0)

    private Coroutine fadeCoroutine;
    private bool isCurrentlyOpaque = true;

    // Shader Property IDs for efficiency
    private int _propIdColor;
    private int _propIdSurfaceType;
    private int _propIdBlendMode;
    private int _propIdSrcBlend;
    private int _propIdDstBlend;
    private int _propIdZWrite;


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
            Debug.LogError($"WallTransparency on '{gameObject.name}' requires a Material to be assigned.", this);
            enabled = false;
            return;
        }

        // Create a unique instance of the material
        materialInstance = new Material(wallRenderer.sharedMaterial);
        wallRenderer.material = materialInstance;

        // Cache shader property IDs
        _propIdColor = Shader.PropertyToID(colorPropertyName);
        _propIdSurfaceType = Shader.PropertyToID(surfaceTypePropertyName);
        _propIdBlendMode = Shader.PropertyToID(blendModePropertyName);
        _propIdSrcBlend = Shader.PropertyToID("_SrcBlend"); // Standard URP property
        _propIdDstBlend = Shader.PropertyToID("_DstBlend"); // Standard URP property
        _propIdZWrite = Shader.PropertyToID("_ZWrite");     // Standard URP property

        if (materialInstance.HasProperty(_propIdColor))
        {
            Color initialColor = materialInstance.GetColor(_propIdColor);
            baseMaterialRGB = new Color(initialColor.r, initialColor.g, initialColor.b, 1f);
            originalMaterialAlpha = initialColor.a; // This should be 1.0 if starting fully opaque
        }
        else
        {
            Debug.LogError($"Material on '{gameObject.name}' (Shader: {materialInstance.shader.name}) " +
                           $"does not have the shader property '{colorPropertyName}'. Fading will not work.", this);
            enabled = false;
            return;
        }

        // Ensure it starts in Opaque mode
        SetMaterialToOpaque();
    }

    private void SetMaterialToOpaque()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType))
        {
            Debug.LogWarning($"Material on {gameObject.name} does not have URP SurfaceType property ('{surfaceTypePropertyName}'). Cannot switch to Opaque.", this);
            return;
        }
        materialInstance.SetFloat(_propIdSurfaceType, 0f); // 0 for Opaque
        materialInstance.SetInt(_propIdSrcBlend, (int)BlendMode.One);
        materialInstance.SetInt(_propIdDstBlend, (int)BlendMode.Zero);
        materialInstance.SetInt(_propIdZWrite, 1); // Enable ZWrite
        // For URP, render queue is often managed by the SurfaceType. If explicit control is needed:
        // materialInstance.renderQueue = (int)RenderQueue.Geometry;

        // Set alpha to original (fully visible)
        SetMaterialAlpha(originalMaterialAlpha);
        isCurrentlyOpaque = true;
        // Debug.Log($"{gameObject.name} set to Opaque mode.");
    }

    private void SetMaterialToTransparentFade()
    {
        if (!materialInstance.HasProperty(_propIdSurfaceType) || !materialInstance.HasProperty(_propIdBlendMode))
        {
            Debug.LogWarning($"Material on {gameObject.name} does not have URP SurfaceType ('{surfaceTypePropertyName}') or BlendMode ('{blendModePropertyName}') property. Cannot switch to Transparent.", this);
            return;
        }
        materialInstance.SetFloat(_propIdSurfaceType, 1f); // 1 for Transparent
        materialInstance.SetFloat(_propIdBlendMode, 0f);   // 0 for Alpha Blend
        materialInstance.SetInt(_propIdSrcBlend, (int)BlendMode.SrcAlpha);
        materialInstance.SetInt(_propIdDstBlend, (int)BlendMode.OneMinusSrcAlpha);
        materialInstance.SetInt(_propIdZWrite, 0); // Disable ZWrite for standard alpha blend
        // For URP, render queue is often managed by the SurfaceType. If explicit control is needed:
        // materialInstance.renderQueue = (int)RenderQueue.Transparent;

        // Alpha will be set by the fade coroutine, ensure it starts from original alpha
        SetMaterialAlpha(originalMaterialAlpha);
        isCurrentlyOpaque = false;
        // Debug.Log($"{gameObject.name} set to Transparent mode for fading.");
    }


    public void FadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // If it's currently opaque, switch to transparent mode first
        if (isCurrentlyOpaque)
        {
            SetMaterialToTransparentFade();
        }
        // else it's already transparent (e.g. mid-fade-in, or was already faded out)

        fadeCoroutine = StartCoroutine(FadeAlphaTo(targetAlpha));
    }

    public void FadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // If it was opaque (shouldn't happen if FadeIn is called correctly after a FadeOut),
        // still switch to transparent to animate alpha, then switch back to opaque on completion.
        if (isCurrentlyOpaque)
        {
             SetMaterialToTransparentFade(); // Prepare for alpha animation
        }

        fadeCoroutine = StartCoroutine(FadeAlphaTo(originalMaterialAlpha, () => {
            SetMaterialToOpaque(); // Switch back to Opaque mode once fully visible
        }));
    }

    private IEnumerator FadeAlphaTo(float newTargetAlpha, System.Action onComplete = null)
    {
        if (materialInstance == null || !materialInstance.HasProperty(_propIdColor))
        {
            Debug.LogWarning($"Cannot fade '{gameObject.name}', material instance or color property '{colorPropertyName}' is missing.", this);
            yield break;
        }

        float currentAlpha = materialInstance.GetColor(_propIdColor).a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(time / fadeDuration);
            float interpolatedAlpha = Mathf.Lerp(currentAlpha, newTargetAlpha, normalizedTime);
            SetMaterialAlpha(interpolatedAlpha);
            yield return null;
        }

        SetMaterialAlpha(newTargetAlpha);
        fadeCoroutine = null;
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