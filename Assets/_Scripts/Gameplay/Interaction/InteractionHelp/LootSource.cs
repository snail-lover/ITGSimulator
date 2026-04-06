using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    public class LootSource : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The ID of the item to give. Leave empty for a container that is always empty.")]
        public string itemIDToGive;

        [Tooltip("Sound to play when searching (rummaging sound)")]
        public AudioClip searchSound;

        // Helper to extract the item
        public string TakeItem()
        {
            string item = itemIDToGive;
            itemIDToGive = null; // Remove it so it can't be taken twice from logic
            return item;
        }
    }
}