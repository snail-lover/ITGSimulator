using UnityEngine;
using UnityEngine.UI;
using Game.Core; // For ItemDefinition and SelectedItemState
using Game.Gameplay; // For Inventory

namespace Game.UI
{
    public class InventorySlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectionOutline; // Optional visual

        private string _itemID;

        public void Setup(ItemDefinition item)
        {
            if (item != null)
            {
                _itemID = item.id;
                iconImage.sprite = item.icon;
                iconImage.enabled = true; // Show icon
                button.interactable = true;
            }
            else
            {
                Clear();
            }

            UpdateSelectionVisual();
        }

        public void Clear()
        {
            _itemID = null;
            iconImage.sprite = null;
            iconImage.enabled = false; // Hide icon
            button.interactable = false;
            if (selectionOutline) selectionOutline.SetActive(false);
        }

        // Linked to the Button Component in Inspector
        public void OnClick()
        {
            if (string.IsNullOrEmpty(_itemID)) return;

            // Toggle selection logic
            if (SelectedItemState.SelectedItemID == _itemID)
            {
                // Deselect if clicking the same item
                SelectedItemState.Clear();
            }
            else
            {
                // Select this item
                SelectedItemState.Set(_itemID);
                Debug.Log($"Selected Item: {_itemID}");
            }
        }

        public void UpdateSelectionVisual()
        {
            if (selectionOutline != null)
            {
                bool isSelected = !string.IsNullOrEmpty(_itemID) &&
                                  SelectedItemState.SelectedItemID == _itemID;
                selectionOutline.SetActive(isSelected);
            }
        }
    }
}