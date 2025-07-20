// InventoryPanel.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // --- NEW: Singleton pattern and state tracking ---
    public static InventoryPanel Instance { get; private set; }
    public bool IsPointerOverPanel { get; private set; }


    [Header("Setup")]
    public Transform itemContainer;
    public GameObject itemSlotPrefab;

    // --- NEW: Awake for Singleton ---
    private void Awake()
    {
        // This makes sure there's only one instance, and that Draggable can find it.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    void OnEnable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += UpdateInventoryDisplay;
        }
        UpdateInventoryDisplay();
    }

    void OnDisable()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= UpdateInventoryDisplay;
        }
        // --- NEW: Cleanup singleton instance ---
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- NEW: Methods for IPointerEnterHandler and IPointerExitHandler ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOverPanel = true;
        Debug.Log("Mouse entered the inventory panel drop zone.");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOverPanel = false;
        Debug.Log("Mouse exited the inventory panel drop zone.");
    }

    private void UpdateInventoryDisplay()
    {
        if (WorldDataManager.Instance == null || ItemDatabase.Instance == null || itemContainer == null || itemSlotPrefab == null)
        {
            Debug.LogError("InventoryPanel cannot update - a critical reference is missing!");
            return;
        }

        // Clear previous items
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the list of item IDs from the central data store.
        List<string> playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

        if (playerItemIDs.Count == 0)
        {
            // You could instantiate a special "Empty" text prefab here if you want
            Debug.Log("Inventory is empty.");
        }
        else
        {
            // Loop through the list of IDs and create a slot for each one.
            foreach (string itemID in playerItemIDs)
            {
                CreateInventoryItem itemData = ItemDatabase.Instance.GetItemByID(itemID);
                if (itemData != null)
                {
                    GameObject slotGO = Instantiate(itemSlotPrefab, itemContainer);
                    InventorySlotUI slotUI = slotGO.GetComponent<InventorySlotUI>();
                    if (slotUI != null)
                    {
                        // Tell the new slot to set itself up with the correct item data
                        slotUI.Setup(itemData);
                    }
                }
            }
        }
    }
}