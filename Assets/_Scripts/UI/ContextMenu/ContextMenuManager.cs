using UnityEngine;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay; // Needed to listen to InteractionController

namespace Game.UI
{
    public class ContextMenuManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject menuPanel; // The panel containing the layout group
        public Transform buttonContainer; // Parent object for the buttons
        public ContextMenuButton buttonPrefab; // The button prefab we made in Step 2

        [Header("Settings")]
        public Vector2 offset = new Vector2(10f, -10f); // Slight offset from cursor
        public bool keepInScreenBounds = true;

        private List<ContextMenuButton> _spawnedButtons = new List<ContextMenuButton>();

        void Awake()
        {
            // Hide on start
            menuPanel.SetActive(false);

        }

        void OnEnable()
        {
            InteractionController.OnOpenContextMenu += OpenMenu;
            InteractionController.OnClearContextMenu += CloseMenu;
        }

        void OnDisable()
        {
            InteractionController.OnOpenContextMenu -= OpenMenu;
            InteractionController.OnClearContextMenu -= CloseMenu;
        }

        void Update()
        {
            // Only check if menu is actually open
            if (menuPanel.activeSelf)
            {
                // Check for Left (0) or Right (1) click
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    // Get the RectTransform of the visible panel
                    RectTransform panelRect = menuPanel.GetComponent<RectTransform>();

                    // Check: Is the mouse position INSIDE the panel?
                    // "null" for camera works if your Canvas is set to "Screen Space - Overlay"
                    bool isMouseOverMenu = RectTransformUtility.RectangleContainsScreenPoint(
                        panelRect,
                        Input.mousePosition,
                        null
                    );

                    // If we clicked OUTSIDE the menu, close it
                    if (!isMouseOverMenu)
                    {
                        CloseMenu();
                    }
                }
            }
        }
        // --- LOGIC ---

        private void OpenMenu(List<InteractionOption> options, Vector2 screenPosition)
        {
            // 1. Clean up old buttons
            foreach (var btn in _spawnedButtons) Destroy(btn.gameObject);
            _spawnedButtons.Clear();

            // 2. Spawn new buttons
            foreach (var option in options)
            {
                ContextMenuButton newBtn = Instantiate(buttonPrefab, buttonContainer);
                newBtn.Setup(option, CloseMenu);
                _spawnedButtons.Add(newBtn);
            }

            // 3. Position the menu
            MoveToPosition(screenPosition);

            // 4. Show
            menuPanel.SetActive(true);
        }

        public void CloseMenu()
        {
            // If it's already closed, do nothing
            if (!menuPanel.activeSelf) return;

            menuPanel.SetActive(false);

            // Optional: Tell controller we closed it manually (so it stops calculating distance)
            InteractionController.Instance.ClearTracking();
        }
        private void MoveToPosition(Vector2 screenPos)
        {
            RectTransform rect = menuPanel.GetComponent<RectTransform>();

            // Set position
            rect.position = screenPos + offset;

            // Optional: Keep inside screen bounds logic could go here
            // checking Screen.width vs rect.rect.width
        }

        // Helper to close menu if clicking empty space (Call this from a giant invisible background button)
        public void OnBackgroundClick()
        {
            CloseMenu();
        }
    }
}