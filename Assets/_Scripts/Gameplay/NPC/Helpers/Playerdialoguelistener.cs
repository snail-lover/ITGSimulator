using UnityEngine;

namespace Game.Gameplay
{
    public class PlayerDialogueListener : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private PlayerMotor _motor;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _motor = GetComponent<PlayerMotor>();
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
            _playerInput.LockInput();

            // Stop movement - facing and snapping handled by NPCDialogueListener
            if (_motor != null)
                _motor.SetMoveDirection(Vector3.zero);
        }

        private void OnDialogueEnded()
        {
            _playerInput.UnlockInput();
        }
    }
}