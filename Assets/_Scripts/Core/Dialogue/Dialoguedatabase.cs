using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Holds all dialogue entries for all NPCs in the game.
    /// One asset lives in your project. Later this will be populated from a CSV importer.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Database")]
    public class DialogueDatabase : ScriptableObject
    {
        [SerializeField] public string googleSheetCsvUrl;
        public List<DialogueEntry> entries = new List<DialogueEntry>();

        /// <summary>
        /// Returns all entries for a given NPC, sorted by priority descending.
        /// </summary>
        public List<DialogueEntry> GetEntriesForNPC(string npcID)
        {
            return entries
                .Where(e => e.npcID == npcID)
                .OrderByDescending(e => e.priority)
                .ToList();
        }
    }
}