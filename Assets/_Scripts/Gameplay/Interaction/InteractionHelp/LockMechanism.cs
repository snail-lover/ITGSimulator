using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    [RequireComponent(typeof(SmartObject))]
    public class LockMechanism : MonoBehaviour
    {
        [Header("Configuration")]
        public string requiredKeyID; 
        
        [Header("States")]
        public StateDefinition lockedState; // State_IsLocked
        public StateDefinition openState;   // State_IsOpen (Optional, for auto-opening)
        public StateDefinition brokenState; // State_IsBroken (For the smash reaction)

        [Header("Audio")]
        public AudioClip unlockSound;
        public AudioClip breakSound;
        public AudioSource audioSource;

        private SmartObject _smartObj;

        void Awake()
        {
            _smartObj = GetComponent<SmartObject>();
        }

        void OnEnable()
        {
            _smartObj.OnStateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            _smartObj.OnStateChanged -= OnStateChanged;
        }

        // --- THE BRAIN ---
        private void OnStateChanged(StateDefinition state, int value)
        {
            // If the Universal Action sets "Broken" to 1...
            if (state == brokenState && value == 1)
            {
                // ...We react by Force Unlocking and Force Opening
                ForceBreakOpen();
            }
        }

        public void UnlockWithKey()
        {
            // 1. Set Locked to 0
            _smartObj.SetState(lockedState, 0);
            
            // 2. Play sound
            if (audioSource && unlockSound) audioSource.PlayOneShot(unlockSound);
            
            Debug.Log("Door unlocked via Key.");
        }

        private void ForceBreakOpen()
        {
            // 1. Force Unlock
            _smartObj.SetState(lockedState, 0);

            // 2. Force Open (Swing the door immediately)
            if (openState != null) _smartObj.SetState(openState, 1);

            // 3. Play Smash Sound
            if (audioSource && breakSound) audioSource.PlayOneShot(breakSound);

            Debug.Log("Door lock destroyed. Door forced open.");
        }
    }
}