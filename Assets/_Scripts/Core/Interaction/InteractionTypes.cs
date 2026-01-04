using UnityEngine;
using System;

namespace Game.Core
{
    // The Context: "Who is asking?"
    public class InteractionContext
    {
        public GameObject Source; // The Player
        public string HeldItemID; // Example: "crowbar"
        // In the future: public List<string> HeldItemTags; 
    }

    // The Result: "What can you do?"
    public class InteractionOption
    {
        public string Label;       // e.g., "Kick Cube"
        public Sprite Icon;        // Optional UI icon
        public int Priority;       // 10 = Default interaction, 0 = Context menu only
        public Action ActionToRun; // The specific function to execute
    }

    [Serializable]
    public struct ConditionalText
    {
        [Tooltip("The text to display.")]
        [TextArea] public string text;

        [Tooltip("The state required for this text. Leave Empty for 'Default'.")]
        public StateDefinition requiredState;

        [Tooltip("The value the state must match (e.g., 1 for Broken).")]
        public int requiredValue;
    }


}