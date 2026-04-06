using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Game.Core; // Listen to Core

namespace Game.UI
{
    public class ScreenFXManager : MonoBehaviour
    {
        public static ScreenFXManager Instance { get; private set; }

        [Header("References")]
        public Image flashOverlay;
        private Coroutine _currentFlashRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            if (flashOverlay != null) 
            {
                // Ensure invisible on start
                var c = flashOverlay.color; c.a = 0; flashOverlay.color = c;
            }
        }

        // --- NEW: Subscribe to Event ---
        void OnEnable()
        {
            GameEvents.OnRequestScreenFlash += TriggerWhiteFlash;
        }

        void OnDisable()
        {
            GameEvents.OnRequestScreenFlash -= TriggerWhiteFlash;
        }

        // Changed signature to match the Action<float, float>
        private void TriggerWhiteFlash(float duration, float intensity)
        {
            if (flashOverlay == null) return;
            if (_currentFlashRoutine != null) StopCoroutine(_currentFlashRoutine);
            _currentFlashRoutine = StartCoroutine(FlashRoutine(duration, intensity));
        }

        private IEnumerator FlashRoutine(float duration, float maxAlpha)
        {
            // ... (Keep existing Flash Routine logic) ...
            Color color = flashOverlay.color;
            color.a = maxAlpha;
            flashOverlay.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = 1 - (elapsed / duration); 
                color.a = percent * maxAlpha;
                flashOverlay.color = color;
                yield return null;
            }
            color.a = 0;
            flashOverlay.color = color;
        }
    }
}