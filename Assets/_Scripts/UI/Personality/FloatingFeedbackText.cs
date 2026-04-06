using UnityEngine;
using TMPro;
using System.Collections;

namespace Game.UI
{
    public class FloatingFeedbackText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text textLabel;
        
        [Header("Settings")]
        [SerializeField] private float floatSpeed = 50f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private AnimationCurve fadeCurve; // Optional: Make it fade nicely

        public void Setup(string textToDisplay, Color color)
        {
            textLabel.text = textToDisplay;
            textLabel.color = color;
            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Color startColor = textLabel.color;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / lifetime;

                // 1. Float Up
                transform.position = startPos + (Vector3.up * floatSpeed * percent);

                // 2. Fade Out (using alpha)
                float alpha = 1f;
                if (fadeCurve != null && fadeCurve.length > 0)
                    alpha = fadeCurve.Evaluate(percent);
                else
                    alpha = 1f - percent; // Linear fade default

                textLabel.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}