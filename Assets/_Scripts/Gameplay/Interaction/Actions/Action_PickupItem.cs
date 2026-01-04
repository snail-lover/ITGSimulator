using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Pickup Item")]
    public class Action_PickupItem : InteractionAction
    {
        // We override the name in the code logic to include the item name dynamically
        // e.g. "Pick Up Camera" instead of just "Pick Up"

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. Check if this SmartObject is actually an Item
            ItemInstance itemComponent = target.GetComponent<ItemInstance>();

            if (itemComponent == null || itemComponent.itemData == null)
            {
                // It's not an item, so we can't pick it up.
                return null;
            }

            // 2. Return the Pickup Option
            return new InteractionOption
            {
                // Dynamic Label: "Pick Up [Item Name]"
                Label = $"Pick Up {itemComponent.itemData.itemName}",

                // High priority (10) so it's usually the default Left-Click action
                Priority = 10,

                ActionToRun = () =>
                {
                    PickupLogic(itemComponent);
                }
            };
        }

        private void PickupLogic(ItemInstance item)
        {
            if (Inventory.Instance != null)
            {
                // Add to Inventory Logic
                Inventory.Instance.AddItem(item.itemData.id);
                Debug.Log($"<color=green>[Action]</color> Picked up: {item.itemData.itemName}");

                // Destroy the physical object
                Destroy(item.gameObject);
            }
            else
            {
                Debug.LogError("Inventory Instance is missing!");
            }
        }
    }
}