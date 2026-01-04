using System;
using UnityEngine;
using Game.Core;

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

        // --- Basic Operations (Pass-through to WorldData) ---

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

        // --- TAG LOGIC (Updated) ---

        public bool HasItemWithTag(ItemTagDefinition tagToCheck)
        {
            if (tagToCheck == null) return false;

            var playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

            foreach (string itemID in playerItemIDs)
            {
                ItemDefinition item = ItemDatabase.Instance.GetItemByID(itemID);
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

            var playerItemIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;

            foreach (string itemID in playerItemIDs)
            {
                ItemDefinition item = ItemDatabase.Instance.GetItemByID(itemID);
                if (item != null && item.HasTag(tagToFind))
                {
                    return itemID;
                }
            }
            return null;
        }
    }
}