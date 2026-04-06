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
        public Transform gridContainer; 
        public int totalSlots = 40; 

        private List<InventorySlot> _slots = new List<InventorySlot>();

        void Start()
        {
            GenerateGrid();
            RedrawUI();

            if (Inventory.Instance != null)
                Inventory.Instance.OnInventoryChanged += RedrawUI;

            // FIX: Match the new parameterless signature
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
            foreach (Transform child in gridContainer) Destroy(child.gameObject);
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
            List<InventoryItemInstance> inventoryItems = WorldDataManager.Instance.saveData.playerInventory.items;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < inventoryItems.Count)
                {
                    _slots[i].Setup(inventoryItems[i]);
                }
                else
                {
                    _slots[i].Clear();
                }
            }
        }

        // --- FIX: Remove the 'string newID' parameter ---
        private void OnSelectionChanged()
        {
            // The slots will internally check SelectedItemState.SelectedUniqueID
            foreach (var slot in _slots) slot.UpdateSelectionVisual();
        }
    }
}