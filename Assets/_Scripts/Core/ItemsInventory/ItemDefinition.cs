using UnityEngine;
using System.Collections.Generic;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Game/Definitions/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Core Data")]
        public string id; // Unique ID like "viktors_camera"
        public string itemName; // Display name like "Viktor's Camera"
        public Sprite icon;

        [Header("Properties")]
        // CHANGED: From Enum to List of Assets
        public List<ItemTagDefinition> tags = new List<ItemTagDefinition>();

        [Header("Model Prefab")]
        public GameObject worldPrefab;

        // Helper to check for a tag
        public bool HasTag(ItemTagDefinition tagToCheck)
        {
            return tags.Contains(tagToCheck);
        }
    }
}