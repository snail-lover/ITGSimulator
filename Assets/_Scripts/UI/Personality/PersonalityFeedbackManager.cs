using UnityEngine;
using Game.Gameplay;
using Game.Core;

namespace Game.UI
{
    [RequireComponent(typeof(AudioSource))]
    public class PersonalityFeedbackManager : MonoBehaviour
    {
        [Header("UI Setup")]
        [Tooltip("Where the text spawns (usually top center of screen)")]
        [SerializeField] private Transform spawnContainer; 
        [SerializeField] private FloatingFeedbackText textPrefab;

        [Header("Audio")]
        [SerializeField] private AudioClip traitGainedSound;
        
        [Header("Colors")]
        [SerializeField] private Color positiveColor = Color.cyan; // For Lawful/Academic/Romantic
        [SerializeField] private Color negativeColor = Color.magenta; // For Chaotic/Slacker/Sigma

        private AudioSource _audioSource;

        void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            PersonalityManager.OnTraitModified += ShowFeedback;
        }

        void OnDisable()
        {
            PersonalityManager.OnTraitModified -= ShowFeedback;
        }

        private void ShowFeedback(TraitType type, float amount)
        {
            // 1. Play Sound
            if (traitGainedSound != null)
            {
                _audioSource.PlayOneShot(traitGainedSound);
            }

            // 2. Determine Text
            // Example: "Lawful Increased" or "Lawful +10"
            string message = $"{type} Increased";
            if (amount < 0) message = $"{type} Decreased"; // Rare case if you pass negative numbers

            // 3. Determine Color (Optional Logic)
            Color visualColor = positiveColor;
            
            // If you want specific colors for specific traits:
            if (type == TraitType.Chaotic || type == TraitType.Slacker || type == TraitType.Sigma)
            {
                visualColor = negativeColor;
            }

            // 4. Spawn and Setup
            FloatingFeedbackText instance = Instantiate(textPrefab, spawnContainer);
            instance.Setup(message, visualColor);
        }
    }
}