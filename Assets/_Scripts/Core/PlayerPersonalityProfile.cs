// PlayerPersonalityProfile.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the data for the player's personality profile.
/// It uses a Dictionary to map each PersonalityAxis to a value, making it scalable.
/// Implements ISerializationCallbackReceiver to handle saving/loading the Dictionary.
/// </summary>
[System.Serializable]
public class PlayerPersonalityProfile : ISerializationCallbackReceiver
{
    // The core data: A mapping from each axis to its current value (-1.0 to 1.0)
    public Dictionary<PersonalityAxis, float> personalityValues = new Dictionary<PersonalityAxis, float>();

    // --- Serialization Helpers ---
    // These lists are what get saved to the file, just like in your GameSaveData.
    [SerializeField] private List<PersonalityAxis> _keys = new List<PersonalityAxis>();
    [SerializeField] private List<float> _values = new List<float>();

    /// <summary>
    /// The constructor now automatically initializes all defined personality axes to a neutral 0.
    /// If you add a new axis to the enum, it will be included automatically!
    /// </summary>
    public PlayerPersonalityProfile()
    {
        // This loop iterates through all items in the PersonalityAxis enum.
        foreach (PersonalityAxis axis in Enum.GetValues(typeof(PersonalityAxis)))
        {
            personalityValues.Add(axis, 0f);
        }
    }

    public void OnBeforeSerialize()
    {
        _keys.Clear();
        _values.Clear();
        foreach (var kvp in personalityValues)
        {
            _keys.Add(kvp.Key);
            _values.Add(kvp.Value);
        }

        // --- DEBUG LINE 8 ---
        // This will only print when the object is about to be saved to JSON.
        // Use string.Join to format the list nicely.
        string valuesString = string.Join(", ", _values);
        Debug.Log($"<color=lightblue>[PlayerPersonalityProfile]</color> OnBeforeSerialize triggered. Values being written: [{valuesString}]");
    }

    public void OnAfterDeserialize()
    {
        // Rebuild the Dictionary from the Lists after loading.
        personalityValues = new Dictionary<PersonalityAxis, float>();
        for (int i = 0; i < _keys.Count; i++)
        {
            // This check prevents errors if you remove an axis from the enum later.
            if (i < _values.Count)
            {
                personalityValues.Add(_keys[i], _values[i]);
            }
        }
    }
}