// File: Game.Gameplay/AI/AIInteractionDefinition.cs
using UnityEngine;
using Game.Core;
using BehaviorDesigner.Runtime; // Requires Assembly Reference

namespace Game.Gameplay
{
    [System.Serializable]
    public struct AIInteractionData
    {
        [Header("Utility")]
        [Tooltip("What need does this fix?")]
        public NeedDefinition satisfiesNeed;

        [Tooltip("How much does it fix? (e.g. 20)")]
        public float recoveryAmount;

        [Header("Execution")]
        [Tooltip("The Behavior Tree to run (e.g., EatSandwich, SleepInBed)")]
        public ExternalBehavior behaviorTree;

        [Tooltip("Where does the NPC stand? If null, uses object transform.")]
        public Transform interactionPoint;
    }
}