using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class BaseInteract : MonoBehaviour, IClickable
{
    [Header("Interaction")]
    public float interactionRange = 2f;
    public AudioClip soundEffect;

    [Header("Hover Text")]
    public GameObject hoverTextPrefab;
    private GameObject currentHoverText;
    private RectTransform hoverTextRect;

    // Cached References
    private AudioSource audioSource;
    private GameObject playerObject;
    private NavMeshAgent playerAgent;
    private Canvas mainCanvas;
    private Coroutine moveToInteractCoroutine;

    protected virtual void Awake()
    {
        //Cache Player References
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerAgent = playerObject.GetComponent<NavMeshAgent>();
            if (playerAgent == null) { Debug.LogError($"[{gameObject.name}] Player '{playerObject.name}' missing NavMeshAgent."); }
        }
        else { Debug.LogError($"[{gameObject.name}] Player object with tag 'Player' not found."); }

        //Cache Canvas Reference
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) { Debug.LogError($"[{gameObject.name}] Canvas not found for hover text."); }

        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); }
        audioSource.volume = 0.5f;
        audioSource.playOnAwake = false;
    }

    //IClickable Implementation 

    public virtual void OnClick()
    {
        if (PointAndClickMovement.currentTarget != null && PointAndClickMovement.currentTarget != this)
        {
            Debug.Log($"[{gameObject.name}] Resetting previous target: {((MonoBehaviour)PointAndClickMovement.currentTarget).name}");
            PointAndClickMovement.currentTarget.ResetInteractionState();
        }

        Debug.Log($"[{gameObject.name}] Clicked. Setting as current target.");
        PointAndClickMovement.currentTarget = this;
        StartInteractionProcess();
    }

    public virtual void ResetInteractionState()
    {
        if (moveToInteractCoroutine != null)
        {
            Debug.Log($"[{gameObject.name}] Resetting State: Stopping movement coroutine.");
            StopCoroutine(moveToInteractCoroutine);
            moveToInteractCoroutine = null;
        }

        // Clear the global target *only if we are still the target*
        if (PointAndClickMovement.currentTarget == this)
        {
            Debug.Log($"[{gameObject.name}] Resetting State: Clearing global target.");
            PointAndClickMovement.currentTarget = null;
        }
        else
        {
            // Debug.Log($"[{gameObject.name}] Resetting State: Was not the current global target.");
        }

        HideHover();
    }

    public virtual void WhenHovered()
    {
        if (hoverTextPrefab == null || mainCanvas == null) return;
        if (currentHoverText == null)
        {
            currentHoverText = Instantiate(hoverTextPrefab, mainCanvas.transform);
            hoverTextRect = currentHoverText.GetComponent<RectTransform>();
            Text textComponent = currentHoverText.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = string.IsNullOrEmpty(gameObject.name) ? "Interactable" : gameObject.name;
                UpdateHoverTextPosition();
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Hover text prefab missing Text component!");
                Destroy(currentHoverText); currentHoverText = null;
            }
        }
    }

    public virtual void HideHover()
    {
        if (currentHoverText != null)
        {
            Destroy(currentHoverText);
            currentHoverText = null;
            hoverTextRect = null;
        }
    }

    //Interaction Logic

    private void StartInteractionProcess()
    {
        if (playerObject == null || playerAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] Player/Agent not found. Cannot interact.");
            ResetInteractionState();
            return;
        }

        float distance = Vector3.Distance(playerObject.transform.position, transform.position);
        Debug.Log($"[{gameObject.name}] StartInteractionProcess. Distance: {distance}, Range: {interactionRange}");

        if (distance <= interactionRange)
        {
            Debug.Log($"[{gameObject.name}] Already in range. Interacting immediately.");
            Interact();
            // Interaction complete immediately, reset state
            ResetInteractionState();
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Out of range. Moving player.");
            MovePlayerToInteractable();
        }
    }

    private void MovePlayerToInteractable()
    {
        if (playerAgent != null && moveToInteractCoroutine == null)
        {
            Debug.Log($"[{gameObject.name}] Setting destination: {transform.position}");
            if (!playerAgent.SetDestination(transform.position))
            {
                 Debug.LogError($"[{gameObject.name}] SetDestination failed! Is NavMesh baked correctly near target?");
                 ResetInteractionState(); // Abort if path can't be set
                 return;
            }
            moveToInteractCoroutine = StartCoroutine(CheckDistanceAndInteract());
            Debug.Log($"[{gameObject.name}] Started CheckDistanceAndInteract coroutine.");
        }
        else if (playerAgent == null) { ResetInteractionState(); }
        else if (moveToInteractCoroutine != null) { Debug.LogWarning($"[{gameObject.name}] MovePlayerToInteractable called while coroutine already running."); }
    }

    private IEnumerator CheckDistanceAndInteract()
    {
        // Wait a frame to ensure path calculation has begun
        yield return null;
        Debug.Log($"[{gameObject.name}] Coroutine: Starting wait loop.");

        bool arrived = false;
        float currentDistance;

        // Loop while this object is the target AND the agent is valid AND we haven't arrived
        while (PointAndClickMovement.currentTarget == this && playerAgent != null && !arrived)
        {
            // Check if path is still calculating
            if (playerAgent.pathPending)
            {
                // Debug.Log($"[{gameObject.name}] Coroutine: Path Pending..."); // Can be spammy
                yield return null; // Wait if path is calculating
                continue;
            }

            // Check if the agent has stopped unexpectedly or the path is invalid
            // (Check velocity alongside remainingDistance to ensure it's truly stopped)
            if (!playerAgent.hasPath && playerAgent.velocity.sqrMagnitude < 0.01f)
            {
                Debug.LogWarning($"[{gameObject.name}] Coroutine: Agent stopped prematurely or path invalid. Breaking loop.");
                // Log agent status for diagnostics
                // Debug.Log($"Agent Status: pathStatus={playerAgent.pathStatus}, hasPath={playerAgent.hasPath}, remainingDistance={playerAgent.remainingDistance}, velocity={playerAgent.velocity.magnitude}");
                break; // Exit loop, interaction will fail below
            }

            // Check distance
            currentDistance = Vector3.Distance(playerObject.transform.position, transform.position);
            // Debug.Log($"[{gameObject.name}] Coroutine: Checking distance: {currentDistance}"); // Can be spammy

            if (currentDistance <= interactionRange)
            {
                Debug.Log($"[{gameObject.name}] Coroutine: Arrived within range ({currentDistance} <= {interactionRange}).");
                arrived = true; // Set flag to exit loop
            }
            else
            {
                // Still moving, wait for the next frame
                yield return null;
            }
        }
        Debug.Log($"[{gameObject.name}] Coroutine: Exited wait loop. Arrived: {arrived}, Is Target: {PointAndClickMovement.currentTarget == this}");


        // --- Post-Loop Check ---
        bool interactionAttempted = false;
        // Check if we arrived successfully AND are still the intended target
        if (arrived && PointAndClickMovement.currentTarget == this)
        {
             Debug.Log($"[{gameObject.name}] Coroutine: Conditions met. Calling Interact().");
             interactionAttempted = true;
            try
            {
                Interact(); // Call the interaction logic
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[{gameObject.name}] Coroutine: Error during Interact(): {e.Message}\n{e.StackTrace}");
            }
        }
        else if (!arrived && PointAndClickMovement.currentTarget == this)
        {
             Debug.LogWarning($"[{gameObject.name}] Coroutine: Loop finished but player didn't arrive in range or agent stopped.");
        }
        else if (PointAndClickMovement.currentTarget != this)
        {
            Debug.Log($"[{gameObject.name}] Coroutine: Target changed before interaction could occur.");
        }


        // --- Cleanup ---
         Debug.Log($"[{gameObject.name}] Coroutine: Reached cleanup. Current target is { (PointAndClickMovement.currentTarget == null ? "null" : ((MonoBehaviour)PointAndClickMovement.currentTarget).name) }");
        // Ensure state is reset *if this interaction attempt just finished*
        // This check prevents resetting if a *new* interaction started elsewhere
        // while this coroutine was somehow still finishing up its last frame.
        if (interactionAttempted || PointAndClickMovement.currentTarget == this)
        {
            Debug.Log($"[{gameObject.name}] Coroutine: Calling ResetInteractionState from coroutine cleanup.");
            ResetInteractionState();
        }
        else {
             Debug.Log($"[{gameObject.name}] Coroutine: Skipping ResetInteractionState in cleanup as target changed or interaction wasn't attempted.");
        }

        // Ensure handle is cleared specifically when coroutine exits
        moveToInteractCoroutine = null;
         Debug.Log($"[{gameObject.name}] Coroutine: Finished execution.");
    }

    // --- Core Interaction Action ---

    public virtual void Interact()
    {
        Debug.Log($"[{gameObject.name}] Interact() called!");

        // Stop player agent reliably
        if (playerAgent != null)
        {
            Debug.Log($"[{gameObject.name}] Interact: Stopping player agent.");
            // playerAgent.isStopped = true; // Force stop
            playerAgent.ResetPath(); // Clear the path immediately
             playerAgent.velocity = Vector3.zero; // Kill velocity
        }

        // Play sound effect
        if (soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundEffect);
        }
        else if (soundEffect == null) { /* Optional Warning */ }
        else if (audioSource == null) { Debug.LogWarning($"[{gameObject.name}] AudioSource missing for sound '{soundEffect.name}'."); }

        // Derived classes will add their specific interaction logic here.
        // IMPORTANT: Do NOT call ResetInteractionState() within Interact() itself
        // unless the interaction is absolutely instantaneous and synchronous.
        // The coroutine handles resetting state after Interact() finishes or fails.
    }

    // --- Update Loop (For Hover Text) ---

    protected virtual void Update()
    {
        UpdateHoverTextPosition();
    }

    private void UpdateHoverTextPosition()
    {
        if (currentHoverText != null && hoverTextRect != null)
        {
            Vector2 mousePos = Input.mousePosition;
            hoverTextRect.position = mousePos + new Vector2(15f, 15f); // Offset
        }
    }
}