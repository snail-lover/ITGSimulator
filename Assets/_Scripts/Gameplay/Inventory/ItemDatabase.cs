using System.Collections.Generic;
using UnityEngine;
using Game.Core; // Needed for ItemDefinition

namespace Game.Gameplay
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        // CHANGED: Type is now ItemDefinition
        private Dictionary<string, ItemDefinition> _itemDatabase = new Dictionary<string, ItemDefinition>();
        private bool _isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
            InitializeDatabase();
        }

        public void InitializeDatabase()
        {
            if (_isInitialized) return;

            _itemDatabase.Clear();
            // Load all ItemDefinition assets from Resources/Items folder
            var allItems = Resources.LoadAll<ItemDefinition>("Items");

            foreach (var item in allItems)
            {
                if (string.IsNullOrEmpty(item.id)) continue;

                if (!_itemDatabase.ContainsKey(item.id))
                {
                    _itemDatabase.Add(item.id, item);
                }
            }
            Debug.Log($"[ItemDatabase] Loaded {_itemDatabase.Count} items.");
            _isInitialized = true;
        }

        public ItemDefinition GetItemByID(string itemID)
        {
            if (!_isInitialized) InitializeDatabase();
            if (string.IsNullOrEmpty(itemID)) return null;

            _itemDatabase.TryGetValue(itemID, out var item);
            return item;
        }

        // Helper Wrappers
        public bool DoesItemExist(string itemID) => GetItemByID(itemID) != null;
        public string GetItemName(string itemID) => GetItemByID(itemID)?.itemName ?? "Invalid Item";
        public Sprite GetItemIcon(string itemID) => GetItemByID(itemID)?.icon;
        public GameObject GetItemWorldPrefab(string itemID) => GetItemByID(itemID)?.worldPrefab;

        // CHANGED: Returns the list of tag assets now
        public List<ItemTagDefinition> GetItemTags(string itemID)
            => GetItemByID(itemID)?.tags ?? new List<ItemTagDefinition>();
    }
}