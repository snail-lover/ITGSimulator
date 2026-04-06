using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using Game.Core; 
using System;
using UnityEngine.EventSystems;

namespace Game.UI
{
    public class ContextMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Components")]
        public Button button;
        public TextMeshProUGUI labelText;
        public Image iconImage; 

        [Header("Style Settings")]
        public Color normalColor = Color.white;
        public Color hoverColor = new Color(1f, 0.8f, 0f); // Gold/Yellow
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grey/Transparent
        
        private Action _callback;

        public void Setup(InteractionOption option, Action closeMenuCallback)
        {
            labelText.text = option.Label;
            
            // Default to normal state
            labelText.color = normalColor;
            button.interactable = true;

            if (option.Icon != null)
            {
                iconImage.sprite = option.Icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                option.ActionToRun?.Invoke();
                closeMenuCallback?.Invoke();
            });
        }

        // --- NEW: Handle Disabled State ---
        public void SetInteractable(bool state)
        {
            button.interactable = state;

            // Immediate Visual Update
            if (state)
            {
                labelText.color = normalColor;
            }
            else
            {
                labelText.color = disabledColor;
            }
        }

        // --- HOVER LOGIC ---
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable)
                labelText.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (button.interactable)
                labelText.color = normalColor;
        }
    }
}