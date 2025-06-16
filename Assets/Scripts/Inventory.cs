// --- START OF FILE Inventory.cs --- // (Updated)

using System; // Required for Action
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    public List<CreateInventoryItem> items = new List<CreateInventoryItem>();
    public HashSet<string> playerItems = new HashSet<string>(); // Stores unique item names

    public event Action OnInventoryChanged; // <-- ADD THIS EVENT

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(CreateInventoryItem item)
    {
        items.Add(item);
        playerItems.Add(item.itemName);
        Debug.Log(item.itemName + " added to inventory!");
        OnInventoryChanged?.Invoke(); // <-- TRIGGER THE EVENT
    }

    public bool HasItem(string itemName)
    {
        // Debug.Log("Checking if inventory contains: " + itemName);
        // if (playerItems.Count == 0)
        // {
        //     Debug.Log("Inventory is empty!");
        // }
        // else
        // {
        //     Debug.Log("Current items in inventory:");
        //     foreach (var itemInInv in playerItems)
        //     {
        //         Debug.Log("- " + itemInInv);
        //     }
        // }
        return playerItems.Contains(itemName);
    }

    public void RemoveItemByID(string itemID)
    {
        int initialCount = items.Count;
        items.RemoveAll(item => item.id == itemID);

        if (items.Count < initialCount) // If something was actually removed
        {
            // Rebuild playerItems to reflect the current state of the main items list
            // This ensures playerItems (unique names) is accurate after removal.
            playerItems.Clear();
            foreach (CreateInventoryItem currentItem in items)
            {
                playerItems.Add(currentItem.itemName);
            }
            Debug.Log("Item(s) with ID " + itemID + " removed from inventory.");
            OnInventoryChanged?.Invoke(); // <-- TRIGGER THE EVENT
        }
        else
        {
            Debug.Log("No item with ID " + itemID + " found to remove.");
        }
    }
}