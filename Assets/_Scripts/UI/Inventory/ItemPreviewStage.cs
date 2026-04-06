using UnityEngine;

namespace Game.UI
{
    public class ItemPreviewStage : MonoBehaviour
    {
        public static ItemPreviewStage Instance { get; private set; }

        [Tooltip("Drag the child object 'ItemPreviewSpawnPoint' here")]
        public Transform spawnPoint;

        private void Awake()
        {
            // Even if the object is disabled, if we enable it later, this sets up the link.
            // However, for this to work, the object usually needs to be Active at start or we use [RuntimeInitializeOnLoadMethod].
            // For now, assume you keep this object ACTIVE in the scene, just far away.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }
        
    }
}