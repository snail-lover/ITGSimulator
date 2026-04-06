using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Game.Core;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Returns Success if the State equals the Value.")]
    public class CheckSmartObjectState : Conditional
    {
        public SharedGameObject targetObject;
        public SharedObject stateToCheck;
        public int valueEquals = 1;

        public override TaskStatus OnUpdate()
        {
            if (targetObject.Value == null) return TaskStatus.Failure;
            
            SmartObject smartObj = targetObject.Value.GetComponent<SmartObject>();
            StateDefinition stateDef = stateToCheck.Value as StateDefinition;

            if (smartObj != null && stateDef != null)
            {
                // Return Success if values match, Failure if they don't
                return smartObj.GetState(stateDef) == valueEquals ? TaskStatus.Success : TaskStatus.Failure;
            }
            return TaskStatus.Failure;
        }
    }
}