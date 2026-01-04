using UnityEngine;
using System.Collections.Generic; // Needed for List
using Game.Core;

namespace Game.Gameplay
{
    public abstract class InteractionAction : ScriptableObject
    {
        public string actionName;
        public int priority = 0;

        [Header("Global Constraints")]
        [Tooltip("If the object has ANY of these states active (value 1), this action will be hidden.")]
        public List<StateDefinition> blockedByStates = new List<StateDefinition>();

        // The abstract method you implement in child classes
        public abstract InteractionOption GetOption(SmartObject target, InteractionContext context);

        // --- HELPER METHOD ---
        // Call this at the start of your GetOption() methods
        protected bool IsBlocked(SmartObject target)
        {
            foreach (var state in blockedByStates)
            {
                // If the object has this state set to 1 (True)
                if (target.GetState(state) == 1)
                {
                    return true; // Action is blocked
                }
            }
            return false; // All clear
        }
    }
}