// ItemDatabase.cs
using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // This is a fallback for editor scripts, will not be hit at runtime if set up correctly.
                _instance = FindFirstObjectByType<ItemDatabase>();
                if (_instance == null)
                {
                    // You could try loading a prefab here as a last resort
                    Debug.LogError("ItemDatabase instance is null and could not be found.");
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }


    private Dictionary<string, CreateInventoryItem> _itemDatabase = new Dictionary<string, CreateInventoryItem>();
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

    // Public initializer so editor scripts can force it to load
    public void InitializeDatabase()
    {
        if (_isInitialized) return;

        _itemDatabase.Clear();
        var allItems = Resources.LoadAll<CreateInventoryItem>("Items");

        foreach (var item in allItems)
        {
            if (string.IsNullOrEmpty(item.id))
            {
                Debug.LogWarning($"[ItemDatabase] Item asset '{item.name}' has no ID. Skipping.", item);
                continue;
            }

            if (!_itemDatabase.ContainsKey(item.id))
            {
                _itemDatabase.Add(item.id, item);
            }
            else
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate item ID '{item.id}' found. Skipping.", item);
            }
        }
        Debug.Log($"[ItemDatabase] Loaded {_itemDatabase.Count} items.");
        _isInitialized = true;
    }

    public CreateInventoryItem GetItemByID(string itemID)
    {
        if (!_isInitialized) InitializeDatabase(); // Safety check

        _itemDatabase.TryGetValue(itemID, out CreateInventoryItem item);
        return item;
    }
}