using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Game.Core;

namespace Game.Gameplay
{
    [TaskCategory("Game")]
    [TaskDescription("Applies the recovery amount from the target interaction to the NPC's stats. Place after RunSmartObjectBehavior.")]
    public class SatisfyNeed : Action
    {
        //Must match the SharedGameObject variable name used by NPCBrain: 'TargetObject'")]
        public SharedGameObject targetObject;

        private NPCStats _stats;

        public override void OnStart()
        {
            _stats = GetComponent<NPCStats>();
        }

        public override TaskStatus OnUpdate()
        {
            if (_stats == null)
            {
                Debug.LogWarning("[SatisfyNeed] No NPCStats found on this NPC.");
                return TaskStatus.Failure;
            }

            if (targetObject.Value == null)
            {
                Debug.LogWarning("[SatisfyNeed] No target object set.");
                return TaskStatus.Failure;
            }

            SmartObject smartObj = targetObject.Value.GetComponent<SmartObject>();
            if (smartObj == null)
            {
                Debug.LogWarning("[SatisfyNeed] Target has no SmartObject component.");
                return TaskStatus.Failure;
            }

            // Find the matching interaction and apply its recovery
            foreach (var interaction in smartObj.GetAllAIInteractions())
            {
                if (interaction.satisfiesNeed == null) continue;

                var need = _stats.GetNeed(interaction.satisfiesNeed);
                if (need != null)
                {
                    float before = need.value;
                    _stats.ModifyNeed(interaction.satisfiesNeed, interaction.recoveryAmount);
                    Debug.Log($"[SatisfyNeed] '{interaction.satisfiesNeed.name}': {before} -> {need.value} (+{interaction.recoveryAmount})");
                    return TaskStatus.Success;
                }
            }

            Debug.LogWarning("[SatisfyNeed] No matching need found on this NPC for the target object's interactions.");
            return TaskStatus.Failure;
        }
    }
}