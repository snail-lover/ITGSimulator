using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public static class UIEvents
    {
        // Added 'string objectName' to the event signature
        public static event Action<List<InteractionOption>, Vector2, string> OnRequestContextMenu;

        public static event Action OnForceCloseContextMenu;
        
        public static void RequestContextMenu(List<InteractionOption> options, Vector2 screenPos, string objectName)
        {
            OnRequestContextMenu?.Invoke(options, screenPos, objectName);
        }
        
        public static void ForceCloseContextMenu() => OnForceCloseContextMenu?.Invoke();
    }
}