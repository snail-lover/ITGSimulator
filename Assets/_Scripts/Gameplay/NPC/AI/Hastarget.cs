using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Returns Success if TargetObject is set (NPCBrain found a goal). Returns Failure if not, allowing the Selector to fall through to wander.")]
    public class HasTarget : Conditional
    {
        [UnityEngine.Tooltip("Must match the SharedGameObject variable name used by NPCBrain: 'TargetObject'")]
        public SharedGameObject targetObject;

        public override TaskStatus OnUpdate()
        {
            return targetObject.Value != null ? TaskStatus.Success : TaskStatus.Failure;
        }
    }
}