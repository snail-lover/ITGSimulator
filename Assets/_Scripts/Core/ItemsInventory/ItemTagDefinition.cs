using UnityEngine;

namespace Game.Core
{
    // Right-click > Create > Game > Definitions > Item Tag
    [CreateAssetMenu(fileName = "NewTag", menuName = "Game/Definitions/Item Tag")]
    public class ItemTagDefinition : ScriptableObject
    {
        [TextArea] public string description;
    }
}