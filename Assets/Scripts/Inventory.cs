using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;
    public List<CreateInventoryItem> items = new List<CreateInventoryItem>();

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
        items.Add(item);
        Debug.Log(item.itemName + " added to inventory!");
    }

    public bool HasItem(string itemName)
    {
        return items.Exists(i => i.itemName == itemName);
    }
}
