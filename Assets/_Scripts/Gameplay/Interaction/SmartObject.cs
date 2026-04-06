using Game.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviorDesigner.Runtime; 

namespace Game.Gameplay
{
    public class SmartObject : MonoBehaviour, IInteractable
    {
        [Header("Identity")]
        [SerializeField] private string objectName = "Object";
        
        [Header("Intrinsic Tags")]
        [Tooltip("What IS this object? (e.g., Brittle, Electronic, Container). These link to Global Rules.")]
        public List<ReceptorTagDefinition> receptorTags = new List<ReceptorTagDefinition>();

        [Header("Local Logic (Specifics)")]
        [Tooltip("Actions specific ONLY to this object (e.g., 'Open Secret Passage').")]
        [SerializeField] private List<InteractionAction> interactionActions = new List<InteractionAction>();

        [Header("State (The Vitals)")]
        [SerializeField] private List<StateEntry> initialStates = new List<StateEntry>();

        [Header("Descriptions")]
        [SerializeField] private List<ConditionalText> inspectDescriptions = new List<ConditionalText>();
        
        [Header("Significance")]
        [Tooltip("Multiplier for rewards. 1 = Mug, 5 = Door, 20 = Server.")]
        [Range(0.1f, 100f)]
        public float interactionWeight = 1f; 
        
        [Header("AI Configuration")]
        [Tooltip("Advertisements for NPCs. 'I can fix your Hunger!'")]
        [SerializeField] private List<AIInteractionData> aiInteractions = new List<AIInteractionData>();
        
        private bool _isRegistered = false;


        // Helper for the NPC to query: "Can you fix my Hunger?"
        public AIInteractionData? GetInteractionForNeed(NeedDefinition need)
        {
            foreach (var interaction in aiInteractions)
            {
                if (interaction.satisfiesNeed == need)
                {
                    // Check if object state prevents use (optional logic)
                    // e.g. if (GetState(BrokenState) == 1) return null;
                    
                    return interaction;
                }
            }
            return null;
        }

        public List<AIInteractionData> GetAllAIInteractions()
        {
            return aiInteractions;
        }
        
        

        // Runtime State Dictionary
        private Dictionary<StateDefinition, int> _stateDictionary = new Dictionary<StateDefinition, int>();
        public event System.Action<StateDefinition, int> OnStateChanged;

        void Awake()
        {
            // Initialize State Dictionary from Inspector values
            foreach (var entry in initialStates)
            {
                if (entry.state != null) _stateDictionary[entry.state] = entry.value;
            }
        }

        void OnEnable()
        {
            TryRegister();
        }

        void Start()
        {
            // Fallback: If Manager wasn't ready during OnEnable, try again now.
            if (!_isRegistered)
            {
                TryRegister();
            }
        }

        void OnDisable()
        {
            if (SmartObjectManager.Instance != null)
            {
                SmartObjectManager.Instance.UnregisterObject(this);
                _isRegistered = false;
            }
        }

                private void TryRegister()
        {
            // Filter out objects with no AI data
            if (aiInteractions.Count == 0) return;

            if (SmartObjectManager.Instance != null)
            {
                SmartObjectManager.Instance.RegisterObject(this);
                _isRegistered = true;
            }
        }

        // --- IInteractable Implementation ---
        public string GetName()
        {
            // Start with the base name (e.g., "Door")
            string finalName = objectName;

            // Loop through all currently tracked states
            foreach (var kvp in _stateDictionary)
            {
                StateDefinition stateDef = kvp.Key;
                int value = kvp.Value;

                // If the state is Active (1) AND has a suffix configured
                if (value == 1 && stateDef != null && !string.IsNullOrEmpty(stateDef.activeNameSuffix))
                {
                    finalName += stateDef.activeNameSuffix;
                }
            }

            return finalName;
        }

        public bool HasReceptorTag(ReceptorTagDefinition tag)
        {
            return receptorTags.Contains(tag);
        }

