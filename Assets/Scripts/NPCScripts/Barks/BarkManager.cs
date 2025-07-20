using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BarkManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static BarkManager _instance;
    public static BarkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find existing instance
                _instance = FindObjectOfType<BarkManager>();
                // Or create a new one if none exists
                if (_instance == null)
                {
                    GameObject go = new GameObject("BarkManager");
                    _instance = go.AddComponent<BarkManager>();
                }
            }
            return _instance;
        }
    }

    // This dictionary will store our barks, organized by their 'type' for fast lookups.
    private Dictionary<string, List<Bark>> _barksByType = new Dictionary<string, List<Bark>>();

    private const string BarksJsonPath = "Barks/barks"; // Path within the Resources folder

    void Awake()
    {
        // --- Enforce Singleton ---
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject); // Keep the manager alive across scene changes

        LoadBarks();
    }

    private void LoadBarks()
    {
        TextAsset barksJson = Resources.Load<TextAsset>(BarksJsonPath);
        if (barksJson == null)
        {
            Debug.LogError($"[BarkManager] Failed to load barks file from Resources path: {BarksJsonPath}");
            return;
        }

        BarkCollection barkCollection = JsonUtility.FromJson<BarkCollection>(barksJson.text);
        if (barkCollection == null || barkCollection.barks == null)
        {
            Debug.LogError("[BarkManager] Failed to parse JSON or bark collection is empty.");
            return;
        }

        // Organize the barks into the dictionary
        foreach (Bark bark in barkCollection.barks)
        {
            if (!_barksByType.ContainsKey(bark.type))
            {
                _barksByType[bark.type] = new List<Bark>();
            }
            _barksByType[bark.type].Add(bark);
        }

        Debug.Log($"[BarkManager] Successfully loaded and organized {barkCollection.barks.Count} barks.");
    }

    /// <summary>
    /// Retrieves a random bark of a specific type
    /// </summary>
    /// <param name="barkType">The type of bark to retrieve (e.g., "generic").</param>
    /// <returns>A Bark object or null if none are found.</returns>
    public Bark GetRandomBarkByType(string barkType)
    {
        if (_barksByType.ContainsKey(barkType) && _barksByType[barkType].Any())
        {
            List<Bark> availableBarks = _barksByType[barkType];
            return availableBarks[Random.Range(0, availableBarks.Count)];
        }

        Debug.LogWarning($"[BarkManager] No barks found for type: {barkType}");
        return null;
    }
}