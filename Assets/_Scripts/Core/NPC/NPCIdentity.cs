using UnityEngine;

namespace Game.Core
{
    public class NPCIdentity : MonoBehaviour
    {
        [Tooltip("Unique ID for this NPC. Used by dialogue picker and relationship system. e.g. 'viktor', 'sophie'")]
        public string npcID;
    }
}