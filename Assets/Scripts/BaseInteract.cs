using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BaseInteract : MonoBehaviour, IClickable
{
    private GameObject currentHoverText;
    public GameObject hoverTextPrefab; 
    private RectTransform hoverTextRect;
    public float interactionRange = 2f;
    private bool isPlayerMovingToInteract = false;
    public AudioClip soundEffect;
    private AudioSource audioSource;

    // Initialize AudioSource in Awake to ensure it's ready earlier
    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = 0.5f;
    }

    public void OnClick()
    {
        if (PointAndClickMovement.currentTarget != null && (object)PointAndClickMovement.currentTarget != this)
        {
            PointAndClickMovement.currentTarget.ResetInteractionState();
        }

        PointAndClickMovement.currentTarget = this;
        MovePlayerToInteractable();
    }

    private void Update()
    {
        if (isPlayerMovingToInteract)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && 
                Vector3.Distance(player.transform.position, transform.position) <= interactionRange)
            {
                Interact();
                ResetInteractionState(); // Ensure state is reset
            }
        }
        if (currentHoverText != null)
        {
            Vector2 mousePos = Input.mousePosition;
            hoverTextRect.position = mousePos + new Vector2(10f, 10f); // Offset slightly
        }
    }

    public virtual void ResetInteractionState()
    {
        isPlayerMovingToInteract = false;
        PointAndClickMovement.currentTarget = null;
    }

    private void MovePlayerToInteractable()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            NavMeshAgent playerAgent = player.GetComponent<NavMeshAgent>();
            if (playerAgent != null && !isPlayerMovingToInteract)
            {
                playerAgent.SetDestination(transform.position);
                isPlayerMovingToInteract = true;
            }
        }
    }

    public virtual void Interact()
    {
        Debug.Log($"{gameObject.name} interacted with!");

        // Stop player movement
        GameObject.FindGameObjectWithTag("Player")
            ?.GetComponent<NavMeshAgent>()
            ?.ResetPath();

        // Play sound effect only if AudioSource exists
        if (soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundEffect);
            Debug.Log($"Playing sound: {soundEffect.name}");
        }
        else
        {
            Debug.LogWarning("AudioSource or soundEffect missing!");
        }
    }

    public void WhenHovered()
    {
        if (currentHoverText == null && hoverTextPrefab != null)
        {
            currentHoverText = Instantiate(hoverTextPrefab, UnityEngine.Object.FindFirstObjectByType<Canvas>().transform);
            hoverTextRect = currentHoverText.GetComponent<RectTransform>();

            Text textComponent = currentHoverText.GetComponent<Text>();
            if (textComponent == null)
            {
                Debug.LogError("Hover text prefab is missing a Text component!");
                return;
            }
            textComponent.text = gameObject.name;
        }
    }

    public void HideHover()
    {
        if (currentHoverText != null)
        {
            Destroy(currentHoverText);
            currentHoverText = null;
        }
    }
    



}