using UnityEngine;
using Game.Gameplay; // To access Action_Inspect

namespace Game.UI
{
    public class BarkDisplayManager : MonoBehaviour
    {
        [Header("Setup")]
        public BarkBubble barkPrefab;
        public Transform playerTransform; // Assign automatically or manually

        [Header("Settings")]
        public Vector3 spawnOffset = new Vector3(0, 2.0f, 0);

        private BarkBubble _currentBark;

        void OnEnable()
        {
            // Listen to the Inspect Action
            // Make sure Action_Inspect has the: public static event Action<string> OnInspectTriggered;
            Action_Inspect.OnInspectTriggered += ShowPlayerThought;
        }

        void OnDisable()
        {
            Action_Inspect.OnInspectTriggered -= ShowPlayerThought;
        }

        void Start()
        {
            if (playerTransform == null)
            {
                var input = FindFirstObjectByType<PlayerInput>();
                if (input != null) playerTransform = input.transform;
            }
        }

        public void ShowPlayerThought(string text)
        {
            if (playerTransform == null) return;

            // 1. If a bark is already showing, destroy it so they don't overlap
            if (_currentBark != null)
            {
                Destroy(_currentBark.gameObject);
            }

            // 2. Instantiate new bark
            // We spawn it in the world, not as a child of the player (to prevent rotation jitter)
            _currentBark = Instantiate(barkPrefab, playerTransform.position + spawnOffset, Quaternion.identity);

            // 3. Setup text
            _currentBark.Setup(text);

            // 4. Make it follow the player (Optional, simple parent constraint logic)
            // Since we instantiate in world space, let's attach a simple follower script
            // OR simpler: just parent it to the player, but rely on BarkBubble to handle rotation.
            _currentBark.transform.SetParent(playerTransform);
            _currentBark.transform.localPosition = spawnOffset;
        }
    }
}