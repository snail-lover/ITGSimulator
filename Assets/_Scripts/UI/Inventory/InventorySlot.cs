using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Game.Core; 
using Game.Gameplay;

namespace Game.UI
{
    public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectionOutline;

        private InventorySlotContext _contextHandler;
        
        private string _itemID; 
        private string _uniqueID; // <--- NEW: Cache the Unique ID

        void Awake()
        {
            _contextHandler = GetComponent<InventorySlotContext>();
        }

        public void Setup(InventoryItemInstance instance)
        {
            if (instance != null)
            {
                _itemID = instance.itemID;
                _uniqueID = instance.uniqueID; // <--- Cache it here

                ItemDefinition def = ItemDatabase.Instance.GetItemByID(instance.itemID);
                if (def != null)
                {
                    iconImage.sprite = def.icon;
                    iconImage.enabled = true;
                    if (backgroundImage != null) backgroundImage.enabled = true;
                    button.interactable = true;
                    
                    if (_contextHandler != null) _contextHandler.itemInstance = instance;
                }
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
            _uniqueID = null; // Clear it
            
            iconImage.sprite = null;
            iconImage.enabled = false;
            if (backgroundImage != null) backgroundImage.enabled = false;
            button.interactable = false;
            
            if (selectionOutline) selectionOutline.SetActive(false);
            if (_contextHandler != null) _contextHandler.itemInstance = null;
        }

        public void OnClick()
        {
            if (string.IsNullOrEmpty(_uniqueID)) return;

            // Toggle logic using Unique ID
            if (SelectedItemState.SelectedUniqueID == _uniqueID)
                SelectedItemState.Clear();
            else
                SelectedItemState.Set(_itemID, _uniqueID);
        }

        public void UpdateSelectionVisual()
        {
            if (selectionOutline != null)
            {
                // FIX: Check against UniqueID, not ItemID
                bool isSelected = !string.IsNullOrEmpty(_uniqueID) &&
                                  SelectedItemState.SelectedUniqueID == _uniqueID;
                selectionOutline.SetActive(isSelected);
            }
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            InventoryHoverState.Set(_itemID, _uniqueID);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            InventoryHoverState.Clear();
        }
    }
}