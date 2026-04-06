using UnityEngine;
using System.Collections.Generic;
using Game.Core; // For InteractionOption
using Game.Gameplay;
using TMPro;

namespace Game.UI
{
    public class ContextMenuManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject menuPanel; 
        public Transform buttonContainer; 
        public ContextMenuButton buttonPrefab; 
        
        [Header("New Header")]
        public TMP_Text headerText; // <--- Drag your new Text object here

        [Header("Warning UI")]
        public TMP_Text warningText; 

        [Header("Settings")]
        public Vector2 offset = new Vector2(10f, -10f); 

        private List<ContextMenuButton> _spawnedButtons = new List<ContextMenuButton>();
        private float _timeOpened; 

        void Awake()
        {
            menuPanel.SetActive(false);
            if(warningText) warningText.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            UIEvents.OnRequestContextMenu += OpenMenu;
            InteractionController.OnClearContextMenu += CloseMenu;
            UIEvents.OnForceCloseContextMenu += CloseMenu;
            InteractionController.OnRangeWarningChanged += UpdateWarningState;
        }

        void OnDisable()
        {
            UIEvents.OnRequestContextMenu -= OpenMenu;
            InteractionController.OnClearContextMenu -= CloseMenu;
            UIEvents.OnForceCloseContextMenu -= CloseMenu;
            InteractionController.OnRangeWarningChanged -= UpdateWarningState;
        }

        void Update()
        {
            if (menuPanel.activeSelf)
            {
                if (Time.time <= _timeOpened + 0.1f) return; 

                // Simple mouse-off check
                RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
                if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, null))
                {
                    CloseMenu();
                }
            }
        }

        // --- UPDATED SIGNATURE ---
        private void OpenMenu(List<InteractionOption> options, Vector2 screenPosition, string objectName)
        {
            // 1. Set the Header Text
            if (headerText != null) 
            {
                headerText.text = $"Selection: {objectName}";
            }

            // 2. Reset UI
            if(warningText) warningText.gameObject.SetActive(false); 
            
            foreach (var btn in _spawnedButtons) Destroy(btn.gameObject);
            _spawnedButtons.Clear();

            foreach (var option in options)
            {
                ContextMenuButton newBtn = Instantiate(buttonPrefab, buttonContainer);
                newBtn.Setup(option, CloseMenu);
                _spawnedButtons.Add(newBtn);
            }

            MoveToPosition(screenPosition);
            menuPanel.SetActive(true);
            _timeOpened = Time.time;
        }

        // ... (Keep UpdateWarningState, CloseMenu, MoveToPosition exactly as they were) ...
        
        private void UpdateWarningState(bool isOutOfRange)
        {
            if (!menuPanel.activeSelf) return;

            // 1. Update Text
            if (warningText != null)
            {
                warningText.gameObject.SetActive(isOutOfRange);
                warningText.text = "Too far away!";
            }

            // 2. Update Buttons (FIXED)
            foreach (var btn in _spawnedButtons)
            {
                // We call our custom method instead of accessing the Button component directly
                // !isOutOfRange means: if out of range is FALSE, then interactable is TRUE.
                btn.SetInteractable(!isOutOfRange);
            }
        }

        public void CloseMenu()
        {
            if (!menuPanel.activeSelf) return;
            if (warningText != null) warningText.gameObject.SetActive(false);
            menuPanel.SetActive(false);
            InteractionController.Instance.ClearTracking();
        }

        private void MoveToPosition(Vector2 screenPos)
        {
            RectTransform rect = menuPanel.GetComponent<RectTransform>();
            rect.position = screenPos + offset;
        }
    }
}