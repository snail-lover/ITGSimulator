// GameSaveData.cs
using System.Collections.Generic;
using UnityEngine; // Needed for ISerializationCallbackReceiver
using System.Linq;
using Game.Core; 

[System.Serializable]
public class GameSaveData : ISerializationCallbackReceiver
{
    // --- Chapter 1: Global Flags ---
    public Dictionary<string, bool> globalFlags = new Dictionary<string, bool>();
    

    // --- Chapter 3: Inventory ---
    public InventoryState playerInventory = new InventoryState();

    // --- Chapter 4: Player Position ---
    public PlayerState playerState = new PlayerState();

    // --- Chapter 5: Item Instances ---
    public List<SavedItemInstance> sceneItemInstances = new List<SavedItemInstance>();

    // --- Chapter 6: Completed One-Time Dialogues ---
    public HashSet<string> completedHighPriorityDialogueIDs = new HashSet<string>();

    // --- Chapter 7: Player Personality Profile ---
    // Because PlayerPersonalityProfile is [System.Serializable], JsonUtility will handle it.
    public PlayerPersonalityProfile playerPersonality = new PlayerPersonalityProfile();

    // 8 NPC relationships
    public Dictionary<string, float> npcRelationships = new Dictionary<string, float>();


    // --- Serialization Helpers (The Magic for Dictionaries) ---
    [SerializeField] private List<string> _flagKeys = new List<string>();
    [SerializeField] private List<bool> _flagValues = new List<bool>();
    [SerializeField] private List<string> _npcKeys = new List<string>();
    [SerializeField] private List<string> _completedHighPriorityDialogueIDsList = new List<string>();
    [SerializeField] private List<string> _relationshipKeys = new List<string>();
    [SerializeField] private List<float> _relationshipValues = new List<float>();

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
        _relationshipKeys.Clear();
        _relationshipValues.Clear();
        foreach (var kvp in npcRelationships)
        {
            _relationshipKeys.Add(kvp.Key);
            _relationshipValues.Add(kvp.Value);
        }

        _npcKeys.Clear();

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

        npcRelationships = new Dictionary<string, float>();
        for (int i = 0; i < _relationshipKeys.Count; i++)
            npcRelationships.Add(_relationshipKeys[i], _relationshipValues[i]);

        completedHighPriorityDialogueIDs = new HashSet<string>(_completedHighPriorityDialogueIDsList);

    }
}