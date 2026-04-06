using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// One dialogue entry. Equivalent to one row in the dialogue spreadsheet.
    /// The picker evaluates all conditions and returns the node name if they pass.
    /// </summary>
    [Serializable]
    public class DialogueEntry
    {
        [Header("Identity")]
        [Tooltip("Which NPC does this entry belong to? Must match NPCIdentity.npcID exactly.")]
        public string npcID;

        [Tooltip("Unique ID for this entry. Used to mark it as completed. e.g. 'viktor_firstmeet'")]
        public string entryID;

        [Tooltip("The node title in the .yarn file to run if this entry wins.")]
        public string nodeName;

        [Tooltip("Higher priority entries are checked first. First passing entry wins.")]
        public int priority = 0;

        [Tooltip("If true, this entry can only fire once. It is then marked completed and skipped forever.")]
        public bool oneTimeOnly = false;

        // -------------------------------------------------------------------------
        // Conditions — all must pass for this entry to be eligible
        // Leave a condition at its default value to ignore it
        // -------------------------------------------------------------------------

        [Header("Relationship Conditions")]
        [Tooltip("Minimum relationship value required. -1 = ignore.")]
        public float minRelationship = -1f;

        [Tooltip("Maximum relationship value allowed. -1 = ignore.")]
        public float maxRelationship = -1f;

        [Tooltip("Required relationship stage. None = ignore.")]
        public RelationshipStage requiredStage = RelationshipStage.Stranger;

        [Tooltip("Whether to enforce the stage check at all.")]
        public bool checkStage = false;

        [Header("Personality Conditions")]
        [Tooltip("Minimum order axis value required (-100 chaotic to 100 lawful). -999 = ignore.")]
        public float minOrder = -999f;
        public float maxOrder = 999f;

        [Tooltip("Minimum diligence axis value required (-100 slacker to 100 academic). -999 = ignore.")]
        public float minDiligence = -999f;
        public float maxDiligence = 999f;

        [Tooltip("Minimum social axis value required (-100 sigma to 100 romantic). -999 = ignore.")]
        public float minSocial = -999f;
        public float maxSocial = 999f;

        [Header("Flag Conditions")]
        [Tooltip("ALL of these flags must be true for this entry to pass.")]
        public List<string> requiredFlags = new List<string>();

        [Tooltip("ALL of these flags must be false for this entry to pass.")]
        public List<string> blockedFlags = new List<string>();

        [Header("Item Conditions")]
        [Tooltip("Player must have ALL of these items in inventory.")]
        public List<string> requiredItems = new List<string>();

        [Header("Dialogue Flag Conditions")]
        [Tooltip("ALL of these dialogue entryIDs must have been completed before this entry is eligible.")]
        public List<string> requiredCompletedDialogues = new List<string>();

        [Tooltip("ALL of these dialogue entryIDs must NOT have been completed.")]
        public List<string> blockedCompletedDialogues = new List<string>();

        [Header("Notes")]
        [Tooltip("Designer notes. Not used in game.")]
        public string notes;
    }
}