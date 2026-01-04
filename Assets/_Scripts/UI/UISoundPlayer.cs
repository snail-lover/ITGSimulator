using UnityEngine;
using UnityEngine.UI;

// This component ensures an AudioSource is available for playing sounds.
[RequireComponent(typeof(AudioSource))]
public class UISoundPlayer : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound; // Optional, but good to have!

    private AudioSource audioSource;

    private void Awake()
    {
        // Get the AudioSource component.
        audioSource = GetComponent<AudioSource>();

        // It's best practice to have UI sounds ignore global pitch/volume changes.
        audioSource.ignoreListenerPause = true;
        audioSource.ignoreListenerVolume = true;

        // Make sure the source doesn't play on start.
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// This public method will be called by the EventTrigger when the pointer enters the button.
    /// </summary>
    public void PlayHoverSound()
    {
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    /// <summary>
    /// This public method can be called by a Button's OnClick event or an EventTrigger.
    /// </summary>
    public void PlayClickSound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}