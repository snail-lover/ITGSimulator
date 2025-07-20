// WorldItem.cs
using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    [Tooltip("The ScriptableObject that defines this item.")]
    public CreateInventoryItem itemData;

    private void Awake()
    {
        if (itemData == null)
        {
            Debug.LogError($"The WorldItem on '{gameObject.name}' is missing its Item Data! It doesn't know what item it is.", this);
        }
    }

    // A helper function that other scripts (like a future 'Pickup' script) can call.
    public void PickupItem()
    {
        if (itemData != null && Inventory.Instance != null)
        {
            Debug.Log($"Picking up {itemData.itemName}.");
            Inventory.Instance.AddItem(itemData);

            // The item is now in the inventory, so we destroy its physical representation.
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Could not pick up item. ItemData or Inventory.Instance is missing.", this);
        }
    }
}