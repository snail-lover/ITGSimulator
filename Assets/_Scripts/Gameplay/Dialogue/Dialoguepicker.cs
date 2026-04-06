using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    /// <summary>
    /// Given an NPC id, walks the DialogueDatabase sorted by priority and returns
    /// the first entry whose conditions all pass against the current game state.
    /// </summary>
    public static class DialoguePicker
    {
        /// <summary>
        /// Returns the winning node name for an NPC, or null if nothing qualifies.
        /// </summary>
        public static string Pick(string npcID, DialogueDatabase database)
        {
            if (string.IsNullOrEmpty(npcID))
            {
                Debug.LogWarning("[DialoguePicker] npcID is null or empty.");
                return null;
            }

            if (database == null)
            {
                Debug.LogWarning("[DialoguePicker] No database provided.");
                return null;
            }

            List<DialogueEntry> candidates = database.GetEntriesForNPC(npcID);

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[DialoguePicker] No entries found for NPC '{npcID}'.");
                return null;
            }

            foreach (DialogueEntry entry in candidates)
            {
                if (PassesConditions(entry))
                {
                    // Mark one-time entries as completed so they never fire again
                    if (entry.oneTimeOnly)
                        WorldDataManager.Instance.MarkHighPriorityDialogueAsCompleted(entry.entryID);

                    Debug.Log($"[DialoguePicker] '{npcID}' -> '{entry.nodeName}' (entry: {entry.entryID}, priority: {entry.priority})");
                    return entry.nodeName;
                }
            }

            Debug.LogWarning($"[DialoguePicker] No passing entry found for NPC '{npcID}'.");
            return null;
        }

        // -------------------------------------------------------------------------
        // Condition evaluation
        // -------------------------------------------------------------------------

        private static bool PassesConditions(DialogueEntry entry)
        {
            // One-time: skip if already completed
            if (entry.oneTimeOnly &&
                WorldDataManager.Instance.HasHighPriorityDialogueBeenCompleted(entry.entryID))
                return false;

            // Relationship
            if (!PassesRelationship(entry)) return false;

            // Personality
            if (!PassesPersonality(entry)) return false;

            // World flags
            if (!PassesFlags(entry)) return false;

            // Inventory
            if (!PassesItems(entry)) return false;

            // Dialogue history
            if (!PassesDialogueHistory(entry)) return false;

            return true;
        }

        private static bool PassesRelationship(DialogueEntry entry)
        {
            if (RelationshipManager.Instance == null) return true;

            float value = RelationshipManager.Instance.GetRelationship(entry.npcID);

            if (entry.minRelationship >= 0f && value < entry.minRelationship) return false;
            if (entry.maxRelationship >= 0f && value > entry.maxRelationship) return false;

            if (entry.checkStage)
            {
                RelationshipStage currentStage = RelationshipManager.Instance.GetStage(entry.npcID);
                if (currentStage != entry.requiredStage) return false;
            }

            return true;
        }

        private static bool PassesPersonality(DialogueEntry entry)
        {
            if (PersonalityManager.Instance == null) return true;

            float order = PersonalityManager.Instance.orderAxis;
            float diligence = PersonalityManager.Instance.diligenceAxis;
            float social = PersonalityManager.Instance.socialAxis;

            if (order < entry.minOrder || order > entry.maxOrder) return false;
            if (diligence < entry.minDiligence || diligence > entry.maxDiligence) return false;
            if (social < entry.minSocial || social > entry.maxSocial) return false;

            return true;
        }

        private static bool PassesFlags(DialogueEntry entry)
        {
            if (WorldDataManager.Instance == null) return true;

            foreach (string flag in entry.requiredFlags)
                if (!WorldDataManager.Instance.GetGlobalFlag(flag)) return false;

            foreach (string flag in entry.blockedFlags)
                if (WorldDataManager.Instance.GetGlobalFlag(flag)) return false;

            return true;
        }

        private static bool PassesItems(DialogueEntry entry)
        {
            if (WorldDataManager.Instance == null) return true;

            foreach (string itemID in entry.requiredItems)
                if (!WorldDataManager.Instance.PlayerHasItem(itemID)) return false;

            return true;
        }

        private static bool PassesDialogueHistory(DialogueEntry entry)
        {
            if (WorldDataManager.Instance == null) return true;

            foreach (string id in entry.requiredCompletedDialogues)
                if (!WorldDataManager.Instance.HasHighPriorityDialogueBeenCompleted(id)) return false;

            foreach (string id in entry.blockedCompletedDialogues)
                if (WorldDataManager.Instance.HasHighPriorityDialogueBeenCompleted(id)) return false;

            return true;
        }
    }
}