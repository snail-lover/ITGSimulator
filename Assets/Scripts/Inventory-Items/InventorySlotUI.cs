// InventorySlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Add this namespace

// --- ADD THE DRAG HANDLER INTERFACES ---
public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;

    private CreateInventoryItem associatedItem;

    public void Setup(CreateInventoryItem item)
    {
        associatedItem = item;

        if (item == null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            itemNameText.text = "";
        }
        else
        {
            itemIcon.sprite = item.icon;
            itemIcon.enabled = true;
            itemNameText.text = item.itemName;
        }
    }

    // --- NEW METHOD ---
    // A public method to control the icon's visibility from the outside
    public void SetIconVisibility(bool isVisible)
    {
        if (itemIcon != null)
        {
            itemIcon.enabled = isVisible;
        }
    }

    // --- NEW: DRAG HANDLER IMPLEMENTATIONS ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check if we have an item and that a world prefab exists for it
        if (associatedItem != null && associatedItem.worldPrefab != null)
        {
            // Tell the controller to start the drag process
            UIDragDropController.Instance.StartDrag(associatedItem, this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Tell the controller to update the position of the ghost icon
        UIDragDropController.Instance.UpdateDrag();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Tell the controller the drag has ended so it can check for a valid drop
        UIDragDropController.Instance.EndDrag();
    }
}