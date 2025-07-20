// GameSaveData.cs
using System.Collections.Generic;
using UnityEngine; // Needed for ISerializationCallbackReceiver
using System.Linq;

/// <summary>
/// A pure data container class that holds ALL the savable state for the entire game.
/// Implements ISerializationCallbackReceiver to handle saving/loading Dictionaries,
/// which JsonUtility cannot do directly.
/// </summary>
[System.Serializable]
public class GameSaveData : ISerializationCallbackReceiver
{
    // --- Chapter 1: Global Flags ---
    public Dictionary<string, bool> globalFlags = new Dictionary<string, bool>();

    // --- Chapter 2: The State of All NPCs ---
    public Dictionary<string, NpcRuntimeData> allNpcRuntimeData = new Dictionary<string, NpcRuntimeData>();

    // --- Chapter 3: Inventory ---
    public InventoryState playerInventory = new InventoryState();

    // --- Chapter 4: Player Position ---
    public PlayerState playerState = new PlayerState();

    // --- Chapter 5: World Item State ---
    public HashSet<string> pickedUpWorldItemIDs = new HashSet<string>();

    // --- NEW CHAPTER 6: Completed One-Time Dialogues ---
    public HashSet<string> completedHighPriorityDialogueIDs = new HashSet<string>();


    // --- Serialization Helpers (The Magic for Dictionaries) ---
    // These lists are what get saved to the file. They are temporary storage.
    [SerializeField] private List<string> _flagKeys = new List<string>();
    [SerializeField] private List<bool> _flagValues = new List<bool>();
    [SerializeField] private List<string> _npcKeys = new List<string>();
    [SerializeField] private List<NpcRuntimeData> _npcValues = new List<NpcRuntimeData>();
    [SerializeField] private List<string> _pickedUpItemsList = new List<string>();
    // Add a list for the new HashSet
    [SerializeField] private List<string> _completedHighPriorityDialogueIDsList = new List<string>();


    public void OnBeforeSerialize()
    {
        // This makes it easy to differentiate a fresh save object from a loaded one.
        globalFlags["_system_hasBeenSaved"] = true;

        // Copy data from Dictionaries to Lists before saving
        _flagKeys.Clear();
        _flagValues.Clear();
        foreach (var kvp in globalFlags)
        {
            _flagKeys.Add(kvp.Key);
            _flagValues.Add(kvp.Value);
        }

        _npcKeys.Clear();
        _npcValues.Clear();
        foreach (var kvp in allNpcRuntimeData)
        {
            _npcKeys.Add(kvp.Key);
            _npcValues.Add(kvp.Value);
        }

        _pickedUpItemsList = pickedUpWorldItemIDs.ToList();

        // Convert the new HashSet to a List for serialization
        _completedHighPriorityDialogueIDsList = completedHighPriorityDialogueIDs.ToList();

    }

    public void OnAfterDeserialize()
    {
        // Rebuild Dictionaries from Lists after loading
        globalFlags = new Dictionary<string, bool>();
        for (int i = 0; i < _flagKeys.Count; i++)
        {
            globalFlags.Add(_flagKeys[i], _flagValues[i]);
        }

        allNpcRuntimeData = new Dictionary<string, NpcRuntimeData>();
        for (int i = 0; i < _npcKeys.Count; i++)
        {
            allNpcRuntimeData.Add(_npcKeys[i], _npcValues[i]);
        }

        pickedUpWorldItemIDs = new HashSet<string>(_pickedUpItemsList);

        // Rebuild the new HashSet from the loaded List
        completedHighPriorityDialogueIDs = new HashSet<string>(_completedHighPriorityDialogueIDsList);

    }
}