using UnityEngine;

namespace Game.Core
{
    // Right-click > Create > Game > State Definition
    // Example: Create one named "Locked", one named "Health"
    [CreateAssetMenu(menuName = "Game/State Definition")]
    public class StateDefinition : ScriptableObject
    {
        [TextArea] public string description;
    }
}