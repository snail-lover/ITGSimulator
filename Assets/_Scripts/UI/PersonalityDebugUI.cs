// PersonalityDebugUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Game.Gameplay;



/// <summary>
/// A debug window that visualizes the player's personality profile in real-time.
/// It dynamically creates UI elements for each axis and subscribes to the
/// PersonalityObserver's OnPersonalityChanged event to update itself.
/// </summary>
public class PersonalityDebugUI : MonoBehaviour
{
    // --- THIS FIELD IS REMOVED ---
    // [SerializeField] private PlayerPersonalityProfile playerProfile;

    // We will get the profile from the PlayerData manager instead.
    private PlayerPersonalityProfile playerProfile;

    [Header("UI Prefabs & Parent")]
    [Tooltip("A prefab for a single UI row (containing a label and a slider).")]
    [SerializeField] private GameObject axisDisplayPrefab;
    [Tooltip("The container transform where the UI rows will be instantiated.")]
    [SerializeField] private Transform displayParent;

    private Dictionary<PersonalityAxis, Slider> uiSliders = new Dictionary<PersonalityAxis, Slider>();

    private void OnEnable()
    {
        // Subscribe to the event when the UI becomes active
        // A safety check is good here in case the observer hasn't awoken yet.
        if (PersonalityObserver.Instance != null)
        {
            PersonalityObserver.OnPersonalityChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        // IMPORTANT: Always unsubscribe when the UI is disabled to prevent errors
        if (PersonalityObserver.Instance != null)
        {
            PersonalityObserver.OnPersonalityChanged -= UpdateUI;
        }
    }

    private void Start()
    {
        // --- THIS IS THE CRITICAL FIX ---
        // Get the single, correct profile instance from the central PlayerData manager.
        if (PlayerData.Instance != null)
        {
            playerProfile = PlayerData.Instance.Profile;
        }

        if (playerProfile == null || axisDisplayPrefab == null || displayParent == null)
        {
            Debug.LogError("PersonalityDebugUI is not fully configured or could not find the PlayerProfile from PlayerData.Instance!", this);
            this.enabled = false;
            return;
        }

        // Add this line just to be 100% sure we're subscribing if we missed the first OnEnable call
        PersonalityObserver.OnPersonalityChanged -= UpdateUI; // Unsubscribe first to prevent double-subscribing
        PersonalityObserver.OnPersonalityChanged += UpdateUI;

        CreateInitialUI();
    }

    // In this version, also make sure sliders are NOT interactable
    private void CreateInitialUI()
    {
        foreach (Transform child in displayParent) { Destroy(child.gameObject); }
        uiSliders.Clear();

        foreach (var kvp in playerProfile.personalityValues)
        {
            GameObject newRow = Instantiate(axisDisplayPrefab, displayParent);
            newRow.name = $"{kvp.Key}_Display";
            TextMeshProUGUI label = newRow.GetComponentInChildren<TextMeshProUGUI>();
            Slider slider = newRow.GetComponentInChildren<Slider>();

            if (label != null) { label.text = kvp.Key.ToString().Replace("_", " <-> "); }
            if (slider != null)
            {
                slider.minValue = -1f;
                slider.maxValue = 1f;
                slider.interactable = false; // Explicitly disable interaction
                slider.SetValueWithoutNotify(kvp.Value); // Use SetValueWithoutNotify here too
                uiSliders[kvp.Key] = slider;
            }
        }
    }

    private void UpdateUI()
    {
        // For debugging, let's confirm this is being called
        Debug.Log("<color=lime>[PersonalityDebugUI]</color> Received OnPersonalityChanged event! Updating sliders.");

        if (playerProfile == null) return; // Safety check

        foreach (var kvp in playerProfile.personalityValues)
        {
            if (uiSliders.TryGetValue(kvp.Key, out Slider slider))
            {
                slider.SetValueWithoutNotify(kvp.Value);
            }
        }
    }
}