using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Ignite")]
    public class Action_Ignite : InteractionAction
    {
        [Header("Requirements")]
        public ItemTagDefinition requiredItemTag; // Drag 'Tag_Ignition' here
        public ReceptorTagDefinition requiredTargetTag; // Drag 'Receptor_Flammable' here

        [Header("Effects")]
        public StateDefinition burningState; // Drag 'State_Burning' here
        public string label = "Ignite";

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. Check if object is currently blocked (or already burning!)
            if (IsBlocked(target)) return null;

            // 2. Prevent igniting if it's already burning
            if (target.GetState(burningState) == 1) return null;

            // 3. Tag Check: Is the object flammable?
            if (!target.HasReceptorTag(requiredTargetTag)) return null;

            // 4. Item Check: Do we have a lighter?
            if (context.HeldItem == null || !context.HeldItem.HasTag(requiredItemTag)) 
                return null;

            return new InteractionOption
            {
                Label = label,
                Priority = 10, // High priority
                ActionToRun = () =>
                {
                    Debug.Log($"Ignited {target.GetName()}!");
                    target.SetState(burningState, 1);
                }
            };
        }
    }
}