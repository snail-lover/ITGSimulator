// Inventory.cs (Refactored)

using System;
using UnityEngine;

/// <summary>
/// This script is now a "manager" for inventory-related actions and UI.
/// It does NOT hold the inventory data itself. Instead, it reads from and
/// writes to the WorldDataManager, which is the single source of truth.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    // The event is still very useful for updating the UI!
    public event Action OnInventoryChanged;

    // --- REMOVED ---
    // We no longer store the items locally. The WorldDataManager does.
    // public List<CreateInventoryItem> items = new List<CreateInventoryItem>();
    // public HashSet<string> playerItems = new HashSet<string>();

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

    /// <summary>
    /// Adds an item by telling the WorldDataManager to update its state.
    /// </summary>
    public void AddItem(CreateInventoryItem item)
    {
        if (item == null) return;

        // --- THE CHANGE ---
        // Instead of adding to a local list, we tell the central manager to do it.
        WorldDataManager.Instance.AddItemToInventory(item.id);

        Debug.Log("[Inventory] Firing OnInventoryChanged event!");
        Debug.Log($"Told WorldDataManager to add item '{item.itemName}' (ID: {item.id}).");

        // We still fire the event so the UI knows to refresh.
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Removes an item by telling the WorldDataManager to update its state.
    /// </summary>
    public void RemoveItemByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;

        // --- THE CHANGE ---
        // We just tell the central manager to remove the item.
        WorldDataManager.Instance.RemoveItemFromInventory(itemID);

        Debug.Log($"Told WorldDataManager to remove item with ID '{itemID}'.");

        // Fire the event so the UI knows to refresh.
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Checks if the player has an item by asking the WorldDataManager.
    /// </summary>
    public bool HasItem(string itemID)
    {
        // --- THE CHANGE ---
        // We ask the single source of truth.
        return WorldDataManager.Instance.PlayerHasItem(itemID);
    }

    /// <summary>
    /// A helper method for things that still use itemName instead of itemID.
    /// Note: It's better to use unique IDs whenever possible.
    /// </summary>
    public bool HasItemByName(string itemName)
    {
        // This is a more complex query, but shows the power of the central system.
        // You would need to add a method to WorldDataManager to support this,
        // or have an ItemDatabase to look up the ID from the name first.
        // For now, let's assume we primarily use IDs.
        // For simplicity, we'll keep the HasItem(itemID) method as primary.
        return false; // Placeholder
    }
    public bool HasItemWithTag(ItemTag tagToCheck)
    {
        // 1. Get the list of all item IDs the player currently possesses.
        var playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

        // 2. Loop through each item ID.
        foreach (string itemID in playerItemIDs)
        {
            // 3. Look up the full item data from the database using the ID.
            CreateInventoryItem itemData = ItemDatabase.Instance.GetItemByID(itemID);

            if (itemData != null)
            {
                // 4. This is the magic! Use a bitwise AND to check if the item's tags include the one we're looking for.
                // Example: item.tags is (Camera | Technology) = 17. tagToCheck is (Camera) = 1.
                // 17 & 1 -> (010001 & 000001) -> 000001, which is not 0. So it's a match!
                if ((itemData.tags & tagToCheck) != 0)
                {
                    return true; // Found an item with the tag, we can stop looking.
                }
            }
        }

        // 5. If we get through the whole list without finding a match, return false.
        return false;
    }

    /// <summary>
    /// A useful helper to get the first item that matches a tag, in case you need to use or remove it.
    /// </summary>
    /// <param name="tagToFind">The tag to look for.</param>
    /// <returns>The CreateInventoryItem data of the first matching item, or null if none are found.</returns>
    public CreateInventoryItem GetFirstItemWithTag(ItemTag tagToFind)
    {
        var playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;
        foreach (string itemID in playerItemIDs)
        {
            CreateInventoryItem itemData = ItemDatabase.Instance.GetItemByID(itemID);
            if (itemData != null && (itemData.tags & tagToFind) != 0)
            {
                return itemData; // Return the full item data
            }
        }
        return null; // No matching item found
    }
}