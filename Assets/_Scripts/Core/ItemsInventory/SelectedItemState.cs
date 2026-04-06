using System;

namespace Game.Core
{
    public static class SelectedItemState
    {
        public static string SelectedItemID { get; private set; }
        public static string SelectedUniqueID { get; private set; } // <--- NEW

        public static event Action OnChanged; // Removed string arg to force pulling from properties

        public static void Set(string itemID, string uniqueID)
        {
            // Only update if something changed
            if (SelectedUniqueID == uniqueID) return;

            SelectedItemID = itemID;
            SelectedUniqueID = uniqueID;
            
            OnChanged?.Invoke();
        }

        public static void Clear()
        {
            SelectedItemID = null;
            SelectedUniqueID = null;
            OnChanged?.Invoke();
        }
    }
}