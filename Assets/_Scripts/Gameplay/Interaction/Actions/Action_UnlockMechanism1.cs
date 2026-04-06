using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    // A single Action that works for ANY object with a LockMechanism
    [CreateAssetMenu(menuName = "Game/Actions/Generic Unlock")]
    public class Action_UnlockMechanism : InteractionAction
    {
        public StateDefinition lockedState; // Drag 'State_IsLocked' here

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. If it's not locked, we don't need to unlock it
            if (target.GetState(lockedState) == 0) return null;

            // 2. Check if the object HAS a lock mechanism
            var mechanism = target.GetComponent<LockMechanism>();
            if (mechanism == null) return null;

            // 3. Check if we have the specific key required by THIS generic mechanism
            string keyID = mechanism.requiredKeyID;
            if (string.IsNullOrEmpty(keyID)) return null;

            if (!Inventory.Instance.HasItem(keyID)) return null;

            // 4. Return the option
            string keyName = ItemDatabase.Instance.GetItemName(keyID);
            
            return new InteractionOption
            {
                Label = $"Unlock ({keyName})",
                Priority = 10,
                ActionToRun = () =>
                {
                    // We just tell the mechanism to unlock. 
                    // It handles the state changes and sounds.
                    mechanism.UnlockWithKey(); 
                }
            };
        }
    }
}