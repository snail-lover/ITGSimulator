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
        [TextArea(3, 10)] 
        public string description;

        [Header("Properties")]
        // CHANGED: From Enum to List of Assets
        public List<ItemTagDefinition> tags = new List<ItemTagDefinition>();

        [Header("Model Prefab")]
        public GameObject worldPrefab;
        
        [Header("Holding Settings")]
// Add these offsets so you can tweak the grip without changing the model
        public Vector3 holdPositionOffset;
        public Vector3 holdRotationOffset;
        
        [Header("Holding Physics")]
        // Should we extend the player's collider when holding this?
        public bool useBumperCollider = true;
        
        [Header("Preview Settings")]
        // How should the model be rotated to look good in the camera?
        public Vector3 previewRotation; 
        // Is the model too big/small? Adjust this.
        public float previewScale = 1f;
        

        // Helper to check for a tag
        public bool HasTag(ItemTagDefinition tagToCheck)
        {
            return tags.Contains(tagToCheck);
        }
    }
}