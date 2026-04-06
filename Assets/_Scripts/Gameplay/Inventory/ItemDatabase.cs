using System.Collections.Generic;
using UnityEngine;
using Game.Core; 
using System.Linq;

namespace Game.Gameplay
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        private Dictionary<string, ItemDefinition> _itemDatabase = new Dictionary<string, ItemDefinition>();
        private List<FluidDefinition> _fluidDatabase = new List<FluidDefinition>();
        private Dictionary<string, StateDefinition> _stateDatabase = new Dictionary<string, StateDefinition>();

        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this.gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            if (_isInitialized) return;

            _itemDatabase.Clear();
            _stateDatabase.Clear();

            // 1. Load Items
            var allItems = Resources.LoadAll<ItemDefinition>("Items");
            foreach (var item in allItems)
            {
                if (!string.IsNullOrEmpty(item.id) && !_itemDatabase.ContainsKey(item.id))
                    _itemDatabase.Add(item.id, item);
            }

            // 2. Load Fluids
            var allFluids = Resources.LoadAll<FluidDefinition>("Fluids"); 
            _fluidDatabase = allFluids.ToList();

            // 3. Load States (Matches Asset Name to State)
            var allStates = Resources.LoadAll<StateDefinition>("States");
            foreach (var state in allStates)
            {
                if (!_stateDatabase.ContainsKey(state.name))
                {
                    _stateDatabase.Add(state.name, state);
                }
            }

            Debug.Log($"[ItemDatabase] Loaded {_itemDatabase.Count} Items, {_fluidDatabase.Count} Fluids, {_stateDatabase.Count} States.");
            _isInitialized = true;
        }

        public ItemDefinition GetItemByID(string itemID)
        {
            if (!_isInitialized) InitializeDatabase();
            if (string.IsNullOrEmpty(itemID)) return null;
            _itemDatabase.TryGetValue(itemID, out var item);
            return item;
        }
        
        public FluidDefinition GetFluidByID(int id)
        {
            if (!_isInitialized) InitializeDatabase();
            return _fluidDatabase.FirstOrDefault(f => f.fluidID == id);
        }

        public StateDefinition GetStateByName(string stateName)
        {
            if (!_isInitialized) InitializeDatabase();
            if (string.IsNullOrEmpty(stateName)) return null;
            _stateDatabase.TryGetValue(stateName, out var state);
            return state;
        }

        // --- HELPER WRAPPERS (Restored) ---
        public bool DoesItemExist(string itemID) => GetItemByID(itemID) != null;
        public string GetItemName(string itemID) => GetItemByID(itemID)?.itemName ?? "Invalid Item";
        public Sprite GetItemIcon(string itemID) => GetItemByID(itemID)?.icon;
        public GameObject GetItemWorldPrefab(string itemID) => GetItemByID(itemID)?.worldPrefab;
        public List<ItemTagDefinition> GetItemTags(string itemID) => GetItemByID(itemID)?.tags ?? new List<ItemTagDefinition>();
    }
}