// InventoryState.cs
using System.Collections.Generic;

[System.Serializable]
public class InventoryState
{
    public List<string> itemIDs = new List<string>();
    // You can add more later, like equippedItem, etc.
}