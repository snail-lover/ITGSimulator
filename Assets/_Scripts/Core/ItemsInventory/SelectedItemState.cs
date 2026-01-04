// Game.Core
namespace Game.Core
{
    using System;

    public static class SelectedItemState
    {
        public static string SelectedItemID { get; private set; }
        public static event Action<string> OnChanged;

        public static void Set(string itemID)
        {
            if (SelectedItemID == itemID) return;
            SelectedItemID = itemID;
            OnChanged?.Invoke(SelectedItemID);
        }

        public static void Clear() => Set(null);
    }
}
