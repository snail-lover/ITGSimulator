using System;

// This class acts as the "Save File format" for personality.
// It must be [Serializable] for JsonUtility to work.
[Serializable]
public class PlayerPersonalityProfile
{
    // We match the ranges used in PersonalityManager (-100 to 100)
    public float orderAxis;      // Lawful <-> Chaotic
    public float diligenceAxis;  // Academic <-> Slacker
    public float socialAxis;     // Romantic <-> Sigma
}