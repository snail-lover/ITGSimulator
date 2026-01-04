using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Inspect")]
    public class Action_Inspect : InteractionAction
    {
        // Define a generic event that UI can listen to
        public static event System.Action<string> OnInspectTriggered;

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // Always available (unless you want to require line of sight or light level)
            return new InteractionOption
            {
                Label = "Inspect",
                Priority = 5, // Lower priority than "Open" or "Fix"
                ActionToRun = () =>
                {
                    string text = target.GetDescription();

                    Debug.Log($"[Inspect] {text}");

                    // Fire event for UI to pick up
                    OnInspectTriggered?.Invoke(text);
                }
            };
        }
    }
}