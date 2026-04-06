using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Study")]
    public class Action_Study : InteractionAction
    {
        [Header("Study Settings")]
        public float amount = 1.0f;
        public string label = "Study";

        [Header("Constraints")]
        [Tooltip("If set, this state will be set to 1 after studying. If it's already 1, the action hides.")]
        public StateDefinition oneTimeFlag; 

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. Check Global Blockers
            if (IsBlocked(target)) return null;

            // 2. Check One-Time Logic
            // If we have a flag, and that flag is ALREADY 1, don't show the option.
            if (oneTimeFlag != null && target.GetState(oneTimeFlag) == 1)
            {
                return null; 
            }

            return new InteractionOption
            {
                Label = label,
                Priority = priority,
                ActionToRun = () => PerformStudy(target)
            };
        }

        private void PerformStudy(SmartObject target)
        {
            // 1. Apply Personality Shift
            if (PersonalityManager.Instance != null)
            {
                // Note: We use target.interactionWeight to scale the reward. 
                // Studying a "Heavy Book" (Weight 5) gives more than a "Napkin" (Weight 1).
                float finalAmount = amount * target.interactionWeight;
                
                PersonalityManager.Instance.ModifyTrait(TraitType.Academic, finalAmount);
                
                Debug.Log($"<color=cyan>[Study]</color> Gained {finalAmount} Academic from {target.GetName()}");
            }

            // 2. Mark as read (if using the one-time flag)
            if (oneTimeFlag != null)
            {
                target.SetState(oneTimeFlag, 1);
            }

            // 3. Optional: Trigger your Notification UI here
            // UIEvents.ShowNotification($"You learned something from the {target.GetName()}.");
        }
    }
}