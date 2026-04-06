using UnityEngine;
using System;
using System.Collections.Generic;
using Game.Core;  

namespace Game.Gameplay
{
    public class PersonalityManager : MonoBehaviour
    {
        public static PersonalityManager Instance { get; private set; }

        [Header("Current Personality (Read Only)")]
        [Tooltip("Range: -100 (Chaotic) to +100 (Lawful)")]
        [Range(-100, 100)] public float orderAxis = 0;

        [Tooltip("Range: -100 (Slacker) to +100 (Academic)")]
        [Range(-100, 100)] public float diligenceAxis = 0;

        [Tooltip("Range: -100 (Sigma) to +100 (Romantic)")]
        [Range(-100, 100)] public float socialAxis = 0;

        // Event for UI to listen to
        public event System.Action OnPersonalityChanged;
        
        public static event Action<TraitType, float> OnTraitModified; 

        void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); }
        }

        // --- SAVE SYSTEM INTEGRATION START ---
        void Start()
        {
            // If the game has already been loaded from a file (continuing a session),
            // we need to grab that data immediately.
            if (WorldDataManager.Instance != null && !WorldDataManager.Instance.isNewGame)
            {
                LoadFromSave();
            }
        }

        void OnEnable()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnBeforeSave += SaveToWorldData;
                WorldDataManager.Instance.OnAfterLoad += LoadFromSave;
            }
        }

        void OnDisable()
        {
            if (WorldDataManager.Instance != null)
            {
                WorldDataManager.Instance.OnBeforeSave -= SaveToWorldData;
                WorldDataManager.Instance.OnAfterLoad -= LoadFromSave;
            }
        }

        // Called RIGHT BEFORE the game writes to disk
        private void SaveToWorldData()
        {
            var saveSlot = WorldDataManager.Instance.saveData.playerPersonality;
            
            saveSlot.orderAxis = this.orderAxis;
            saveSlot.diligenceAxis = this.diligenceAxis;
            saveSlot.socialAxis = this.socialAxis;

            Debug.Log("[PersonalityManager] Saved personality state.");
        }

        // Called RIGHT AFTER the game reads from disk
        private void LoadFromSave()
        {
            var saveSlot = WorldDataManager.Instance.saveData.playerPersonality;

            this.orderAxis = saveSlot.orderAxis;
            this.diligenceAxis = saveSlot.diligenceAxis;
            this.socialAxis = saveSlot.socialAxis;

            Debug.Log($"[PersonalityManager] Loaded state. Order: {orderAxis}");

            // Update UI
            OnPersonalityChanged?.Invoke();
        }


        public void ApplyShift(TraitShift shift)
        {
            ModifyTrait(shift.type, shift.amount);
        }

        public void ModifyTrait(TraitType type, float amount)
        {
            
            switch (type)
            {
                // Order Axis
                case TraitType.Lawful: orderAxis += amount; break;
                case TraitType.Chaotic: orderAxis -= amount; break;

                // Diligence Axis
                case TraitType.Academic: diligenceAxis += amount; break;
                case TraitType.Slacker: diligenceAxis -= amount; break;

                // Social Axis
                case TraitType.Romantic: socialAxis += amount; break;
                case TraitType.Sigma: socialAxis -= amount; break;
            }

            // Clamp
            orderAxis = Mathf.Clamp(orderAxis, -100f, 100f);
            diligenceAxis = Mathf.Clamp(diligenceAxis, -100f, 100f);
            socialAxis = Mathf.Clamp(socialAxis, -100f, 100f);

            OnPersonalityChanged?.Invoke();
            if(Mathf.Abs(amount) > 0)
            {
                OnTraitModified?.Invoke(type, amount);
            }
        }
        
        public float CheckCompatibility(TraitType npcPreference)
        {
            float playerVal = 0;
            float targetVal = 0; 

            switch (npcPreference)
            {
                case TraitType.Lawful:   playerVal = orderAxis; targetVal = 100; break;
                case TraitType.Chaotic:  playerVal = orderAxis; targetVal = -100; break;
                case TraitType.Academic: playerVal = diligenceAxis; targetVal = 100; break;
                case TraitType.Slacker:  playerVal = diligenceAxis; targetVal = -100; break;
                case TraitType.Romantic: playerVal = socialAxis; targetVal = 100; break;
                case TraitType.Sigma:    playerVal = socialAxis; targetVal = -100; break;
            }

            float diff = Mathf.Abs(targetVal - playerVal);
            return 1.0f - (diff / 100f); 
        }
    }
}