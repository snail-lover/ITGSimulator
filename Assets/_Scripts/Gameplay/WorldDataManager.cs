// WorldDataManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.IO; // Required for saving and loading files
using System;
using UnityEngine.SceneManagement; // Add this if you handle scene loading here

/// <summary>
/// The new central manager for ALL game state. It is the "Librarian".
/// It holds the "Master Ledger" (the GameSaveData object) and provides
/// safe, clean methods for other systems to read or write data.
/// It is a persistent Singleton that survives scene changes.
/// </summary>
public class WorldDataManager : MonoBehaviour // Renamed from WorldStateManager
{
    // --- The New Singleton Pattern ---

    private static bool _isQuitting = false;
    private void OnApplicationQuit()
{
    _isQuitting = true;
}

    // 1. The private variable that holds the actual instance.
    private static WorldDataManager _instance;

    // 2. The public property that all other scripts will use to access the manager.
    public static WorldDataManager Instance
    {
        get
        {
            if (_instance == null && !_isQuitting)
            {
                Debug.LogError("WorldDataManager.Instance is null! This usually means the WorldDataManager is missing from the scene or has been destroyed. Please ensure a GameObject with the WorldDataManager component exists and is active in your scene.");
            }
            return _instance;
        }
    }

    // --- The Master Data Object ---
    // Instead of just one dictionary, this manager now holds the entire GameSaveData object.
    public GameSaveData saveData = new GameSaveData();
    public event Action OnBeforeSave;
    public event Action OnAfterLoad;
    public IReadOnlyDictionary<string, bool> GlobalFlags => saveData.globalFlags;

    // --- NEW: The definitive flag to track game state ---
    public bool isNewGame { get; private set; } = true;

    private void Awake()
    {
        // We now check and assign to the private _instance variable.

        if (_instance != null && _instance != this)
        {
            // If another instance already exists, destroy this one.
            Destroy(this.gameObject);
        }
        else
        {
            // Assign this component to the private static variable.
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    void OnEnable()
    {
        // Subscribe to the event that fires after a scene is finished loading.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Always unsubscribe to prevent errors.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This method is now called automatically by Unity AFTER a scene loads.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // We only want to fire the load event if this is NOT a new game.
        // If it's a new game, we do nothing.
        // If we just loaded data, isNewGame will be false.
        if (!isNewGame)
        {
            Debug.Log("[WorldDataManager] Scene has loaded. Now firing OnAfterLoad event to restore state.");
            OnAfterLoad?.Invoke();
        }
    }

    /// <summary>
    /// Gets a global true/false flag from the save data.
    /// This replaces your old GetState method.
    /// </summary>
    public bool GetGlobalFlag(string key)
    {
        // It now looks inside the 'globalFlags' chapter of the save data.
        if (saveData.globalFlags.TryGetValue(key, out bool value))
        {
            return value;
        }
        return false;
    }

    /// <summary>
    /// Sets a global true/false flag in the save data.
    /// This replaces your old SetState method.
    /// </summary>
    public void SetGlobalFlag(string key, bool value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("Attempted to set a world state with a null or empty key.");
            return;
        }

        Debug.Log($"[WorldDataManager] Setting Global Flag '{key}' to '{value}'.");
        // It now writes to the 'globalFlags' chapter of the save data.
        saveData.globalFlags[key] = value;
    }
    

    // --- INVENTORY LOGIC (UPDATED) ---

    // 1. New method: Adds a fully formed item (with states like "Full" or "Hot")
    public void AddItemInstance(InventoryItemInstance newItem)
    {
        saveData.playerInventory.items.Add(newItem);
        Debug.Log($"[WorldDataManager] Added item instance '{newItem.itemID}' (States: {newItem.savedStates.Count})");
    }

    // 2. Backward Compatibility: Adds a simple item by string ID (creates a default instance)
    public void AddItemToInventory(string itemID)
    {
        // Create a fresh instance with a unique GUID
        var newInstance = new InventoryItemInstance
        {
            itemID = itemID,
            uniqueID = System.Guid.NewGuid().ToString()
        };
        
        saveData.playerInventory.items.Add(newInstance);
        Debug.Log($"[WorldDataManager] Added basic item '{itemID}' to inventory.");
    }

    // 3. Updated: Find an item by string ID and remove the first match
    public void RemoveItemFromInventory(string itemID)
    {
        // Find the object in the list that matches the ID
        var itemToRemove = saveData.playerInventory.items.Find(x => x.itemID == itemID);

        if (itemToRemove != null)
        {
            saveData.playerInventory.items.Remove(itemToRemove);
            Debug.Log($"[WorldDataManager] Removed '{itemID}' from inventory.");
        }
    }

    // 4. Updated: Check if any item in the list has this ID
    public bool PlayerHasItem(string itemID)
    {
        // Return true if Find returns something not null
        return saveData.playerInventory.items.Exists(x => x.itemID == itemID);
    }

    // --- Saving and Loading Logic ---

    [ContextMenu("Save Game")] // Lets you right-click the component in the Inspector to save
    public void SaveGame()
    {
        Debug.Log("[WorldDataManager] Firing OnBeforeSave event...");
        OnBeforeSave?.Invoke();
        string filePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        // Turn the entire GameSaveData object into a JSON string
        string json = JsonUtility.ToJson(saveData, true);
        // Write that string to a file
        File.WriteAllText(filePath, json);
        Debug.Log($"Game Saved to: {filePath}");
    }

    public bool LoadGameFromFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, saveData);

            isNewGame = false;

            Debug.Log("Game data loaded from file! Firing OnAfterLoad event...");

            // It's generally better to fire the event AFTER the scene loads,
            // but for now, we will keep your existing structure.
            // We will call this from a new OnSceneLoaded method later.
            // OnAfterLoad?.Invoke(); 

            return true; // <<< RETURN TRUE on success
        }
        else
        {
            Debug.LogWarning("No save file found to load.");
            return false; // <<< RETURN FALSE on failure
        }
    }

    /// <summary>
    /// Marks a high-priority dialogue as having been completed.
    /// This prevents it from triggering again.
    /// </summary>
    public void MarkHighPriorityDialogueAsCompleted(string triggerID)
    {
        if (string.IsNullOrEmpty(triggerID)) return;

        if (saveData.completedHighPriorityDialogueIDs.Add(triggerID))
        {
            Debug.Log($"[WorldDataManager] Marked high-priority dialogue '{triggerID}' as completed.");
        }
    }

    /// <summary>
    /// Checks if a high-priority dialogue has already been completed.
    /// </summary>
    public bool HasHighPriorityDialogueBeenCompleted(string triggerID)
    {
        if (string.IsNullOrEmpty(triggerID)) return true; // Treat empty IDs as "completed" to be saf

        return saveData.completedHighPriorityDialogueIDs.Contains(triggerID);
    }

        /// <summary>Returns all world flags for debug display.</summary>
    public IReadOnlyDictionary<string, bool> GetAllFlags()
        => saveData.globalFlags;

    /// <summary>Returns all completed one-time dialogue entry IDs for debug display.</summary>
    public IReadOnlyCollection<string> GetCompletedDialogues()
        => saveData.completedHighPriorityDialogueIDs;
}