using UnityEngine;

namespace Game.UI
{
    public class InventoryDisplayManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Drag the InventoryPanel Prefab here")]
        public GameObject inventoryPrefab;

        [Tooltip("The Canvas where the UI will be spawned")]
        public Transform uiCanvas;

        private GameObject _activeInventoryInstance;

        void Update()
        {
            // Toggle with 'I'
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }

            // Optional: Close with Escape if open
            if (Input.GetKeyDown(KeyCode.Escape) && _activeInventoryInstance != null)
            {
                CloseInventory();
            }
        }

        public void ToggleInventory()
        {
            if (_activeInventoryInstance == null)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }

        private void OpenInventory()
        {
            if (_activeInventoryInstance != null) return;

            // Just spawn the UI. 
            // We do NOT lock input, so you can still walk (WASD) while this is open.
            _activeInventoryInstance = Instantiate(inventoryPrefab, uiCanvas);
        }

        private void CloseInventory()
        {
            if (_activeInventoryInstance != null)
            {
                Destroy(_activeInventoryInstance);
                _activeInventoryInstance = null;
            }
        }
    }
}