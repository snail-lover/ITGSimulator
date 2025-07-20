using UnityEngine;

[RequireComponent(typeof(Interactable))] // An action should always have an Interactable
public class Action_PickupItem : MonoBehaviour, IInteractableAction
{
    [Header("Item Data")]
    public CreateInventoryItem item;
    [Tooltip("A UNIQUE ID for saving/loading.")]
    public string worldItemID;

    // --- ADDED: Cache a reference to our Interactable component ---
    private Interactable interactableComponent;

    void Awake()
    {
        // --- ADDED: Get the component in Awake ---
        interactableComponent = GetComponent<Interactable>();

        if (string.IsNullOrEmpty(worldItemID))
            Debug.LogError($"[{gameObject.name}] This item has no 'World Item ID'!", this);

        if (WorldDataManager.Instance != null && WorldDataManager.Instance.IsWorldItemPickedUp(worldItemID))
        {
            Destroy(gameObject);
        }
    }

    public void ExecuteAction()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(item);
            WorldDataManager.Instance?.MarkWorldItemAsPickedUp(worldItemID);
            Debug.Log($"Picked up [{item.itemName}] and destroyed the world object.");

            // ===================================================================
            // --- THIS IS THE FIX ---
            // ===================================================================
            // Before we destroy this object, we MUST check if it is still the
            // global target in PointAndClickMovement. If it is, we clear the reference.
            if (PointAndClickMovement.currentTarget == this.interactableComponent)
            {
                PointAndClickMovement.currentTarget = null;
            }
            // ===================================================================

            Destroy(gameObject); // Now it's safe to destroy the object.
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Inventory.Instance is null. Cannot pick up item.");
        }
    }

    public void ResetAction()
    {
        // No cleanup needed for a simple pickup.
    }
}