using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Actions/Fill From Source")]
    public class Action_FillFromSource : InteractionAction
    {
        [Header("Requirements")]
        public ReceptorTagDefinition sourceTag;   // Drag "Fluid_Source" here
        public ItemTagDefinition containerTag;    // Drag "Fluid_Container" here

        [Header("Inventory State Keys")]
        // These are the State Definitions we will modify on the ITEM
        public StateDefinition stateFillLevel;    // 0 = Empty, 1 = Full
        public StateDefinition stateFluidID;      // Stores the ID (10=Water, etc.)

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            // 1. Validation: Is the target a Fluid Source?
            if (!target.HasReceptorTag(sourceTag)) return null;

            // 2. Validation: Are we holding a Container?
            if (context.HeldItem == null) return null;
            if (!context.HeldItem.HasTag(containerTag)) return null;

            // 3. Validation: Get the specific Fluid Data from the target
            var sourceComp = target.GetComponent<FluidSource>();
            if (sourceComp == null || sourceComp.fluidType == null) return null;

            // 4. (Optional) Check if Item is already full
            // (Requires checking Inventory Instance state, skipped for brevity but recommended)

            return new InteractionOption
            {
                Label = $"Fill with {sourceComp.fluidType.fluidName}",
                Priority = 10,
                ActionToRun = () =>
                {
                    FillLogic(context, sourceComp.fluidType);
                }
            };
        }

        private void FillLogic(InteractionContext context, FluidDefinition fluid)
        {
            if (Inventory.Instance == null) return;

            // Update the state of the CURRENTLY HELD item
            // We need to add this helper method to Inventory.cs (see below)
            Inventory.Instance.SetStateOnHeldItem(stateFillLevel, 1);
            Inventory.Instance.SetStateOnHeldItem(stateFluidID, fluid.fluidID);

            Debug.Log($"Filled {context.HeldItem.itemName} with {fluid.fluidName}");
        }
    }
}