using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the multi‑state blur shader.  The shader is now <b>active only while the player is
/// inside a zone or its activation belt</b>.  Outside all zones <c>_BlurAmount</c> is set to 0 so
/// the pass early‑outs (no blur / no tint).
/// </summary>
[ExecuteAlways]
public class VisitedBlurController : MonoBehaviour
{
    [Header("Blur intensity while active")] public float activeAmount = 1f;

    [Header("Overlay strengths")][Range(0, 1)] public float visitedOverlay = 0.25f;
    [Range(0, 1)] public float unvisitedOverlay = 0.60f;
    public Color overlayTint = new(0.90f, 0.80f, 1.00f, 1);

    [Header("Player")] public Transform player; // auto‑finds by tag if left null

    [Header("Hand‑over belt (normalised)")][Range(0, 0.25f)] public float activationBelt = 0.08f;

    // shader IDs
    private static readonly int ID_BoxCount = Shader.PropertyToID("_BoxCount");
    private static readonly int ID_WorldToBoxArr = Shader.PropertyToID("_WorldToBoxArr");
    private static readonly int ID_OverlayArr = Shader.PropertyToID("_OverlayArr");
    private static readonly int ID_BlurAmount = Shader.PropertyToID("_BlurAmount");
    private static readonly int ID_PlayerWorldPos = Shader.PropertyToID("_PlayerWorldPos");
    private static readonly int ID_ActivationBelt = Shader.PropertyToID("_ActivationBelt");
    private static readonly int ID_OverlayTint = Shader.PropertyToID("_OverlayTint");

    private const int MAX_BOXES = 32;
    private readonly Matrix4x4[] mArr = new Matrix4x4[MAX_BOXES];
    private readonly float[] oArr = new float[MAX_BOXES];

    private readonly List<VisitedBlurZone> zones = new();

    // ─────────────────────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        FindZones();

        Shader.SetGlobalFloat(ID_ActivationBelt, activationBelt);
        Shader.SetGlobalVector(ID_OverlayTint, overlayTint);
        Shader.SetGlobalFloat(ID_BlurAmount, 0f); // start disabled
    }

    private void OnValidate()
    {
        Shader.SetGlobalFloat(ID_ActivationBelt, activationBelt);
        Shader.SetGlobalVector(ID_OverlayTint, overlayTint);
    }

    private void FindZones()
    {
        zones.Clear();
        zones.AddRange(FindObjectsOfType<VisitedBlurZone>());
    }

    private void LateUpdate()
    {
        // Ensure player reference
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
        if (!player) return;

        // Re‑discover zones if any were destroyed/created at runtime
        if (zones.RemoveAll(z => z == null) > 0) FindZones();

        int count = 0;
        float maxW = 0f; // highest activation weight this frame

        foreach (var z in zones)
        {
            if (!z.enabled) continue;
            z.CacheMatrix();

            // build arrays (up to MAX_BOXES)
            if (count < MAX_BOXES)
            {
                mArr[count] = z.worldToBox;
                oArr[count] = z.Visited ? visitedOverlay : unvisitedOverlay;
                count++;
            }

            // activation weight for global on/off
            float w = z.ActivationWeight(player.position);
            if (w > maxW) maxW = w;
        }

        // Fill unused slots
        for (int i = count; i < MAX_BOXES; i++) { mArr[i] = Matrix4x4.identity; oArr[i] = 0f; }

        // Push data each frame
        Shader.SetGlobalInt(ID_BoxCount, count);
        Shader.SetGlobalMatrixArray(ID_WorldToBoxArr, mArr);
        Shader.SetGlobalFloatArray(ID_OverlayArr, oArr);
        Shader.SetGlobalVector(ID_PlayerWorldPos, player.position);

        float finalBlurAmount = maxW > 0.0001f ? activeAmount : 0f;
        Shader.SetGlobalFloat(ID_BlurAmount, finalBlurAmount);

        // --- ADD THIS DEBUG LOG ---
        if (finalBlurAmount > 0)
        {
            //Debug.Log($"Blur Active! Sending {count} boxes to the shader. First matrix: \n{mArr[0]}");
        }

        // Enable blur only when inside any zone (or belt)
        Shader.SetGlobalFloat(ID_BlurAmount, maxW > 0.0001f ? activeAmount : 0f);
    }
}
