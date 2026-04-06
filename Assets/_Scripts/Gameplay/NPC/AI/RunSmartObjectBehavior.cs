using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Runs an External Behavior Tree stored in a variable.")]
    public class RunSmartObjectBehavior : BehaviorReference
    {
        public SharedObject targetBehaviorTree;

        public override ExternalBehavior[] GetExternalBehaviors()
        {
            // SAFETY CHECK: If the variable is empty (which it is at Start), return an empty list.
            // Do NOT return null.
            if (targetBehaviorTree == null || targetBehaviorTree.Value == null)
            {
                return new ExternalBehavior[0];
            }

            // If we have a tree, wrap it in an array and return it
            if (targetBehaviorTree.Value is ExternalBehavior tree)
            {
                return new ExternalBehavior[] { tree };
            }

            return new ExternalBehavior[0];
        }
    }
}