        public List<InteractionOption> GetInteractions(InteractionContext context)
        {
            List<InteractionOption> validOptions = new List<InteractionOption>();

            // ---------------------------------------------------------
            // 1. CHECK LOCAL ACTIONS (Specific to this prefab)
            // ---------------------------------------------------------
            CheckAndAddActions(interactionActions, context, validOptions);

            // ---------------------------------------------------------
            // 2. CHECK GLOBAL RULES (Universal Physics/Logic)
            // ---------------------------------------------------------
            // We loop through our tags (e.g., Brittle) and ask the Registry
            // "What can happen to Brittle things?"
            if (InteractionRegistry.Instance != null)
            {
                // 1. ADD UNIVERSAL ACTIONS (Inspect)
                // We add these to ALL objects.
                CheckAndAddActions(InteractionRegistry.Instance.AlwaysAvailableActions, context, validOptions);

                // 2. ADD TAG-BASED ACTIONS (Drag, Pickup, Smash)
                foreach (var tag in receptorTags)
                {
                    List<InteractionAction> globalActions = InteractionRegistry.Instance.GetActionsForTag(tag);
                    CheckAndAddActions(globalActions, context, validOptions);
                }
            }
            else
            {
                // Optional warning if you forget to put the Registry in the scene
                // Debug.LogWarning("InteractionRegistry.Instance is null! Global rules won't work.");
            }

            // ---------------------------------------------------------
            // 3. GENERIC FALLBACK (The "Use Item" Logic)
            // ---------------------------------------------------------
            if (context.HeldItem != null)
            {
                // FIX: Instead of checking the name, check the PRIORITY.
                // If we found a specific action (Priority 10), we don't need the fallback.
                // If we only found "Inspect" (Priority 0) or nothing, show the fallback.
                bool hasSpecificAction = validOptions.Any(o => o.Priority > 2);

                if (!hasSpecificAction)
                {
                    validOptions.Add(new InteractionOption
                    {
                        Label = $"Use {context.HeldItem.itemName}",
                        Priority = 1, // Low priority (above Inspect, below Actions)
                        ActionToRun = () => {
                            Debug.Log($"Tried to use {context.HeldItem.id} on {objectName}, but nothing happened.");
                            // Optional: Play a "Confused/Deny" sound here
                        }
                    });
                }
            }

            return validOptions;
        }

        // --- Helper to avoid duplicate code loops ---
        private void CheckAndAddActions(List<InteractionAction> actionsToCheck, InteractionContext context, List<InteractionOption> targetList)
        {
            if (actionsToCheck == null) return;

            foreach (var action in actionsToCheck)
            {
                if (action == null) continue;

                // The Action itself checks constraints (Tags, States, etc.)
                var option = action.GetOption(this, context);
                
                if (option != null) 
                {
                    targetList.Add(option);
                }
            }
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

            _stateDictionary[key] = value;
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
            // 1. Check Specific States
            foreach (var entry in inspectDescriptions)
            {
                if (entry.requiredState != null)
                {
                    if (GetState(entry.requiredState) == entry.requiredValue)
                    {
                        return entry.text;
                    }
                }
            }

            // 2. Default Description
            var defaultEntry = inspectDescriptions.Find(x => x.requiredState == null);
            if (!string.IsNullOrEmpty(defaultEntry.text))
            {
                return defaultEntry.text;
            }

            // 3. Name Fallback
            return $"It's a {objectName}.";
        }
        public List<InventoryItemInstance.SavedState> ExportStates()
        {
            var list = new List<InventoryItemInstance.SavedState>();
            foreach (var kvp in _stateDictionary)
            {
                if (kvp.Key != null)
                {
                    list.Add(new InventoryItemInstance.SavedState 
                    { 
                        stateID = kvp.Key.name, // We rely on asset name matching
                        value = kvp.Value 
                    });
                }
            }
            return list;
        }

        public void ImportStates(List<InventoryItemInstance.SavedState> states)
        {
            if (states == null || states.Count == 0) return;

            foreach (var savedState in states)
            {
                // 1. Convert String ID -> Asset
                // Use the new helper in ItemDatabase
                StateDefinition stateAsset = ItemDatabase.Instance.GetStateByName(savedState.stateID);

                if (stateAsset != null)
                {
                    // 2. Set the value in the local dictionary
                    // This creates the entry if missing, or updates it if present
                    SetState(stateAsset, savedState.value);
                    
                    // Debugging
                    // Debug.Log($"[SmartObject] Imported State: {stateAsset.name} = {savedState.value}");
                }
                else
                {
                    Debug.LogWarning($"[SmartObject] Could not find State Asset named '{savedState.stateID}'. Make sure it is in Resources/States.");
                }
            }
        }
        
        // Helper: Direct set from logic
      public void SetStateByName(string stateName, int value)
        {
            // Search our dictionary keys for a matching name
            foreach (var stateDef in _stateDictionary.Keys)
            {
                if (stateDef.name == stateName)
                {
                    SetState(stateDef, value);
                    return;
                }
            }
            
            // If not found in dictionary, search the initial states list
            foreach (var entry in initialStates)
            {
                if (entry.state != null && entry.state.name == stateName)
                {
                    SetState(entry.state, value);
                    return;
                }
            }
        }
        
    }
}