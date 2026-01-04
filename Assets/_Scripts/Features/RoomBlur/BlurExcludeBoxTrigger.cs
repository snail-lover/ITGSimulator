using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class BlurExcludeBoxTrigger : MonoBehaviour
{
    public string playerTag = "Player";
    public Transform player;                 // optional explicit target
    public bool addKinematicRigidbody = true;

    [Header("Blur Activation")]
    [Range(0, 1)] public float inactiveAmount = 0f;  // "off" baseline
    [Range(0, 1)] public float activeAmount = 1f;    // when fully inside any box
    public float activationBeltNormalized = 0.1f;    // Normalized transition zone width at the box edge

    [Header("Fading")]
    public float enterFadeSeconds = 0.5f;            // Time to fade in
    public float exitFadeSeconds = 0.5f;             // Time to fade out
    public bool useUnscaledTime = true;              // Use unscaled delta time for fading

    // --- Shader Property IDs ---
    const int MAX_BOXES = 8;
    static readonly int ID_BoxCount = Shader.PropertyToID("_BoxCount");
    static readonly int ID_WorldToBoxArr = Shader.PropertyToID("_WorldToBoxArr");
    static readonly int ID_BlurAmount = Shader.PropertyToID("_BlurAmount");
    static readonly int ID_BoxParamsArr = Shader.PropertyToID("_BoxParamsArr");
    static readonly int ID_PlayerWorldPos = Shader.PropertyToID("_PlayerWorldPos");
    static readonly int ID_ActivationBelt = Shader.PropertyToID("_ActivationBelt");

    // --- Static fields for managing all instances ---
    static readonly List<BlurExcludeBoxTrigger> s_All = new();
    static readonly List<(BlurExcludeBoxTrigger box, float weight, float dist)> s_Temp = new();
    static float s_CurrentAmount; // shared global blur amount
    static float s_Velocity;      // for SmoothDamp

    // --- Instance fields ---
    BoxCollider box;
    Matrix4x4 worldToBox;
    bool visited; // Flag to track if the player has entered this box before

    void OnEnable()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true; // Still useful for other physics interactions

        if (addKinematicRigidbody)
        {
            var rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
        }

        s_All.Add(this);
        UpdateMatrix();
        PushGlobals(true);
    }

    void OnDisable()
    {
        s_All.Remove(this);
        PushGlobals(true);
    }

    void LateUpdate()
    {
        UpdateMatrix();
        PushGlobals(false);
    }

    void UpdateMatrix()
    {
        var t = box.transform;
        Vector3 worldCenter = t.TransformPoint(box.center);
        Vector3 worldSize = Vector3.Scale(t.lossyScale, box.size);
        var trs = Matrix4x4.TRS(worldCenter, t.rotation, worldSize);
        worldToBox = trs.inverse;
    }

    // The old OnTriggerEnter/Exit and related methods are no longer needed
    // as the new PushGlobals logic handles player position detection manually.

    static void PushGlobals(bool immediate)
    {
        if (s_All.Count == 0)
        {
            Shader.SetGlobalInt(ID_BoxCount, 0);
            Shader.SetGlobalFloat(ID_BlurAmount, 0f);
            return;
        }

        // Resolve player transform
        Transform player = null;
        foreach (var b in s_All) { if (b && b.player) { player = b.player; break; } }
        if (!player)
        {
            string tag = s_All[0].playerTag;
            var go = GameObject.FindWithTag(tag);
            if (go) player = go.transform;
        }
        if (!player)
        {
            Shader.SetGlobalInt(ID_BoxCount, 0);
            Shader.SetGlobalFloat(ID_BlurAmount, 0f);
            return;
        }

        Vector3 playerPos = player.position;

        // Build weighted list (activation) and mark visited
        s_Temp.Clear();
        float maxW = 0f;

        foreach (var b in s_All)
        {
            if (!b || !b.isActiveAndEnabled) continue;

            Vector3 p = b.worldToBox.MultiplyPoint(playerPos);
            float s = Mathf.Min(Mathf.Min(0.5f - Mathf.Abs(p.x), 0.5f - Mathf.Abs(p.y)), 0.5f - Mathf.Abs(p.z));
            float w = Mathf.InverseLerp(-b.activationBeltNormalized, b.activationBeltNormalized, s);
            w = Mathf.Clamp01(w);

            // Mark as visited once player is actually inside
            if (s > 0f) b.visited = true;

            float distOutside = Mathf.Max(-s, 0f);
            if (w > maxW) maxW = w;
            s_Temp.Add((b, w, distOutside));
        }

        // Sort by weight then distance; choose up to MAX_BOXES
        s_Temp.Sort((a, b) =>
        {
            int cw = b.weight.CompareTo(a.weight);
            return (cw != 0) ? cw : a.dist.CompareTo(b.dist);
        });

        int count = Mathf.Min(MAX_BOXES, s_Temp.Count);

        var mats = new Matrix4x4[MAX_BOXES];
        var pars = new Vector4[MAX_BOXES];

        for (int i = 0; i < count; i++)
        {
            var bx = s_Temp[i].box;
            mats[i] = bx.worldToBox;
            pars[i] = new Vector4(bx.visited ? 1f : 0f, 0f, 0f, 0f); // x = visited flag
        }
        for (int i = count; i < MAX_BOXES; i++)
        {
            mats[i] = Matrix4x4.identity;
            pars[i] = Vector4.zero;
        }

        Shader.SetGlobalInt(ID_BoxCount, count);
        Shader.SetGlobalMatrixArray(ID_WorldToBoxArr, mats);
        Shader.SetGlobalVectorArray(ID_BoxParamsArr, pars);
        Shader.SetGlobalVector(ID_PlayerWorldPos, playerPos);
        Shader.SetGlobalFloat(ID_ActivationBelt, s_All[0].activationBeltNormalized);

        // Smooth global intensity toward lerp(inactive, active, maxW)
        float inactive = s_All[0].inactiveAmount;
        float active = s_All[0].activeAmount;
        float target = Mathf.Lerp(inactive, active, maxW);

        if (immediate || !Application.isPlaying || (s_All[0].enterFadeSeconds == 0f && s_All[0].exitFadeSeconds == 0f))
        {
            s_CurrentAmount = target;
            s_Velocity = 0f;
        }
        else
        {
            float dt = s_All[0].useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float smoothTime = (target > s_CurrentAmount) ? Mathf.Max(0.0001f, s_All[0].enterFadeSeconds)
                                                          : Mathf.Max(0.0001f, s_All[0].exitFadeSeconds);
            s_CurrentAmount = Mathf.SmoothDamp(s_CurrentAmount, target, ref s_Velocity, smoothTime, Mathf.Infinity, dt);
        }
        Shader.SetGlobalFloat(ID_BlurAmount, s_CurrentAmount);
    }
}