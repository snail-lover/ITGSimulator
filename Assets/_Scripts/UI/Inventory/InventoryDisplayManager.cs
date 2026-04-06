using UnityEngine;

namespace Game.UI
{
    // This line ensures that if you add this script to an object, 
    // it automatically adds an AudioSource component if one is missing.
    [RequireComponent(typeof(AudioSource))] 
    public class InventoryDisplayManager : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Drag the InventoryPanel Prefab here")]
        public GameObject inventoryPrefab;

        [Tooltip("The Canvas where the UI will be spawned")]
        public Transform uiCanvas;

        [Header("Audio Settings")] // <--- NEW SECTION
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;

        private GameObject _activeInventoryInstance;
        private AudioSource _audioSource; // <--- NEW REFERENCE

        void Awake()
        {
            // Get the AudioSource attached to this GameObject
            _audioSource = GetComponent<AudioSource>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && _activeInventoryInstance != null)
            {
                CloseInventory();
            }
        }

        public void ToggleInventory()
        {
            if (_activeInventoryInstance == null)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }

        private void OpenInventory()
        {
            if (_activeInventoryInstance != null) return;

            // Play Open Sound
            PlaySound(openSound); // <--- NEW

            _activeInventoryInstance = Instantiate(inventoryPrefab, uiCanvas);
        }

        private void CloseInventory()
        {
            if (_activeInventoryInstance != null)
            {
                // Play Close Sound
                PlaySound(closeSound); // <--- NEW

                Game.Core.UIEvents.ForceCloseContextMenu();
                Destroy(_activeInventoryInstance);
                _activeInventoryInstance = null;
            }
        }

        // Helper method to play sound safely
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                // PlayOneShot allows sounds to overlap (so opening/closing fast sounds natural)
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}