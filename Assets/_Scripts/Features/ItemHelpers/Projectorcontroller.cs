using UnityEngine;
using UnityEngine.Video;
using Game.Core;

namespace Game.Gameplay
{
    /// <summary>
    /// Drives a VideoPlayer + projection screen in response to a SmartObject state.
    ///
    /// SCENE SETUP:
    ///   [Projector GameObject]
    ///     ├── SmartObject           (add State_IsOn to initialStates, value 0)
    ///     ├── ProjectorController   (this script)
    ///     ├── VideoPlayer           (Play On Awake: OFF, Audio Output: Audio Source)
    ///     └── AudioSource           (optional, for hum / click sounds)
    ///
    ///   [Screen Plane (on the wall)]
    ///     └── MeshRenderer          (assign below as screenRenderer)
    ///
    /// RENDER TEXTURE SETUP:
    ///   1. Right-click in Project → Create → Render Texture
    ///   2. Match resolution to your video (e.g. 1920x1080)
    ///   3. Assign it to: VideoPlayer.targetTexture AND this.renderTexture
    ///   4. Create a new Material for the screen, set its Base Map to the same RenderTexture
    ///   5. Assign that material to onMaterial below
    /// </summary>
    [RequireComponent(typeof(SmartObject))]
    public class ProjectorController : MonoBehaviour
    {
        [Header("State Binding")]
        [Tooltip("The state that powers the projector. Toggled by Action_ToggleState.")]
        public StateDefinition powerState;

        [Header("Video")]
        public VideoPlayer videoPlayer;

        [Header("Screen")]
        [Tooltip("The wall/screen MeshRenderer.")]
        public MeshRenderer screenRenderer;
        [Tooltip("Shown while OFF — ideally a plain dark/blank material.")]
        public Material offMaterial;
        [Tooltip("Shown while ON — must have the RenderTexture as its Base Map.")]
        public Material onMaterial;

        [Header("Projector Light (optional)")]
        [Tooltip("A Spot Light that acts as the projector beam. Will be toggled with power.")]
        public Light projectorLight;

        [Header("Audio (optional)")]
        public AudioSource audioSource;
        public AudioClip powerOnSound;
        public AudioClip powerOffSound;

        // ---------------------------------------------------------------

        private SmartObject _smartObj;

        void Awake()
        {
            _smartObj = GetComponent<SmartObject>();
        }

        void Start()
        {
            // Initialise visuals to match current state without playing sounds
            ApplyPowerState(_smartObj.GetState(powerState) == 1, playAudio: false);
        }

        void OnEnable()
        {
            _smartObj.OnStateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            _smartObj.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(StateDefinition state, int value)
        {
            if (state == powerState)
            {
                ApplyPowerState(value == 1, playAudio: true);
            }
        }

        private void ApplyPowerState(bool isOn, bool playAudio)
        {
            // --- Video ---
            if (videoPlayer != null)
            {
                if (isOn)
                    videoPlayer.Play();
                else
                {
                    videoPlayer.Stop();
                    // Rewind so next power-on starts from the beginning
                    videoPlayer.time = 0;
                }
            }

            // --- Screen material ---
            if (screenRenderer != null)
            {
                screenRenderer.material = isOn ? onMaterial : offMaterial;
            }

            // --- Light ---
            if (projectorLight != null)
            {
                projectorLight.enabled = isOn;
            }

            // --- Audio ---
            if (playAudio && audioSource != null)
            {
                AudioClip clip = isOn ? powerOnSound : powerOffSound;
                if (clip != null) audioSource.PlayOneShot(clip);
            }

            Debug.Log($"[ProjectorController] Power {(isOn ? "ON" : "OFF")}");
        }
    }
}