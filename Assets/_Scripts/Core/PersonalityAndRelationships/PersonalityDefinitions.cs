using UnityEngine;

namespace Game.Core
{
    public enum PersonalityAxis
    {
        Order,      // Lawful vs Chaotic
        Diligence,  // Academic vs Slacker
        Social      // Romantic vs Sigma
    }

    public enum TraitType
    {
        // Axis: Order
        Lawful, 
        Chaotic,

        // Axis: Diligence
        Academic, 
        Slacker,

        // Axis: Social
        Romantic, // (Simp)
        Sigma     // (Gigachad/Aloof)
    }

    [System.Serializable]
    public struct TraitShift
    {
        [Tooltip("Which trait are we feeding?")]
        public TraitType type;
        
        [Tooltip("How much intensity? (Standard is usually 5-10)")]
        [Range(0, 50)]
        public float amount;
    }
}