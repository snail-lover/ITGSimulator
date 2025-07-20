using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragDropController : MonoBehaviour
{
    public static UIDragDropController Instance { get; private set; }

    [Header("Setup")]
    [Tooltip("A UI Image prefab that will be used as the ghost icon while dragging.")]
    public GameObject ghostIconPrefab;

    [Header("Interaction")]
    [Tooltip("The maximum distance from the player that an item can be dropped.")]
    public float maxDropDistance = 3f;

    // --- Runtime Data ---
    private GameObject currentGhostIcon;
    private CreateInventoryItem currentlyDraggedItem;
    private InventorySlotUI originalSlot;
    private Transform playerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }
    }

    void Start()
    {
        // Find the player transform for distance checks
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public void StartDrag(CreateInventoryItem item, InventorySlotUI slot)
    {
        if (ghostIconPrefab == null) return;

        currentlyDraggedItem = item;
        originalSlot = slot;

        // Create and setup the ghost icon
        currentGhostIcon = Instantiate(ghostIconPrefab, transform); // 'transform' should be the main Canvas
        currentGhostIcon.GetComponent<UnityEngine.UI.Image>().sprite = item.icon;

        // Hide the original item in the slot
        originalSlot.SetIconVisibility(false);
    }

    public void UpdateDrag()
    {
        if (currentGhostIcon != null)
        {
            // Make the ghost icon follow the mouse
            currentGhostIcon.GetComponent<RectTransform>().position = Input.mousePosition;
        }
    }

    public void EndDrag()
    {
        if (currentlyDraggedItem == null) return;

        // Check if the pointer is over a UI element (like the inventory panel itself)
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Drop failed: Dropped on top of UI.");
            CancelDrag();
            return;
        }

        // Raycast from the camera to the mouse position to find a point in the 3D world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (playerTransform == null)
            {
                Debug.LogError("Drop failed: Player transform not found!");
                CancelDrag();
                return;
            }

            // Check if the drop location is within range of the player
            float distanceToPlayer = Vector3.Distance(hit.point, playerTransform.position);
            if (distanceToPlayer <= maxDropDistance)
            {
                // Drop was successful!
                PerformSuccessfulDrop(hit.point);
            }
            else
            {
                Debug.Log("Drop failed: Too far from player.");
                CancelDrag();
            }
        }
        else
        {
            Debug.Log("Drop failed: No valid drop surface found.");
            CancelDrag();
        }
    }

    private void PerformSuccessfulDrop(Vector3 dropPosition)
    {
        Debug.Log($"Successfully dropped {currentlyDraggedItem.itemName} at {dropPosition}");

        // 1. Instantiate the item's 3D prefab in the world
        // We add a slight vertical offset to prevent it from spawning inside the floor
        Instantiate(currentlyDraggedItem.worldPrefab, dropPosition + new Vector3(0, 0.1f, 0), Quaternion.identity);

        // 2. Remove the item from the inventory data
        Inventory.Instance.RemoveItemByID(currentlyDraggedItem.id);

        // 3. Clean up the ghost icon
        Destroy(currentGhostIcon);
        currentlyDraggedItem = null;
        originalSlot = null;
        // The OnInventoryChanged event will handle redrawing the UI, so we don't need to touch the originalSlot.
    }

    private void CancelDrag()
    {
        if (originalSlot != null)
        {
            // Make the original icon visible again
            originalSlot.SetIconVisibility(true);
        }

        // Clean up
        Destroy(currentGhostIcon);
        currentlyDraggedItem = null;
        originalSlot = null;
    }
}