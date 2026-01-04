using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming you use TextMeshPro
using Game.Core;
using System;

namespace Game.UI
{
    public class ContextMenuButton : MonoBehaviour
    {
        [Header("Components")]
        public Button button;
        public TextMeshProUGUI labelText;
        public Image iconImage; // Optional

        private Action _callback;

        public void Setup(InteractionOption option, Action closeMenuCallback)
        {
            // Set Text
            labelText.text = option.Label;

            // Set Icon (if available)
            if (option.Icon != null)
            {
                iconImage.sprite = option.Icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            // Setup Click Event
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 1. Run the actual game logic (e.g., Break Door)
                option.ActionToRun?.Invoke();

                // 2. Tell the menu to close itself
                closeMenuCallback?.Invoke();
            });
        }
    }
}