using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay.AI.Tasks
{
    [TaskCategory("Game")]
    [TaskDescription("Returns Failure while the NPC is in dialogue, blocking the entire tree. Place at the very top of the root Sequence.")]
    public class CheckNotInDialogue : Conditional
    {
        private NPCBrain _brain;

        public override void OnStart()
        {
            _brain = GetComponent<NPCBrain>();
        }

        public override TaskStatus OnUpdate()
        {
            if (_brain == null) return TaskStatus.Success;
            return _brain.IsInDialogue ? TaskStatus.Failure : TaskStatus.Success;
        }
    }
}