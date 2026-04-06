using System;

namespace Game.UI // Or Game.Core, depending on where you put it
{
    public static class InventoryHoverState
    {
        public static string HoveredItemID { get; private set; }
        public static string HoveredUniqueID { get; private set; }

        public static event Action OnChanged;

        public static void Set(string itemID, string uniqueID)
        {
            if (HoveredItemID == itemID && HoveredUniqueID == uniqueID) return;

            HoveredItemID = itemID;
            HoveredUniqueID = uniqueID;
            OnChanged?.Invoke();
        }

        public static void Clear()
        {
            if (HoveredItemID == null && HoveredUniqueID == null) return;

            HoveredItemID = null;
            HoveredUniqueID = null;
            OnChanged?.Invoke();
        }
    }
}