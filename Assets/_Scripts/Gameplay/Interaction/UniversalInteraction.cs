using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Universal Interaction")]
    public class UniversalInteraction : InteractionAction
    {
        [Header("Requirements")]
        public ItemTagDefinition requiredItemTag;
        public ReceptorTagDefinition requiredTargetTag;

        [Header("Consequences")]
        public StateDefinition targetState;
        public int setStateValue = 1;
        
        [Header("Personality Rewards")]
        [Tooltip("What traits does the player gain?")]
        public List<TraitShift> personalityRewards = new List<TraitShift>();

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // [Keep your existing validation logic here...]
            if (IsBlocked(target)) return null;
            if (requiredTargetTag != null && !target.HasReceptorTag(requiredTargetTag)) return null;
            if (requiredItemTag != null)
            {
                if (context.HeldItem == null) return null;
                if (!context.HeldItem.HasTag(requiredItemTag)) return null;
            }

            return new InteractionOption
            {
                Label = actionName,
                Priority = priority,
                ActionToRun = () => Execute(target)
            };
        }

        private void Execute(SmartObject target)
        {
            Debug.Log($"Performed {actionName} on {target.GetName()}");

            // 1. Change State
            if (targetState != null) target.SetState(targetState, setStateValue);

            // 2. Award Personality
            if (PersonalityManager.Instance != null)
            {
                foreach (var reward in personalityRewards)
                {
                    float finalAmount = reward.amount * target.interactionWeight;
                    PersonalityManager.Instance.ApplyShift(reward);
                }
            }
        }
    }
}