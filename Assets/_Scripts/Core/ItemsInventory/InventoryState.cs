using System;
using System.Collections.Generic;
using Game.Core;

[Serializable]
public class InventoryItemInstance
{
    public string itemID;      // "mug"
    public string uniqueID;    // GUID (to distinguish between two different mugs)
    
    // We store the saved state here (e.g., FillLevel=1, FluidType=2)
    public List<SavedState> savedStates = new List<SavedState>();

    [Serializable]
    public struct SavedState
    {
        public string stateID; // Using string ID of StateDefinition for serialization safety
        public int value;
    }
}

[Serializable]
public class InventoryState
{
    // Changed from List<string> to List<InventoryItemInstance>
    public List<InventoryItemInstance> items = new List<InventoryItemInstance>();
}