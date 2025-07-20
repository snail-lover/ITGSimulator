// AIDataTypes.cs

// Note: This file does NOT use "using UnityEngine;" because it only contains pure C# data structures.
// It also does NOT have a class that inherits from MonoBehaviour. It is just a container for definitions.

/// <summary>
/// Defines the possible outcomes of an AI's decision-making process.
/// </summary>
public enum AiDecisionType
{
    DoNothing,
    PerformActivity,
    SearchZone
}

/// <summary>
/// A simple struct to hold the result of the AI's decision-making process.
/// </summary>
public struct AiDecision
{
    public AiDecisionType type;
    public object targetObject;
    public string reason;

    public AiDecision(AiDecisionType type, object target = null, string reason = "")
    {
        this.type = type;
        this.targetObject = target;
        this.reason = reason;
    }
}