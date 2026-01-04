// AttractionEngine.cs
using UnityEngine;
using System.Collections.Generic; // Make sure this is included for Dictionaries

/// <summary>
/// A static utility class that contains the core logic for the attraction system.
/// </summary>
public static class AttractionEngine
{
    /// <summary>
    /// Calculates a single, overall resonance score between the player and an NPC.
    /// Good for general relationship level checks.
    /// </summary>
    /// <returns>A float between 0.0 (no resonance) and 1.0 (perfect resonance).</returns>
    public static float CalculateResonance(PlayerPersonalityProfile playerProfile, NpcConfig npcConfig)
    {
        if (npcConfig.valueSystem == null || npcConfig.valueSystem.Count == 0)
        {
            return 0f;
        }

        float totalWeightedScore = 0f;
        float totalImportanceSum = 0f;

        foreach (var preference in npcConfig.valueSystem)
        {
            if (playerProfile.personalityValues.TryGetValue(preference.axis, out float playerValue))
            {
                float difference = Mathf.Abs(playerValue - preference.npcTraitValue);
                float similarity = 1 - (difference / 2f);
                totalWeightedScore += similarity * preference.importance;
                totalImportanceSum += preference.importance;
            }
        }

        if (totalImportanceSum == 0)
        {
            return 0f;
        }

        float finalResonance = totalWeightedScore / totalImportanceSum;
        return finalResonance;
    }

    /// <summary>
    /// Gets a detailed breakdown of resonance for each of the NPC's important personality axes.
    /// This is perfect for triggering specific dialogue or events based on WHY the player and NPC connect.
    /// </summary>
    /// <param name="playerProfile">The player's current personality data.</param>
    /// <param name="npcConfig">The NPC's configuration data.</param>
    /// <returns>A Dictionary mapping each of the NPC's valued axes to its 0-1 resonance score. Axes the NPC doesn't care about are omitted.</returns>
    public static Dictionary<PersonalityAxis, float> GetPerAxisResonance(PlayerPersonalityProfile playerProfile, NpcConfig npcConfig)
    {
        var perAxisScores = new Dictionary<PersonalityAxis, float>();

        if (npcConfig.valueSystem == null || playerProfile == null)
        {
            return perAxisScores; // Return an empty dictionary
        }

        // Loop through each of the NPC's core personality traits.
        foreach (var preference in npcConfig.valueSystem)
        {
            // Find the player's corresponding personality value.
            if (playerProfile.personalityValues.TryGetValue(preference.axis, out float playerValue))
            {
                // Calculate the similarity for THIS SPECIFIC AXIS (ignoring importance for the raw score)
                float difference = Mathf.Abs(playerValue - preference.npcTraitValue);
                float similarity = 1 - (difference / 2f);

                // Add the specific score for this axis to our results.
                perAxisScores[preference.axis] = similarity;
            }
        }

        return perAxisScores;
    }
}