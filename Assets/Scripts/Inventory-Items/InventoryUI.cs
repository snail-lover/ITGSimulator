// InventoryUI.cs (Refactored for Drag & Drop)
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Prefabs")]
    [Tooltip("Drag the InventoryPanel PREFAB here from your Project folder.")]
    public GameObject inventoryPanelPrefab;

    [Header("Runtime References")]
    [Tooltip("The parent Canvas for the UI. If null, will try to find it by tag.")]
    public Canvas mainCanvas;

    private GameObject currentPanelInstance;

    // --- NEW: Subscribe to drag events ---
    private void OnEnable()
    {
        Draggable.OnDragStarted += OpenInventoryPanel;
        Draggable.OnDragEnded += CloseInventoryPanel;
    }

    // --- NEW: Unsubscribe to prevent memory leaks ---
    private void OnDisable()
    {
        Draggable.OnDragStarted -= OpenInventoryPanel;
        Draggable.OnDragEnded -= CloseInventoryPanel;
    }
    private void OpenInventoryPanel(Draggable draggedItem)
    {
        // Your logic to make the inventory panel appear.
        Debug.Log($"Drag of '{draggedItem.name}' started, opening inventory panel.");
    }

    // Same fix for this method.
    private void CloseInventoryPanel(Draggable draggedItem)
    {
        // Your logic to make the inventory panel disappear.
        Debug.Log($"Drag of '{draggedItem.name}' ended, closing inventory panel.");
    }

    void Start()
    {
        if (inventoryPanelPrefab == null)
        {
            Debug.LogError("InventoryPanelPrefab is not assigned in the Inspector!", this);
            enabled = false;
            return;
        }

        if (mainCanvas == null)
        {
            GameObject canvasObj = GameObject.FindGameObjectWithTag("MainCanvas");
            if (canvasObj != null) mainCanvas = canvasObj.GetComponent<Canvas>();
            else
            {
                Debug.LogError("Could not find a Canvas tagged 'MainCanvas'. Please assign it in the Inspector.", this);
                enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventoryPanel();
        }
    }

    // --- MODIFIED: Split Toggle into Open/Close methods ---

    public void OpenInventoryPanel()
    {
        // Only create the panel if it doesn't already exist
        if (currentPanelInstance == null)
        {
            if (inventoryPanelPrefab != null && mainCanvas != null)
            {
                currentPanelInstance = Instantiate(inventoryPanelPrefab, mainCanvas.transform);
                Debug.Log("Inventory panel instantiated.");
            }
        }
    }

    public void CloseInventoryPanel()
    {
        // Only destroy the panel if it exists
        if (currentPanelInstance != null)
        {
            Destroy(currentPanelInstance);
            currentPanelInstance = null; // Clear the reference
            Debug.Log("Inventory panel closed and destroyed.");
        }
    }

    public void ToggleInventoryPanel()
    {
        // The toggle now just calls the appropriate open/close method
        if (currentPanelInstance != null)
        {
            CloseInventoryPanel();
        }
        else
        {
            OpenInventoryPanel();
        }
    }
}