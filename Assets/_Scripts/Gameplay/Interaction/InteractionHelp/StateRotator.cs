using UnityEngine;
using System.Collections;
using Game.Core;

namespace Game.Gameplay
{
    // Listens to a SmartObject state and rotates a transform accordingly.
    [RequireComponent(typeof(SmartObject))]
    public class StateRotator : MonoBehaviour
    {
        [Header("Settings")]
        public StateDefinition stateToWatch; // Drag 'State_IsOpen' here
        public Transform targetTransform;    // The actual hinge/mesh to rotate

        [Header("Rotation")]
        public Vector3 closedRotation;       // e.g., (0, 0, 0)
        public Vector3 openRotation;         // e.g., (0, 90, 0)
        public float duration = 1.0f;
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip openSound;
        public AudioClip closeSound;

        private SmartObject _smartObj;
        private Coroutine _moveRoutine;

        void Awake()
        {
            _smartObj = GetComponent<SmartObject>();
        }

        void OnEnable()
        {
            _smartObj.OnStateChanged += HandleStateChange;
        }

        void OnDisable()
        {
            _smartObj.OnStateChanged -= HandleStateChange;
        }

        void Start()
        {
            // Set initial position instantly without sound
            int currentState = _smartObj.GetState(stateToWatch);
            SetRotation(currentState == 1, false);
        }

        private void HandleStateChange(StateDefinition state, int value)
        {
            if (state == stateToWatch)
            {
                // 1 = Open, 0 = Closed
                bool isOpen = (value == 1);
                SetRotation(isOpen, true);
            }
        }

        private void SetRotation(bool isOpen, bool playSound)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            
            Vector3 targetRot = isOpen ? openRotation : closedRotation;
            _moveRoutine = StartCoroutine(RotateRoutine(targetRot));

            if (playSound && audioSource != null)
            {
                audioSource.PlayOneShot(isOpen ? openSound : closeSound);
            }
        }

        private IEnumerator RotateRoutine(Vector3 targetEuler)
        {
            Quaternion startRot = targetTransform.localRotation;
            Quaternion endRot = Quaternion.Euler(targetEuler);
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = movementCurve.Evaluate(timer / duration);
                targetTransform.localRotation = Quaternion.Lerp(startRot, endRot, t);
                yield return null;
            }

            targetTransform.localRotation = endRot;
        }
    }
}