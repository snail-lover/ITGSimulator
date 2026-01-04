using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    public class LightStateReactor : MonoBehaviour
    {
        [Header("Setup")]
        public SmartObject smartObject;
        public StateDefinition stateToWatch; // "Powered"

        [Header("Visuals")]
        public Light targetLight;
        public MeshRenderer bulbRenderer;
        public Material onMat;
        public Material offMat;

        void Start()
        {
            if (smartObject == null) smartObject = GetComponent<SmartObject>();

            // 1. Listen for changes
            smartObject.OnStateChanged += HandleStateChange;

            // 2. Initialize visual state immediately
            UpdateVisuals(smartObject.GetState(stateToWatch));
        }

        void OnDestroy()
        {
            if (smartObject != null) smartObject.OnStateChanged -= HandleStateChange;
        }

        void HandleStateChange(StateDefinition state, int value)
        {
            // Only care about the specific state (e.g., "Powered")
            if (state == stateToWatch)
            {
                UpdateVisuals(value);
            }
        }

        void UpdateVisuals(int value)
        {
            bool isOn = (value == 1);

            if (targetLight != null) targetLight.enabled = isOn;

            if (bulbRenderer != null)
            {
                bulbRenderer.material = isOn ? onMat : offMat;
            }
        }
    }
}