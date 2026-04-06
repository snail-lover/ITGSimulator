using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(fileName = "NewReceptorTag", menuName = "Data/Receptor Tag")]
    public class ReceptorTagDefinition : ScriptableObject
    {
        [TextArea] public string description;
    }
}