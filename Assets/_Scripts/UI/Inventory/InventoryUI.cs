using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;

namespace Game.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject slotPrefab;
        public Transform gridContainer; // The object with GridLayoutGroup
        public int totalSlots = 40; // 10x4

        private List<InventorySlot> _slots = new List<InventorySlot>();

        void Start()
        {
            GenerateGrid();

            // Initial Draw
            RedrawUI();

            // Subscribe to events
            if (Inventory.Instance != null)
                Inventory.Instance.OnInventoryChanged += RedrawUI;

            // Subscribe to selection changes to update outlines
            SelectedItemState.OnChanged += OnSelectionChanged;
        }

        void OnDestroy()
        {
            if (Inventory.Instance != null)
                Inventory.Instance.OnInventoryChanged -= RedrawUI;

            SelectedItemState.OnChanged -= OnSelectionChanged;
        }

        private void GenerateGrid()
        {
            // Clear existing children (useful for testing)
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

            for (int i = 0; i < totalSlots; i++)
            {
                GameObject newSlotObj = Instantiate(slotPrefab, gridContainer);
                InventorySlot slotScript = newSlotObj.GetComponent<InventorySlot>();
                _slots.Add(slotScript);
            }
        }

        private void RedrawUI()
        {
            // 1. Get the list of Item IDs from the Save Data
            List<string> inventoryIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

            // 2. Loop through our UI slots
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < inventoryIDs.Count)
                {
                    // This slot has an item
                    string id = inventoryIDs[i];
                    ItemDefinition def = ItemDatabase.Instance.GetItemByID(id);
                    _slots[i].Setup(def);
                }
                else
                {
                    // This slot is empty
                    _slots[i].Clear();
                }
            }
        }

        private void OnSelectionChanged(string newID)
        {
            // Just refresh visual outlines
            foreach (var slot in _slots)
            {
                slot.UpdateSelectionVisual();
            }
        }
    }
}