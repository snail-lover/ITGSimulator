using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 3‑state blur zone (current / visited / un‑visited) that persists its <see cref="Visited"/> flag
/// through the <c>WorldDataManager</c> save system.
/// </summary>
/// <remarks>
/// Assembly‑layering: this script lives in the <b>Features</b> assembly. It only touches the
/// <b>Gameplay</b> assembly (<c>WorldDataManager</c>) through its public API (events & flag access),
/// so there is no direct reference to <b>Core</b> where <c>GameSaveData</c> resides. Persistence is
/// handled via <c>WorldDataManager.SetGlobalFlag</c>/<c>GetGlobalFlag</c> which in turn serialises to
/// <c>GameSaveData.globalFlags</c>.
/// </remarks>
[RequireComponent(typeof(BoxCollider))]
[ExecuteAlways]
public class VisitedBlurZone : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Inspector fields

    [Tooltip("Unique key used in the save file. Leave empty to auto‑derive from the scene path.")]
    public string zoneKey = string.Empty;

    [Tooltip("If checked, the zone starts in the Visited state even on a fresh save.")]
    public bool startVisited = false;

    // Hand‑over belt thickness (normalised box‑space units [0‒0.25]).
    public static float beltNorm = 0.08f;

    // ─────────────────────────────────────────────────────────────────────────────
    // Runtime state

    /// <summary>True once the player has been inside (or <c>startVisited</c>).</summary>
    public bool Visited { get; private set; } = false;

    private Matrix4x4 _worldToBox;
    private BoxCollider _col;

    /// <summary>Inverse TRS that maps world → unit cube space.</summary>
    public Matrix4x4 worldToBox => _worldToBox;

    // ─────────────────────────────────────────────────────────────────────────────
    // Unity lifecycle

    private void Awake()
    {
        _col = GetComponent<BoxCollider>();
        _col.isTrigger = true;

        // Derive a stable key if none provided (SceneName/HierarchyPath
        if (string.IsNullOrEmpty(zoneKey))
            zoneKey = $"{gameObject.scene.name}:{GetHierarchyPath(transform)}";

        CacheMatrix();

        // --- MODIFIED LOGIC ---
        // Check if the game is currently running
        if (Application.isPlaying)
        {
            // We are in Play Mode, so we can safely access the WorldDataManager
            // (assuming Script Execution Order is set correctly for runtime)
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnAfterLoad += HandleLoad;
                WorldDataManager.Instance.OnBeforeSave += HandleBeforeSave;
            }
            // Restore the visited state using save data
            Visited = startVisited || GetSavedVisited();
        }
        else // We are in the Editor and not in Play Mode
        {
            // In the editor, we can't access the save system.
            // Just use the value from the inspector field.
            Visited = startVisited;
        }
    }

    private void OnDestroy()
    {
        if (Application.isPlaying && WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnAfterLoad -= HandleLoad;
            WorldDataManager.Instance.OnBeforeSave -= HandleBeforeSave;
        }
    }

    private void OnEnable() => CacheMatrix();

    // ─────────────────────────────────────────────────────────────────────────────
    // Event handlers

    private void HandleLoad()
    {
        // When a save is loaded, pull our flag and update local state.
        Visited = startVisited || GetSavedVisited();
    }

    private void HandleBeforeSave()
    {
        // Ensure our current flag is stored just before serialisation.
        SetSavedVisited(Visited || startVisited);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Trigger logic

    private void OnTriggerEnter(Collider other)
    {
        if (Visited) return;
        if (other.CompareTag("Player"))
        {
            Visited = true;
            if (Application.isPlaying && WorldDataManager.Instance != null)
                SetSavedVisited(true); // Persist immediately so mid‑session quick‑save works.
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API used by the controller

    public void CacheMatrix()
    {
        var t = transform;
        Vector3 worldCenter = t.TransformPoint(_col.center);
        Vector3 worldSize = Vector3.Scale(t.lossyScale, _col.size);

        // clamp to avoid a singular matrix
        const float EPS = 1e-4f;
        worldSize = new Vector3(
            Mathf.Max(Mathf.Abs(worldSize.x), EPS),
            Mathf.Max(Mathf.Abs(worldSize.y), EPS),
            Mathf.Max(Mathf.Abs(worldSize.z), EPS)
        );

        _worldToBox = Matrix4x4.TRS(worldCenter, t.rotation, worldSize).inverse;
    }

    /// <summary>Activation weight for global fade/mask (0 outside belt → 1 deep inside).</summary>
    public float ActivationWeight(Vector3 playerPos)
    {
        Vector3 p = _worldToBox.MultiplyPoint(playerPos);
        float s = Mathf.Min(Mathf.Min(0.5f - Mathf.Abs(p.x), 0.5f - Mathf.Abs(p.y)), 0.5f - Mathf.Abs(p.z));
        return Mathf.Clamp01(Mathf.InverseLerp(-beltNorm, beltNorm, s));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Save helpers (thin wrappers over WorldDataManager)

    private bool GetSavedVisited()
    {
        return WorldDataManager.Instance != null &&
               WorldDataManager.Instance.GetGlobalFlag(zoneKey);
    }

    private void SetSavedVisited(bool value)
    {
        WorldDataManager.Instance?.SetGlobalFlag(zoneKey, value);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Utility

    private static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
