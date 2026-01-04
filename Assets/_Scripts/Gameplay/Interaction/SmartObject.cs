using Game.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.CullingGroup;

namespace Game.Gameplay
{
    public class SmartObject : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string objectName = "Object";

        [Header("Logic")]
        // This list uses the Gameplay class InteractionAction
        [SerializeField] private List<InteractionAction> interactionActions = new List<InteractionAction>();

        [Header("State")]
        [SerializeField] private List<StateEntry> initialStates = new List<StateEntry>();

        [Header("Descriptions")]
        [SerializeField] private List<ConditionalText> inspectDescriptions = new List<ConditionalText>();

        private Dictionary<StateDefinition, int> _stateDictionary = new Dictionary<StateDefinition, int>();
        public event System.Action<StateDefinition, int> OnStateChanged;

        void Awake()
        {
            foreach (var entry in initialStates)
            {
                if (entry.state != null) _stateDictionary[entry.state] = entry.value;
            }
        }

        // --- IInteractable Implementation ---
        public string GetName() => objectName;

        public List<InteractionOption> GetInteractions(InteractionContext context)
        {
            List<InteractionOption> validOptions = new List<InteractionOption>();

            // 1. Loop through defined actions (Repair, Inspect, etc.)
            foreach (var action in interactionActions)
            {
                if (action == null) continue;
                var option = action.GetOption(this, context);
                if (option != null) validOptions.Add(option);
            }

            // 2. NEW: Generic "Use Item" fallback
            // If we are holding an item, AND we haven't already generated a specific action for it...
            if (!string.IsNullOrEmpty(context.HeldItemID))
            {
                // Check if any existing option is already using this item 
                // (This prevents seeing "Repair" AND "Use Screwdriver" at the same time)
                bool alreadyHandled = validOptions.Any(o => o.Label.Contains(context.HeldItemID)); // Rudimentary check

                if (!alreadyHandled)
                {
                    validOptions.Add(new InteractionOption
                    {
                        Label = $"Use {context.HeldItemID}",
                        Priority = 1, // Low priority
                        ActionToRun = () => {
                            Debug.Log($"Tried to use {context.HeldItemID} on {objectName}, but nothing happened.");
                            // You could trigger a "Wobble" animation or "Confused" bark here
                        }
                    });
                }
            }

            return validOptions;
        }

        // --- State API ---
        public int GetState(StateDefinition key)
        {
            if (key == null) return 0;
            return _stateDictionary.ContainsKey(key) ? _stateDictionary[key] : 0;
        }

        public void SetState(StateDefinition key, int value)
        {
            if (key == null) return;

            // Update dictionary
            _stateDictionary[key] = value;

            // Notify listeners (Visuals, Audio, etc.)
            OnStateChanged?.Invoke(key, value);
        }

        [System.Serializable]
        public struct StateEntry
        {
            public StateDefinition state;
            public int value;
        }

        // --- Description Logic ---
        public string GetDescription()
        {
            // 1. Loop through specific conditions (e.g., Broken)
            foreach (var entry in inspectDescriptions)
            {
                if (entry.requiredState != null)
                {
                    // Check if the object matches this state
                    if (GetState(entry.requiredState) == entry.requiredValue)
                    {
                        return entry.text;
                    }
                }
            }

            // 2. Fallback: Find the entry with NO state (Default)
            var defaultEntry = inspectDescriptions.Find(x => x.requiredState == null);
            if (!string.IsNullOrEmpty(defaultEntry.text))
            {
                return defaultEntry.text;
            }

            // 3. Fallback: Name
            return $"It's a {objectName}.";
        }
    }

}