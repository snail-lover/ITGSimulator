// --- START OF FILE Action_SimpleInteract.cs ---
using UnityEngine;

public class Action_SimpleInteract : MonoBehaviour, IInteractableAction
{
    [Header("Simple Interaction")]
    public AudioClip soundEffect;
    public string stateToSetOnInteract;
    public bool valueToSet = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void ExecuteAction()
    {
        Debug.Log($"[{gameObject.name}] Performing simple interaction.");
        if (soundEffect != null)
        {
            audioSource.PlayOneShot(soundEffect);
        }

        if (!string.IsNullOrEmpty(stateToSetOnInteract) && WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.SetGlobalFlag(stateToSetOnInteract, valueToSet);
        }
    }

    public void ResetAction()
    {
        // No cleanup needed.
    }
}