// --- START OF FILE InventoryUI.cs --- // (Updated)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryDisplayPanel;
    public GameObject itemTextPrefab;
    public Transform itemContainer;

    private bool isPanelVisible = false;

    void Start()
    {
        if (inventoryDisplayPanel == null)
        {
            Debug.LogError("InventoryDisplayPanel is not assigned in the Inspector!");
            enabled = false;
            return;
        }
        if (itemTextPrefab == null)
        {
            Debug.LogError("ItemTextPrefab is not assigned in the Inspector!");
            enabled = false;
            return;
        }
        if (itemContainer == null)
        {
            Debug.LogError("ItemContainer (for dynamic items) is not assigned in the Inspector!");
            enabled = false;
            return;
        }

        inventoryDisplayPanel.SetActive(false);

        // Subscribe to the inventory changed event
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += HandleInventoryChanged;
        }
        else
        {
            // This might happen if InventoryUI initializes before Inventory.
            // Consider a more robust way to handle this if it becomes an issue,
            // e.g., by having Inventory find and notify UI or using a delayed subscription.
            Debug.LogWarning("Inventory.Instance was null when InventoryUI tried to subscribe. UI might not auto-update.");
        }
    }

    private void OnDestroy() // Or OnDisable if this object can be disabled/enabled
    {
        // Unsubscribe to prevent errors if InventoryUI is destroyed before Inventory
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryPanel();
        }
    }

    public void ToggleInventoryPanel()
    {
        isPanelVisible = !isPanelVisible;
        inventoryDisplayPanel.SetActive(isPanelVisible);

        if (isPanelVisible)
        {
            UpdateInventoryDisplay(); // Fresh update when opening
        }
    }

    // This method will be called when the OnInventoryChanged event is fired
    private void HandleInventoryChanged()
    {

        Debug.Log("[InventoryUI] HandleInventoryChanged event received!");
        if (isPanelVisible) // Only update the UI if it's currently showing
        {
            Debug.Log("Inventory changed, updating UI display.");
            UpdateInventoryDisplay();
        }
        else
        {
            Debug.Log("Inventory changed, but UI is not visible. No immediate UI update.");
        }
    }

    public void UpdateInventoryDisplay()
    {
        // --- THIS IS THE NEW LOGIC ---

        Debug.Log("[InventoryUI] Executing UpdateInventoryDisplay().");
        // Safety checks for the new systems
        if (WorldDataManager.Instance == null || ItemDatabase.Instance == null || itemContainer == null) return;

        // Clear previous items
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        // 1. Get the list of item IDs from the central data store.
        List<string> playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

        // Populate with current items
        if (playerItemIDs.Count == 0)
        {
            GameObject noItemEntry = Instantiate(itemTextPrefab, itemContainer);
            TextMeshProUGUI itemTextComponent = noItemEntry.GetComponent<TextMeshProUGUI>();
            if (itemTextComponent != null)
            {
                itemTextComponent.text = "Inventory is empty.";
            }
        }
        else
        {
            // 2. Loop through the list of IDs.
            foreach (string itemID in playerItemIDs)
            {
                // 3. For each ID, look up the full item data from our new database.
                CreateInventoryItem itemData = ItemDatabase.Instance.GetItemByID(itemID);

                // Make sure we found the item data (it might be null if an ID is invalid)
                if (itemData != null)
                {
                    // 4. Create the UI element and set its text to the item's NAME.
                    GameObject itemEntry = Instantiate(itemTextPrefab, itemContainer);
                    TextMeshProUGUI itemTextComponent = itemEntry.GetComponent<TextMeshProUGUI>();
                    if (itemTextComponent != null)
                    {
                        // Use the human-readable name for display!
                        itemTextComponent.text = itemData.itemName;
                    }
                }
                else
                {
                    Debug.LogWarning($"[InventoryUI] FAILED to find item in ItemDatabase with ID: '{itemID}'. Check your ScriptableObject asset.");
                    Debug.LogWarning($"[InventoryUI] Could not find item data for ID: {itemID}. It will not be displayed.");
                }
            }
        }
    }
}