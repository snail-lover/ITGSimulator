using UnityEngine;
using UnityEngine.Events;
using Game.Core;

namespace Game.Gameplay
{
    [RequireComponent(typeof(SmartObject))]
    public class SmartObjectReactor : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Which state definition are we listening for?")]
        public StateDefinition stateToWatch;

        [Tooltip("If the state is 1, OnActive fires. If 0, OnInactive fires.")]
        public bool invertLogic = false;

        [Header("Reactions")]
        public UnityEvent OnActive;   // e.g., Turn on Fire, Play Sound
        public UnityEvent OnInactive; // e.g., Stop Fire, Stop Sound

        private SmartObject _smartObject;

        void Awake()
        {
            _smartObject = GetComponent<SmartObject>();
        }

        void Start()
        {
            // initialize visuals based on current data
            if (_smartObject != null && stateToWatch != null)
            {
                UpdateVisuals(_smartObject.GetState(stateToWatch));
            }
        }

        void OnEnable()
        {
            if (_smartObject != null) _smartObject.OnStateChanged += HandleStateChange;
        }

        void OnDisable()
        {
            if (_smartObject != null) _smartObject.OnStateChanged -= HandleStateChange;
        }

        private void HandleStateChange(StateDefinition state, int value)
        {
            if (state == stateToWatch)
            {
                UpdateVisuals(value);
            }
        }

        private void UpdateVisuals(int value)
        {
            bool isActive = (value == 1);
            if (invertLogic) isActive = !isActive;

            if (isActive) OnActive.Invoke();
            else OnInactive.Invoke();
        }
    }
}