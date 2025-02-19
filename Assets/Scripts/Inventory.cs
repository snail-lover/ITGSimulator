using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    public List<CreateInventoryItem> items = new List<CreateInventoryItem>();
    private HashSet<string> playerItems = new HashSet<string>(); // Stores item names


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep inventory across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(CreateInventoryItem item)
    {
        items.Add(item); // Keep storing the full object
        playerItems.Add(item.itemName); // Store its name in the HashSet too
        Debug.Log(item.itemName + " added to inventory!");
    }

    public bool HasItem(string itemName)
{
    Debug.Log("Checking if inventory contains: " + itemName);
    
    if (playerItems.Count == 0)
    {
        Debug.Log("Inventory is empty!");
    }
    else
    {
        Debug.Log("Current items in inventory:");
        foreach (var item in playerItems)
        {
            Debug.Log("- " + item);
        }
    }

    return playerItems.Contains(itemName);
}


    public void RemoveItemByID(string itemID)
    {
        items.RemoveAll(item => item.id == itemID);
    }

}
