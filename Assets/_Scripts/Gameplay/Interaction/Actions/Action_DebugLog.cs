using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    // This adds the entry to the Right-Click menu in the Project View
    [CreateAssetMenu(menuName = "Game/Actions/Debug Log")]
    public class Action_DebugLog : InteractionAction
    {
        [TextArea] public string messageToPrint = "Interaction Test Successful!";

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // For this test, we don't care about states or items.
            // We ALWAYS return a valid option.

            return new InteractionOption
            {
                Label = this.actionName, // Takes the name you type in the Inspector
                Priority = this.priority,
                ActionToRun = () =>
                {
                    // The actual logic that runs when clicked
                    Debug.Log($"<color=magenta>[Interaction Test]</color> {target.GetName()} says: {messageToPrint}");
                }
            };
        }
    }
}