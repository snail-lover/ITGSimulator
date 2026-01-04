// PersonalityObserver.cs
using UnityEngine;
using Game.Core; // Or whatever your Core namespace is
using Game.Gameplay; // Or whatever your Gameplay namespace is
using System;

/// <summary>
/// A central, globally accessible manager that observes player actions and translates
/// them into changes in the Player's Personality Profile.
/// This fulfills Phase 2.2 of the Attraction Trait Roadmap.
/// </summary>
public class PersonalityObserver : MonoBehaviour
{
    // Singleton pattern for easy global access
    public static PersonalityObserver Instance { get; private set; }

    // --- THIS IS THE OLD FIELD. IT IS NOW GONE. ---
    // [SerializeField] private PlayerPersonalityProfile playerProfile;

    // We get the profile from PlayerData now.
    private PlayerPersonalityProfile playerProfile;

    [Header("Tuning Values")]
    [Tooltip("The amount of change a single tagged action imparts on a personality axis.")]
    [SerializeField]
    private float pointValue = 1f; // Changed to 1f for your testing

    public static event Action OnPersonalityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
    }

    // --- THIS START METHOD IS THE CRITICAL ADDITION ---
    private void Start()
    {
        // Get the single, correct profile instance from the central PlayerData manager.
        if (PlayerData.Instance != null)
        {
            playerProfile = PlayerData.Instance.Profile;
        }

        if (playerProfile == null)
        {
            Debug.LogError("PersonalityObserver could not find the Player Profile from PlayerData.Instance!", this);
            this.enabled = false;
        }
    }


    public void EvaluateAction(ItemTag tags)
    {
        if (playerProfile == null)
        {
            Debug.LogError("PersonalityObserver cannot evaluate action: PlayerProfile is not set! (Was it found in Start?)", this);
            return;
        }

        Debug.Log($"<color=yellow>[PersonalityObserver]</color> Evaluating action with tags: {tags}");

        if (tags.HasFlag(ItemTag.Instinct))
        {
            // Instinct means more Spontaneity (left side of Spontaneity_Planning)
            AdjustPersonality(PersonalityAxis.Spontaneity_Planning, -pointValue);
        }
        if (tags.HasFlag(ItemTag.Planning))
        {
            // Planning means more Planning (right side of Spontaneity_Planning)
            AdjustPersonality(PersonalityAxis.Spontaneity_Planning, pointValue);
        }


        if (tags.HasFlag(ItemTag.Intellect))
        {
            // Intellect means more Intellect (left side of Intellect_Instinct)
            AdjustPersonality(PersonalityAxis.Intellect_Instinct, -pointValue);
        }
        // Note: The 'Instinct' tag above already handles its side of this axis.


        if (tags.HasFlag(ItemTag.Mischief))
        {
            // Mischief means more Mischief (left side of Mischief_Earnestness)
            AdjustPersonality(PersonalityAxis.Mischief_Earnestness, -pointValue);
        }


        if (tags.HasFlag(ItemTag.Forceful))
        {
            // Forceful can imply a lack of Planning (more Spontaneity/Instinct) or Directness.
            // Let's lean towards Instinct (right side of Intellect_Instinct) or Spontaneity (left side of Spontaneity_Planning)
            AdjustPersonality(PersonalityAxis.Intellect_Instinct, pointValue); // More Instinct
            AdjustPersonality(PersonalityAxis.Spontaneity_Planning, -pointValue); // Less Planning/More Spontaneity
        }
        if (tags.HasFlag(ItemTag.Subtle))
        {
            // Subtle can imply more Planning or Intellect.
            AdjustPersonality(PersonalityAxis.Spontaneity_Planning, pointValue); // More Planning
            AdjustPersonality(PersonalityAxis.Intellect_Instinct, -pointValue); // More Intellect
        }
        if (tags.HasFlag(ItemTag.Destructive))
        {
            // Destructive could imply more Instinct, less Planning, or less Altruism.
            AdjustPersonality(PersonalityAxis.Intellect_Instinct, pointValue); // More Instinct
            // AdjustPersonality(PersonalityAxis.Altruism_SelfInterest, -pointValue); // If you want to link it to Self-Interest
        }
        if (tags.HasFlag(ItemTag.Creative))
        {
            // Creative can imply more Intellect or Spontaneity (thinking outside the box).
            AdjustPersonality(PersonalityAxis.Intellect_Instinct, -pointValue); // More Intellect
            AdjustPersonality(PersonalityAxis.Spontaneity_Planning, -pointValue); // More Spontaneity
        }
    }

    private void AdjustPersonality(PersonalityAxis axis, float amount)
    {
        if (playerProfile.personalityValues.TryGetValue(axis, out float currentValue))
        {
            float newValue = Mathf.Clamp(currentValue + amount, -1f, 1f);

            // --- DEBUG LINE 7 ---
            Debug.Log($"<color=yellow>[PersonalityObserver]</color> Adjusting {axis}. Old value: {currentValue}, New value: {newValue}");

            playerProfile.personalityValues[axis] = newValue;

            OnPersonalityChanged?.Invoke();
        }
    }
}