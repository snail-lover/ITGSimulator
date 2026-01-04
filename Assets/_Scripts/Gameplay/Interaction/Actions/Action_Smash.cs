using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Smash")]
    public class Action_Smash : InteractionAction
    {
        [Header("Requirements")]
        public ItemTagDefinition requiredTag;
        public StateDefinition brokenState;

        [Header("Results")]
        public string menuLabel = "Smash";

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. Check Hand
            if (string.IsNullOrEmpty(context.HeldItemID))
            {
                // This is normal for empty hands, so we don't log error here
                return null;
            }

            // 2. Check Database Lookup
            ItemDefinition heldItem = ItemDatabase.Instance.GetItemByID(context.HeldItemID);
            if (heldItem == null)
            {
                Debug.LogError($"[Action_Smash] CRITICAL: Player is holding ID '{context.HeldItemID}', but this ID was NOT found in ItemDatabase!");
                return null;
            }

            // 3. Check Tag Match
            // We print exactly what is happening here
            if (!heldItem.HasTag(requiredTag))
            {
                // Create a string list of tags the item DOES have
                string existingTags = "";
                foreach (var t in heldItem.tags) existingTags += t.name + ", ";

                Debug.LogWarning($"[Action_Smash] Failed Tag Check. \n" +
                    $"Item: {heldItem.itemName} \n" +
                    $"Required Tag: {requiredTag.name} \n" +
                    $"Item Has Tags: {existingTags}");

                return null;
            }

            // 4. Check Broken State
            int currentState = target.GetState(brokenState);
            if (currentState == 1)
            {
                // This is normal (don't show "Smash" if already smashed)
                return null;
            }

            // If we get here, it WORKS
            return new InteractionOption
            {
                Label = $"{menuLabel} with {heldItem.itemName}",
                Priority = this.priority,
                ActionToRun = () =>
                {
                    target.SetState(brokenState, 1);
                    Debug.Log($"<color=red>CRASH!</color> Smashed {target.GetName()}!");
                    var renderer = target.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = Color.black;
                }
            };
        }
    }
}
