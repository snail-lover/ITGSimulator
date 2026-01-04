using UnityEngine;
using TMPro;

namespace Game.UI
{
    public class BarkBubble : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI textComponent;

        [Header("Settings")]
        public float lifetime = 3f;
        public Vector3 offset = new Vector3(0, 2.2f, 0); // Height above player head

        private Transform _cameraTransform;

        void Awake()
        {
            _cameraTransform = Camera.main.transform;

            // Auto-destroy after lifetime
            Destroy(gameObject, lifetime);
        }

        public void Setup(string text)
        {
            textComponent.text = text;
        }

        void LateUpdate()
        {
            // Billboard Effect: Always face the camera
            if (_cameraTransform != null)
            {
                // Rotate the UI to look at the camera
                // We use LookRotation(forward, up) to keep it upright
                transform.rotation = Quaternion.LookRotation(
                    transform.position - _cameraTransform.position
                );
            }
        }
    }
}