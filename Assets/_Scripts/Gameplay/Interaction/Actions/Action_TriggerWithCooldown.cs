using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Gameplay
{
    // Right-click > Create > Game > Actions > Trigger with Cooldown
    [CreateAssetMenu(menuName = "Game/Actions/Trigger with Cooldown")]
    public class Action_TriggerWithCooldown : InteractionAction
    {
        [Header("Button Config")]
        public string buttonLabel = "Flush";
        public float cooldownSeconds = 5f;

        // We use a dictionary to track cooldowns for EVERY object separately.
        // Key = GameObject Instance ID, Value = Time when it can be used again.
        private static Dictionary<int, float> _cooldowns = new Dictionary<int, float>();

        public override InteractionOption GetOption(SmartObject target, InteractionContext context)
        {
            if (IsBlocked(target)) return null;

            // 1. Check Cooldown
            int targetID = target.gameObject.GetInstanceID();
            if (_cooldowns.ContainsKey(targetID))
            {
                if (Time.time < _cooldowns[targetID])
                {
                    // It is on cooldown. Return NULL to hide the button.
                    // (Or return a disabled gray button if your UI supports that)
                    return null; 
                }
            }

            // 2. Check if it actually has sound to play
            var audioSource = target.GetComponent<AudioSource>();
            if (audioSource == null) return null; // No audio? Can't flush.

            return new InteractionOption
            {
                Label = buttonLabel,
                Priority = priority,
                ActionToRun = () =>
                {
                    // Play Sound
                    audioSource.Play();

                    // Set Cooldown
                    _cooldowns[targetID] = Time.time + cooldownSeconds;
                    
                    Debug.Log($"Used {buttonLabel} on {target.name}. Cooldown until {Time.time + cooldownSeconds}");
                }
            };
        }
    }
}