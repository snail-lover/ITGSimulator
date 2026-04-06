using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Game.Core; // For StateDefinition

namespace Game.Gameplay
{
    [TaskCategory("Game")]
    [TaskDescription("Sets a specific state on a SmartObject to a specific value.")]
    public class SetSmartObjectState : Action
    {
        public SharedGameObject targetObject;
        
        public SharedObject targetStateDefinition; 
        
        public SharedInt targetValue;

        public override TaskStatus OnUpdate()
        {
            if (targetObject.Value == null || targetStateDefinition.Value == null)
            {
                Debug.LogWarning("SetSmartObjectState: Missing Object or State Definition.");
                return TaskStatus.Failure;
            }

            SmartObject smartObj = targetObject.Value.GetComponent<SmartObject>();
            if (smartObj == null)
            {
                return TaskStatus.Failure;
            }

            // Cast the raw object back to your StateDefinition
            StateDefinition stateDef = targetStateDefinition.Value as StateDefinition;
            if (stateDef == null)
            {
                Debug.LogError("SetSmartObjectState: Variable is not a StateDefinition!");
                return TaskStatus.Failure;
            }

            // --- EXECUTE ---
            // This triggers your StateRotator, SmartObjectReactor, etc.
            smartObj.SetState(stateDef, targetValue.Value);

            return TaskStatus.Success;
        }
    }
}