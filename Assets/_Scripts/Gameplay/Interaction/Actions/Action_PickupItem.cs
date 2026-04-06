using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Pickup Item")]
    public class Action_PickupItem : InteractionAction
    {
        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            ItemInstance itemComponent = target.GetComponent<ItemInstance>();

            if (itemComponent == null || itemComponent.itemData == null) return null;

            return new InteractionOption
            {
                Label = $"Pick Up {itemComponent.itemData.itemName}",
                Priority = 10,
                ActionToRun = () =>
                {
                    PickupLogic(target, itemComponent);
                }
            };
        }

        private void PickupLogic(SmartObject smartObj, ItemInstance item)
        {
            if (Inventory.Instance != null)
            {
                // 1. Create the new container
                var newItemData = new InventoryItemInstance
                {
                    itemID = item.itemData.id,
                    uniqueID = System.Guid.NewGuid().ToString()
                };

                // 2. SAVE THE STATE! (This preserves "Full", "Hot", etc.)
                newItemData.savedStates = smartObj.ExportStates();

                // 3. Add to Inventory
                Inventory.Instance.AddItemInstance(newItemData);

                Debug.Log($"Picked up {item.itemData.itemName}. Saved {newItemData.savedStates.Count} states.");
                Destroy(item.gameObject);
            }
        }
    }
}