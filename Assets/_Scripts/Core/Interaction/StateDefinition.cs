using UnityEngine;

namespace Game.Core
{
    // Right-click > Create > Game > State Definition
    [CreateAssetMenu(menuName = "Game/State Definition")]
    public class StateDefinition : ScriptableObject
    {
        [TextArea] public string description;

        [Header("UI Tags (Inventory)")]
        public bool showInUI = false;
        public string uiLabel;
        public Color uiColor = Color.white; 

        // --- NEW SECTION ---
        [Header("Name Modifier (Context Menu)")]
        [Tooltip("Text to append to the name when active. Example: ' (Locked)'")]
        public string activeNameSuffix; 
    }
}