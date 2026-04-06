using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Interactions/Action: Drag")]
    public class Action_Drag : InteractionAction
    {
        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // Can't drag if "Fixed" or "Heavy"? (Check blockedByStates)
            if (IsBlocked(target)) return null;

            // Find the Drag Controller on the Player
            var dragController = context.Source.GetComponent<PlayerDragController>();
            if (dragController == null) return null;

            // If we are ALREADY dragging THIS object -> Show "Stop Dragging"
            // (Requires DragController to expose what it's holding, or we just handle toggle in logic)
            if (dragController.IsDragging) 
            {
                return new InteractionOption
                {
                    Label = "Stop Dragging",
                    Priority = 10,
                    ActionToRun = () => dragController.StopDrag()
                };
            }

            // Otherwise -> "Drag"
            return new InteractionOption
            {
                Label = "Drag",
                Priority = 5, // Context menu priority
                ActionToRun = () => dragController.StartDrag(target.gameObject)
            };
        }
    }
}