using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbienceManager : MonoBehaviour
{
    public static AmbienceManager Instance;

    private AudioSource _audioSource;
    private Coroutine _fadeJob;

    [Header("Settings")]
    public float fadeDuration = 2.0f;

    void Awake()
    {
        // Singleton Pattern: There can only be one Manager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _audioSource = GetComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f; // Force 2D
    }

    public void SetAmbience(AudioClip newClip)
    {
        // Don't restart if it's the same clip
        if (_audioSource.clip == newClip) return;

        if (_fadeJob != null) StopCoroutine(_fadeJob);
        _fadeJob = StartCoroutine(FadeTo(newClip));
    }

    private IEnumerator FadeTo(AudioClip newClip)
    {
        float startVolume = _audioSource.volume;
        
        // 1. Fade Out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
            yield return null;
        }
        _audioSource.volume = 0f;

        // 2. Swap Clip
        _audioSource.clip = newClip;
        _audioSource.Play();

        // 3. Fade In
        // (If newClip is null, we just stay silent)
        if (newClip != null)
        {
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                _audioSource.volume = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }
            _audioSource.volume = 1f;
        }
    }
}