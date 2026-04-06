using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay
{
    public class NPCStats : MonoBehaviour
    {
        [System.Serializable]
        public class RuntimeNeed
        {
            public NeedDefinition definition;
            [Range(0, 100)] public float value = 100f; // 100 = Full/Happy, 0 = Empty/Critical
            
            [Tooltip("How urgently do I need this based on current value? (X: Value 0-100, Y: Urgency 0-1)")]
            public AnimationCurve urgencyCurve = AnimationCurve.Linear(0, 1, 1, 0); // Default: Low value = High urgency

            public float GetUrgency()
            {
                // Normalize value (0-100) to (0-1) for the curve
                return urgencyCurve.Evaluate(value / 100f);
            }
        }

        [Header("Configuration")]
        public List<RuntimeNeed> needs = new List<RuntimeNeed>();

        void Update()
        {
            // Decay needs over time
            foreach (var need in needs)
            {
                if (need.definition != null)
                {
                    need.value -= need.definition.decayRate * Time.deltaTime;
                    need.value = Mathf.Clamp(need.value, 0, 100);
                }
            }
        }

        public RuntimeNeed GetNeed(NeedDefinition def)
        {
            return needs.Find(n => n.definition == def);
        }

        public void ModifyNeed(NeedDefinition def, float amount)
        {
            var need = GetNeed(def);
            if (need != null)
            {
                need.value = Mathf.Clamp(need.value + amount, 0, 100);
            }
        }
    }
}