using UnityEngine;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using Game.Core;

namespace Game.Gameplay
{
    [RequireComponent(typeof(NPCStats))]
    [RequireComponent(typeof(BehaviorTree))]
    public class NPCBrain : MonoBehaviour
    {
        [Header("Sensors")]
        public float perceptionRadius = 20f;
        public LayerMask smartObjectLayer;

        [Header("Decision Frequency")]
        public float thinkInterval = 1.0f;

        [Header("Decision Thresholds")]
        [Tooltip("Urgency must exceed this before the NPC acts on a need. 0.3 = needs to be ~70% depleted.")]
        public float urgencyThreshold = 0.3f;

        // Read by CheckNotInDialogue BT task to gate the entire tree
        public bool IsInDialogue { get; private set; } = false;

        private NPCStats _stats;
        private BehaviorTree _behaviorTree;
        private float _nextThinkTime;
        private bool _isBusy = false;
        private SmartObject _currentTarget = null;

        private const string VAR_TARGET_OBJ = "TargetObject";
        private const string VAR_EXTERNAL_TREE = "TargetBehaviorTree";
        private const string VAR_INTERACTION_POINT = "InteractionPoint";

        void Awake()
        {
            _stats = GetComponent<NPCStats>();
            _behaviorTree = GetComponent<BehaviorTree>();
            Debug.Log($"[NPC Brain] AWAKE on {gameObject.name}. Stats: {_stats != null}, Tree: {_behaviorTree != null}");
        }

        void Start()
        {
            if (smartObjectLayer == 0)
                Debug.LogError($"[NPC Brain] CRITICAL: Smart Object Layer is set to NOTHING!");
        }

        void Update()
        {
            // Don't think during dialogue or while executing a goal
            if (_isBusy || IsInDialogue) return;

            if (Time.time >= _nextThinkTime)
            {
                _nextThinkTime = Time.time + thinkInterval;
                DecideBestAction();
            }
        }

        public void SetDialogue(bool inDialogue)
        {
            IsInDialogue = inDialogue;

            if (!inDialogue)
            {
                // Clear stale state so brain picks fresh on next tick
                _currentTarget = null;
                _isBusy = false;
                _nextThinkTime = Time.time + thinkInterval;
            }
        }

        public void SetBusy(bool busy)
        {
            _isBusy = busy;
            if (!busy) _currentTarget = null;
            Debug.Log($"[NPC Brain] Busy state set to: {busy}");
        }

        public void DecideBestAction()
        {
            if (SmartObjectManager.Instance == null)
            {
                Debug.LogError("[NPC Brain] CRITICAL: SmartObjectManager is missing!");
                return;
            }

            List<SmartObject> candidates = SmartObjectManager.Instance.GetRelevantObjects();
            if (candidates.Count == 0) return;

            float bestScore = -1f;
            SmartObject bestObject = null;
            AIInteractionData bestInteraction = default;

            foreach (var smartObj in candidates)
            {
                float distSqr = (smartObj.transform.position - transform.position).sqrMagnitude;
                if (distSqr > perceptionRadius * perceptionRadius) continue;

                foreach (var interaction in smartObj.GetAllAIInteractions())
                {
                    float score = CalculateScore(smartObj, interaction);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestObject = smartObj;
                        bestInteraction = interaction;
                    }
                }
            }

            if (bestObject != null && bestScore > 3f)
            {
                Debug.Log($"[NPC Brain] WINNER: {bestObject.name} with score {bestScore}");
                SetGoal(bestObject, bestInteraction);
            }
        }

        private float CalculateScore(SmartObject obj, AIInteractionData interaction)
        {
            if (interaction.satisfiesNeed == null) return 0f;

            var need = _stats.GetNeed(interaction.satisfiesNeed);
            if (need == null) return 0f;

            float urgency = need.GetUrgency();
            if (urgency < urgencyThreshold) return 0f;

            float distance = Vector3.Distance(transform.position, obj.transform.position);
            float distanceFactor = Mathf.Clamp01(1f - (distance / perceptionRadius));
            float score = (urgency * 10f) + (interaction.recoveryAmount * 0.1f) + (distanceFactor * 2f);

            Debug.Log($"[NPC Brain] MATH REPORT for {obj.name}:\n" +
                      $"- Need: {interaction.satisfiesNeed.name}\n" +
                      $"- Current Value: {need.value}\n" +
                      $"- Urgency: {urgency}\n" +
                      $"- FINAL SCORE: {score}");

            return score;
        }

        private void SetGoal(SmartObject obj, AIInteractionData interaction)
        {
            if (obj == _currentTarget) return;
            _currentTarget = obj;

            _behaviorTree.SetVariableValue(VAR_TARGET_OBJ, obj.gameObject);

            if (interaction.behaviorTree != null)
                _behaviorTree.SetVariableValue(VAR_EXTERNAL_TREE, interaction.behaviorTree);
            else
                Debug.LogError($"[NPC Brain] Winner '{obj.name}' has no Behavior Tree assigned!");

            Transform targetPoint = interaction.interactionPoint != null
                ? interaction.interactionPoint
                : obj.transform;
            _behaviorTree.SetVariableValue(VAR_INTERACTION_POINT, targetPoint.gameObject);

            Debug.Log($"[NPC Brain] New goal: {obj.name}. Restarting BT.");
            _behaviorTree.DisableBehavior(false);
            _behaviorTree.EnableBehavior();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
        }
    }
}