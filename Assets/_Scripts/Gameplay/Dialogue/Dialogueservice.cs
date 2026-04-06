using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    /// <summary>
    /// Persistent singleton that holds the DialogueDatabase reference.
    /// Drop this on your persistent managers GameObject and assign the database asset.
    /// </summary>
    public class DialogueService : MonoBehaviour
    {
        public static DialogueService Instance { get; private set; }

        [Tooltip("Drag your DialogueDatabase asset here.")]
        public DialogueDatabase database;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (database == null)
                Debug.LogWarning("[DialogueService] No DialogueDatabase assigned!");
        }

        /// <summary>
        /// Ask the picker for the best node name for this NPC given current game state.
        /// Returns null if nothing qualifies.
        /// </summary>
        public string GetNodeForNPC(string npcID)
        {
            return DialoguePicker.Pick(npcID, database);
        }
    }
}