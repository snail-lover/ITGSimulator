using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Clears the target object variables.")]
    public class ResetInteraction : Action
    {
        public SharedGameObject targetObject;
        public SharedObject targetBehaviorTree; // Changed to generic SharedObject

        public override TaskStatus OnUpdate()
        {
            // Clear the variables
            targetObject.Value = null;
            targetBehaviorTree.Value = null;

            return TaskStatus.Success;
        }
    }
}