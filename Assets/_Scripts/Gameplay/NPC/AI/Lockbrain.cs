using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay
{
    [TaskCategory("Game")]
    [TaskDescription("Locks the NPCBrain so it stops re-evaluating goals. Place at the START of an interaction sequence.")]
    public class LockBrain : Action
    {
        public override TaskStatus OnUpdate()
        {
            GetComponent<NPCBrain>()?.SetBusy(true);
            return TaskStatus.Success;
        }
    }
}