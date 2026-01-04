using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class WallTransparency : MonoBehaviour
{
    [Tooltip("How long the fade in/out animation should take for the overall wall material.")]
    public float fadeDuration = 0.0f;

    [Tooltip("The target alpha for the wall material when faded. The shader hole handles the inner circle.")]
    public float targetAlpha = 1.0f;

    [Header("Partial Fade (local fallback)")]
    public float localHoleRadius = 1.5f;
    [Range(0.01f, 1.0f)] public float partialFadeSoftness = 0.5f;
    public float holeOpenCloseDuration = 0.2f;

    [Header("URP Shader Property Names")]
    public string colorPropertyName = "_BaseColor";
    public string surfaceTypePropertyName = "_Surface";
    public string blendModePropertyName = "_Blend";

    private Renderer wallRenderer;
    private Material materialInstance;
    private Color baseRGB;
    private float originalAlpha;

    private Coroutine transitionCoroutine;
    private Coroutine holeCoroutine;
    private bool isFaded;

    // Shader property IDs (must exist in your shader)
    private int _pidColor, _pidSurface, _pidBlend, _pidSrcBlend, _pidDstBlend, _pidZWrite;
    private static readonly int PID_OcclPoint = Shader.PropertyToID("_OcclusionPoint");
    private static readonly int PID_OcclRadius = Shader.PropertyToID("_OcclusionRadius");
    private static readonly int PID_OcclSoftness = Shader.PropertyToID("_OcclusionSoftness");
    private static readonly int PID_OcclActive = Shader.PropertyToID("_OcclusionActive");

    void Awake()
    {
        wallRenderer = GetComponent<Renderer>();
        if (!wallRenderer || !wallRenderer.sharedMaterial) { enabled = false; return; }

        materialInstance = new Material(wallRenderer.sharedMaterial);
        wallRenderer.material = materialInstance;

        _pidColor = Shader.PropertyToID(colorPropertyName);
        _pidSurface = Shader.PropertyToID(surfaceTypePropertyName);
        _pidBlend = Shader.PropertyToID(blendModePropertyName);
        _pidSrcBlend = Shader.PropertyToID("_SrcBlend");
        _pidDstBlend = Shader.PropertyToID("_DstBlend");
        _pidZWrite = Shader.PropertyToID("_ZWrite");

        if (!materialInstance.HasProperty(_pidColor)) { enabled = false; return; }

        Color start = materialInstance.GetColor(_pidColor);
        baseRGB = new Color(start.r, start.g, start.b, 1f);
        originalAlpha = start.a;

        if (!materialInstance.HasProperty(PID_OcclPoint) ||
            !materialInstance.HasProperty(PID_OcclRadius) ||
            !materialInstance.HasProperty(PID_OcclSoftness) ||
            !materialInstance.HasProperty(PID_OcclActive))
        {
            Debug.LogError($"Material on '{name}' is missing occlusion shader properties.");
        }

        SetOpaque();
        materialInstance.SetFloat(PID_OcclActive, 0f);
    }

    void Update()
    {
        var gom = GlobalOcclusionManager.Instance;

        // If manager says there is an occluder, drive hole from it.
        if (gom != null && gom.IsGlobalOcclusionActive && gom.GlobalOcclusionRadius > 0f)
        {
            Activate(gom.SmoothedGlobalOcclusionPoint, gom.GlobalOcclusionRadius, gom.GlobalOcclusionSoftness);
        }
        else
        {
            // Otherwise close the hole and restore
            Deactivate();
        }
    }

    // ---- Material state helpers ----
    void SetOpaque()
    {
        if (!materialInstance.HasProperty(_pidSurface)) return;
        materialInstance.SetFloat(_pidSurface, 0f);
        materialInstance.SetInt(_pidSrcBlend, (int)BlendMode.One);
        materialInstance.SetInt(_pidDstBlend, (int)BlendMode.Zero);
        materialInstance.SetInt(_pidZWrite, 1);
        SetAlpha(originalAlpha);
    }

    void SetTransparentFade()
    {
        if (!materialInstance.HasProperty(_pidSurface) || !materialInstance.HasProperty(_pidBlend)) return;
        materialInstance.SetFloat(_pidSurface, 1f);
        materialInstance.SetFloat(_pidBlend, 1f);
        materialInstance.SetInt(_pidSrcBlend, (int)BlendMode.One);
        materialInstance.SetInt(_pidDstBlend, (int)BlendMode.OneMinusSrcAlpha);
        materialInstance.SetInt(_pidZWrite, 0);
    }

    void SetAlpha(float a)
    {
        if (!materialInstance) return;
        var c = baseRGB; c.a = Mathf.Clamp01(a);
        materialInstance.SetColor(_pidColor, c);
    }

    // ---- Activation / deactivation ----
    void Activate(Vector3 point, float radius, float softness)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (holeCoroutine != null) StopCoroutine(holeCoroutine);

        if (!isFaded || materialInstance.GetFloat(_pidSurface) == 0f)
            SetTransparentFade();

        transitionCoroutine = StartCoroutine(AnimateAlphaTo(targetAlpha));
        holeCoroutine = StartCoroutine(AnimateHoleTo(radius));

        isFaded = true;
        materialInstance.SetVector(PID_OcclPoint, point);
        materialInstance.SetFloat(PID_OcclSoftness, softness);
        materialInstance.SetFloat(PID_OcclActive, 1f);
    }

    void Deactivate()
    {
        if (!isFaded) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        if (holeCoroutine != null) StopCoroutine(holeCoroutine);

        transitionCoroutine = StartCoroutine(AnimateAlphaTo(originalAlpha, TryFinalizeDeactivation));
        holeCoroutine = StartCoroutine(AnimateHoleTo(0f, TryFinalizeDeactivation));
    }

    float _currentHole;
    IEnumerator AnimateHoleTo(float target, System.Action onComplete = null)
    {
        float start = _currentHole;
        float t = 0f;
        if (holeOpenCloseDuration <= 0f)
        {
            materialInstance.SetFloat(PID_OcclRadius, target);
            _currentHole = target;
            onComplete?.Invoke();
            yield break;
        }
        while (t < holeOpenCloseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / holeOpenCloseDuration);
            float v = Mathf.Lerp(start, target, k);
            materialInstance.SetFloat(PID_OcclRadius, v);
            _currentHole = v;
            yield return null;
        }
        materialInstance.SetFloat(PID_OcclRadius, target);
        _currentHole = target;
        onComplete?.Invoke();
    }

    IEnumerator AnimateAlphaTo(float target, System.Action onComplete = null)
    {
        float start = materialInstance.GetColor(_pidColor).a;
        float t = 0f;
        if (fadeDuration <= 0f)
        {
            SetAlpha(target);
            onComplete?.Invoke();
            yield break;
        }
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            SetAlpha(Mathf.Lerp(start, target, k));
            yield return null;
        }
        SetAlpha(target);
        onComplete?.Invoke();
    }

    void TryFinalizeDeactivation()
    {
        var gom = GlobalOcclusionManager.Instance;
        bool gomInactive = gom == null || !gom.IsGlobalOcclusionActive;

        if (gomInactive &&
            Mathf.Approximately(materialInstance.GetColor(_pidColor).a, originalAlpha) &&
            _currentHole <= 0.01f)
        {
            if (isFaded)
            {
                SetOpaque();
                materialInstance.SetFloat(PID_OcclActive, 0f);
                isFaded = false;
            }
        }
    }

    void OnDisable()
    {
        if (materialInstance != null)
        {
            SetAlpha(originalAlpha);
            SetOpaque();
            materialInstance.SetFloat(PID_OcclActive, 0f);
        }
        isFaded = false;
    }

    void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }
}
