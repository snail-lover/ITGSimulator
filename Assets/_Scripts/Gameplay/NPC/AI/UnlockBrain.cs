using BehaviorDesigner.Runtime.Tasks;

namespace Game.Gameplay
{
    [TaskCategory("Game")]
    [TaskDescription("Unlocks the NPCBrain so it can evaluate new goals. Place at the END of an interaction sequence AND in any failure branch.")]
    public class UnlockBrain : Action
    {
        public override TaskStatus OnUpdate()
        {
            GetComponent<NPCBrain>()?.SetBusy(false);
            return TaskStatus.Success;
        }
    }
}