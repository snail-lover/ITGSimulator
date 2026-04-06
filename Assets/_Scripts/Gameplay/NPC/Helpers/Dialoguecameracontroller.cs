using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Attach to the same GameObject as CameraFollow.
    /// Transitions to an over-the-shoulder shot when dialogue starts.
    /// </summary>
    public class DialogueCameraController : MonoBehaviour
    {
        [Header("Over-Shoulder Settings")]
        public float backOffset = 2.5f;
        public float sideOffset = 0.8f;
        public float heightOffset = 1.6f;
        public float transitionDuration = 0.6f;
        [Tooltip("Tilt the camera down (positive = look down more)")]
        public float pitchAngle = 10f;

        private CameraFollow _cameraFollow;
        private Transform _playerTransform;

        private void Awake()
        {
            _cameraFollow = GetComponent<CameraFollow>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void OnEnable()
        {
            DialogueEvents.OnDialogueStarted += OnDialogueStarted;
            DialogueEvents.OnDialogueEnded += OnDialogueEnded;
        }

        private void OnDisable()
        {
            DialogueEvents.OnDialogueStarted -= OnDialogueStarted;
            DialogueEvents.OnDialogueEnded -= OnDialogueEnded;
        }

        private void OnDialogueStarted(Transform npcTransform)
        {
            if (_playerTransform == null || npcTransform == null) return;

            Vector3 directionToNPC = (npcTransform.position - _playerTransform.position).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, directionToNPC);

            Vector3 cameraPosition = _playerTransform.position
                - directionToNPC * backOffset
                + right * sideOffset
                + Vector3.up * heightOffset;

            // Look at a point between player and NPC
            Vector3 lookTarget = Vector3.Lerp(_playerTransform.position, npcTransform.position, 0.6f)
                                 + Vector3.up * 1.2f;

            Quaternion baseLook = Quaternion.LookRotation(lookTarget - cameraPosition);
            Quaternion cameraRotation = baseLook * Quaternion.Euler(pitchAngle, 0, 0);

            _cameraFollow.SetManualControl(true);
            _cameraFollow.TransitionToViewCoroutine(cameraPosition, cameraRotation, transitionDuration);
        }

        private void OnDialogueEnded()
        {
            _cameraFollow.SetManualControl(false);
        }
    }
}