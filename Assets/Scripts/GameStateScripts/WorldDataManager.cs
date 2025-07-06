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
    // --- Singleton Pattern ---
    public static WorldDataManager Instance { get; private set; }

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
        // --- Singleton Initialization ---
        if (Instance != null && Instance != this)
        {
            // If another instance already exists, destroy this one.
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // CRITICAL: This line keeps the manager (and all its data)
            // alive when you switch between scenes.
            DontDestroyOnLoad(this.gameObject);
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

    /// <summary>
    /// Gets or creates the runtime data for a specific NPC.
    /// The NpcController calls this in its Awake() to get its persistent state.
    /// </summary>
    public NpcRuntimeData GetOrCreateNpcData(NpcConfig config)
    {
        // Use the ScriptableObject's unique asset name as the key.
        string key = config.name;

        // Check the "NPCs" chapter to see if we already have data for this NPC.
        if (saveData.allNpcRuntimeData.TryGetValue(key, out NpcRuntimeData existingData))
        {
            // Found it! Return the existing data (e.g., from a loaded game).
            return existingData;
        }

        // Not found. This is a new game. Create new data from the config's defaults.
        NpcRuntimeData newData = new NpcRuntimeData(config);

        // Add the new data to our central database so it can be saved.
        saveData.allNpcRuntimeData.Add(key, newData);

        return newData;
    }

    public void AddItemToInventory(string itemID)
    {
        if (!saveData.playerInventory.itemIDs.Contains(itemID))
        {
            saveData.playerInventory.itemIDs.Add(itemID);
            Debug.Log($"[WorldDataManager] Added '{itemID}' to inventory.");
        }
    }

    public void RemoveItemFromInventory(string itemID)
    {
        if (saveData.playerInventory.itemIDs.Contains(itemID))
        {
            saveData.playerInventory.itemIDs.Remove(itemID);
            Debug.Log($"[WorldDataManager] Removed '{itemID}' from inventory.");
        }
    }

    public bool PlayerHasItem(string itemID)
    {
        return saveData.playerInventory.itemIDs.Contains(itemID);
    }

    public void MarkWorldItemAsPickedUp(string worldID)
    {
        if (string.IsNullOrEmpty(worldID)) return;

        if (!saveData.pickedUpWorldItemIDs.Contains(worldID))
        {
            saveData.pickedUpWorldItemIDs.Add(worldID);
            Debug.Log($"[WorldDataManager] Marked world item '{worldID}' as picked up.");
        }
    }

    /// <summary>
    /// Checks if a unique world item has already been picked up.
    /// </summary>
    public bool IsWorldItemPickedUp(string worldID)
    {
        if (string.IsNullOrEmpty(worldID)) return false;

        return saveData.pickedUpWorldItemIDs.Contains(worldID);
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

    [ContextMenu("Load Game")] // Lets you right-click the component in the Inspector to load
    public void LoadGame()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(filePath))
        {
            // Read the JSON string from the file
            string json = File.ReadAllText(filePath);
            // Overwrite the current saveData object with the one loaded from the file
            JsonUtility.FromJsonOverwrite(json, saveData); // Use FromJsonOverwrite to avoid creating a new object, which is better for references

            // --- FIX: A loaded game is, by definition, NOT a new game. ---
            isNewGame = false;

            Debug.Log("Game Loaded! Firing OnAfterLoad event...");
            OnAfterLoad?.Invoke();
        }
        else
        {
            Debug.LogWarning("No save file found to load.");
        }
    }
}