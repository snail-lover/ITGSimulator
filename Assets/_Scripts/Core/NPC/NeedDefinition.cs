// File: Game.Core/Definitions/NeedDefinition.cs
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Definitions/Need")]
    public class NeedDefinition : ScriptableObject
    {
        public string needName;
        
        [Tooltip("How fast this need decays per second naturally.")]
        public float decayRate = 0.5f;

        [Tooltip("If this reaches 0, is it critical?")]
        public bool isCritical;
    }
}