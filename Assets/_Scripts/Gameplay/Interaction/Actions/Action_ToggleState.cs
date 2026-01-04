using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Toggle State")]
    public class Action_ToggleState : InteractionAction
    {
        [Header("Configuration")]
        public StateDefinition stateToToggle;
        public string labelOn = "Turn Off";
        public string labelOff = "Turn On";

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. GLOBAL CHECK (The new one-liner)
            if (IsBlocked(target)) return null;

            // 2. Normal Logic
            int val = target.GetState(stateToToggle);
            bool isOn = val == 1;

            return new InteractionOption
            {
                Label = isOn ? labelOn : labelOff,
                Priority = this.priority,
                ActionToRun = () =>
                {
                    target.SetState(stateToToggle, isOn ? 0 : 1);
                }
            };
        }
    }
}