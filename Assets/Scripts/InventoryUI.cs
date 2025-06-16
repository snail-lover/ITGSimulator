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
        if (Inventory.Instance == null || itemContainer == null) return; // Basic safety check

        // Clear previous items ONLY from the itemContainer
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        // Populate with current items
        if (Inventory.Instance.playerItems.Count == 0)
        {
            GameObject noItemEntry = Instantiate(itemTextPrefab, itemContainer);
            TextMeshProUGUI itemTextComponent = noItemEntry.GetComponent<TextMeshProUGUI>();
            if (itemTextComponent != null)
            {
                itemTextComponent.text = "Inventory is empty.";
            }
            else
            {
                Debug.LogError("ItemTextPrefab does not have a TextMeshProUGUI component!");
            }
        }
        else
        {
            foreach (string itemName in Inventory.Instance.playerItems)
            {
                GameObject itemEntry = Instantiate(itemTextPrefab, itemContainer);
                TextMeshProUGUI itemTextComponent = itemEntry.GetComponent<TextMeshProUGUI>();
                if (itemTextComponent != null)
                {
                    itemTextComponent.text = itemName;
                }
                else
                {
                    Debug.LogError("ItemTextPrefab does not have a TextMeshProUGUI component!");
                }
            }
        }
    }
}