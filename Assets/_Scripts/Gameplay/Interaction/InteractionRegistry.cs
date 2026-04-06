using UnityEngine;
using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay
{
    [CreateAssetMenu(menuName = "Game/Systems/Interaction Registry")]
    public class InteractionRegistry : ScriptableObject
    {
        public static InteractionRegistry Instance { get; private set; }

        [System.Serializable]
        public struct TagRule
        {
            public ReceptorTagDefinition tag;
            public List<InteractionAction> actions;
        }

        // --- NEW: Actions that apply to EVERY SmartObject ---
        [Header("Universal Actions")]
        [Tooltip("Actions in this list are added to EVERY SmartObject automatically (e.g. Inspect).")]
        public List<InteractionAction> AlwaysAvailableActions = new List<InteractionAction>();

        [Header("Tag-Based Rules")]
        public List<TagRule> Rules = new List<TagRule>();

        private Dictionary<ReceptorTagDefinition, List<InteractionAction>> _lookup;

        public void Initialize()
        {
            Instance = this;
            _lookup = new Dictionary<ReceptorTagDefinition, List<InteractionAction>>();

            foreach (var rule in Rules)
            {
                if (rule.tag != null)
                {
                    if (!_lookup.ContainsKey(rule.tag))
                    {
                        _lookup[rule.tag] = new List<InteractionAction>();
                    }
                    _lookup[rule.tag].AddRange(rule.actions);
                }
            }
            Debug.Log($"[InteractionRegistry] Loaded rules. Universal Actions: {AlwaysAvailableActions.Count}");
        }

        public List<InteractionAction> GetActionsForTag(ReceptorTagDefinition tag)
        {
            if (_lookup == null) Initialize();
            return _lookup.ContainsKey(tag) ? _lookup[tag] : null;
        }
    }
}