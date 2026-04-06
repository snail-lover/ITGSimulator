using System;
using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Gameplay
{
    public class Inventory : MonoBehaviour
    {
        public static Inventory Instance { get; private set; }
        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // --- Basic Operations ---

        // NEW: Method to add a complex item instance
        public void AddItemInstance(InventoryItemInstance itemInstance)
        {
            WorldDataManager.Instance.AddItemInstance(itemInstance);
            OnInventoryChanged?.Invoke();
        }

        // OLD: Wrapper for simple strings (backward compatibility)
        public void AddItem(string itemID)
        {
            WorldDataManager.Instance.AddItemToInventory(itemID);
            OnInventoryChanged?.Invoke();
        }

        public void RemoveItemByID(string itemID)
        {
            WorldDataManager.Instance.RemoveItemFromInventory(itemID);
            OnInventoryChanged?.Invoke();
        }

        public bool HasItem(string itemID) => WorldDataManager.Instance.PlayerHasItem(itemID);

        // --- TAG LOGIC (FIXED) ---

        public bool HasItemWithTag(ItemTagDefinition tagToCheck)
        {
            if (tagToCheck == null) return false;

            // FIX: Iterate through 'items' (List<InventoryItemInstance>) not 'itemIDs'
            var playerItems = WorldDataManager.Instance.saveData.playerInventory.items;

            foreach (var instance in playerItems)
            {
                ItemDefinition item = ItemDatabase.Instance.GetItemByID(instance.itemID);
                if (item != null && item.HasTag(tagToCheck))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetFirstItemIDWithTag(ItemTagDefinition tagToFind)
        {
            if (tagToFind == null) return null;

            var playerItems = WorldDataManager.Instance.saveData.playerInventory.items;

            foreach (var instance in playerItems)
            {
                ItemDefinition item = ItemDatabase.Instance.GetItemByID(instance.itemID);
                if (item != null && item.HasTag(tagToFind))
                {
                    return instance.itemID;
                }
            }
            return null;
        }
        
          public void SetStateOnHeldItem(StateDefinition stateDef, int value)
        {
            // 1. Priority A: Is something selected in the UI?
            string uniqueID = SelectedItemState.SelectedUniqueID;

            // 2. Priority B: If nothing selected, are we HOLDING something?
            if (string.IsNullOrEmpty(uniqueID))
            {
                var holder = FindFirstObjectByType<PlayerHoldController>();
                if (holder != null)
                {
                    var heldInstance = holder.GetHeldItemInstance();
                    if (heldInstance != null)
                    {
                        uniqueID = heldInstance.uniqueID;
                    }
                }
            }
            
            // 3. If still null, we fail.
            if (string.IsNullOrEmpty(uniqueID)) 
            {
                Debug.LogWarning("Cannot set state: No item selected and nothing in hand.");
                return;
            }

            // 4. Find and Update
            var instance = WorldDataManager.Instance.saveData.playerInventory.items
                .Find(x => x.uniqueID == uniqueID);

            if (instance != null)
            {
                instance.savedStates.RemoveAll(s => s.stateID == stateDef.name);
                instance.savedStates.Add(new InventoryItemInstance.SavedState
                {
                    stateID = stateDef.name,
                    value = value
                });

                Debug.Log($"Inventory: Set {stateDef.name}={value} on item");
                
                // IMPORTANT: If we modified the held item, we must update the VISUALS in the hand immediately
                // so the water appears instantly without needing to redrop the item.
                var holder = FindFirstObjectByType<PlayerHoldController>();
                if (holder != null && holder.GetHeldItemInstance() == instance)
                {
                    // Find the visual object in hand and update its SmartObject
                    SmartObject visualSmartObj = holder.holdSocket.GetComponentInChildren<SmartObject>();
                    if (visualSmartObj != null)
                    {
                        visualSmartObj.SetState(stateDef, value);
                    }
                }

                OnInventoryChanged?.Invoke();
            }
        }
          
        public void RemoveItemInstance(InventoryItemInstance instance)
        {
            WorldDataManager.Instance.saveData.playerInventory.items.Remove(instance);
            OnInventoryChanged?.Invoke();
        }
    }
}