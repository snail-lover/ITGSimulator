using UnityEngine;
using System;
using Game.Core;
using System.Collections.Generic;

namespace Game.Gameplay
{

    public class RelationshipManager : MonoBehaviour
    {
        public static RelationshipManager Instance { get; private set; }

        public static event Action<string, float, RelationshipStage> OnRelationshipChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnAfterLoad += OnAfterLoad;
            }
        }

        private void OnDisable()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnAfterLoad -= OnAfterLoad;
            }
        }

        private void OnAfterLoad()
        {
            // Relationships live directly in saveData so nothing to sync —
            // any read will get the freshly loaded data automatically
            Debug.Log("[RelationshipManager] Save data loaded.");
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Get relationship value (0-100) for an NPC. Returns 0 if never met.</summary>
        public float GetRelationship(string npcID)
        {
            if (WorldDataManager.Instance.saveData.npcRelationships.TryGetValue(npcID, out float value))
                return value;
            return 0f;
        }

        /// <summary>Returns a snapshot of all tracked NPC relationships for debug display.</summary>
        public IReadOnlyDictionary<string, float> GetAllRelationships()
            => WorldDataManager.Instance.saveData.npcRelationships;

        /// <summary>Add (or subtract) relationship points with an NPC.</summary>
        public void ModifyRelationship(string npcID, float amount)
        {
            var relationships = WorldDataManager.Instance.saveData.npcRelationships;

            float current = relationships.ContainsKey(npcID) ? relationships[npcID] : 0f;
            float newValue = Mathf.Clamp(current + amount, 0f, 100f);
            relationships[npcID] = newValue;

            Debug.Log($"[RelationshipManager] {npcID}: {current} -> {newValue}");
            OnRelationshipChanged?.Invoke(npcID, newValue, GetStage(npcID));
        }

        /// <summary>Get the named relationship stage for an NPC.</summary>
        public RelationshipStage GetStage(string npcID)
        {
            return ValueToStage(GetRelationship(npcID));
        }

        /// <summary>Convert a raw 0-100 value to a named stage.</summary>
        public static RelationshipStage ValueToStage(float value)
        {
            if (value >= 80f) return RelationshipStage.Dating;
            if (value >= 60f) return RelationshipStage.RomanticInterest;
            if (value >= 40f) return RelationshipStage.Friend;
            if (value >= 20f) return RelationshipStage.Acquaintance;
            return RelationshipStage.Stranger;
        }
    }
}