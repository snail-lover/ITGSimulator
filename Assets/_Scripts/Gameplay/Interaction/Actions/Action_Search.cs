using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Search")]
    public class Action_Search : InteractionAction
    {
        [Header("Configuration")]
        public StateDefinition stateEmpty; // Drag 'State_Empty' here
        public string labelReady = "Search";
        public string labelEmpty = "Search again";

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            if (IsBlocked(target)) return null;

            // Optional: You could hide the button entirely if it's empty, 
            // but you asked to be able to "search again and find nothing".
            bool isEmpty = target.GetState(stateEmpty) == 1;

            return new InteractionOption
            {
                Label = isEmpty ? labelEmpty : labelReady,
                Priority = 5,
                ActionToRun = () => ExecuteSearch(target)
            };
        }

        private void ExecuteSearch(SmartObject target)
        {
            // 1. Check if already searched/empty
            if (target.GetState(stateEmpty) == 1)
            {
                Debug.Log("You searched again, but found nothing.");
                // Optional: Play a "hollow" sound or show UI notification
                return;
            }

            // 2. Play Sound (if any)
            var lootSource = target.GetComponent<LootSource>();
            if (lootSource != null && lootSource.searchSound != null)
            {
                AudioSource.PlayClipAtPoint(lootSource.searchSound, target.transform.position);
            }

            // 3. Give Item (if any)
            if (lootSource != null && !string.IsNullOrEmpty(lootSource.itemIDToGive))
            {
                string itemID = lootSource.TakeItem();
                Inventory.Instance.AddItem(itemID);
                
                string itemName = ItemDatabase.Instance.GetItemName(itemID);
                Debug.Log($"<color=green>Found item: {itemName}!</color>");
                // TODO: Trigger your UI Notification here (e.g. "Acquired Key!")
            }
            else
            {
                Debug.Log("You searched, but found nothing useful.");
            }

            // 4. Set State to Empty
            // This ensures next time we click, we get the "Empty" behavior
            target.SetState(stateEmpty, 1);
        }
    }
}