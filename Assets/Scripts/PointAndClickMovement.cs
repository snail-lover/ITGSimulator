using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    [Header("Setup")]
    public Camera mainCamera; // Assign in the Inspector
    public LayerMask clickableLayers; // Layers for ground AND interactables
    private NavMeshAgent agent;
    private IClickable lastHovered;

    public static IClickable currentTarget; // Tracks the current interaction target (NPC, Item, etc.)
    private bool isMovementLocked = false; //locks movement input

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing from Player.");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Fallback to Camera.main
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found or not tagged 'MainCamera'. Please assign 'mainCamera' in the Inspector.");
            }
        }
    }

    void Update()
    {
        // Only handle movement input if not locked
        if (!isMovementLocked)
        {
            HandleMovementInput();
        }

        // Hover effects can update even if movement is locked
        HandleHoverEffects();
    }

    private void HandleMovementInput()
    {
        // Check for left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            // Ensure camera and agent are valid
            if (mainCamera == null || agent == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers)) // Use combined layer mask
            {
                // --- Case 1: Clicked on an Interactable ---
                IClickable clickable = hit.collider.GetComponent<IClickable>();
                if (clickable != null)
                {
                    // Before checking currentTarget, make sure it's a valid Unity object
                    // Use the Unity == null check
                    bool currentTargetIsValid = currentTarget != null && !(currentTarget as UnityEngine.Object == null);

                    // Check if it's a new target AND the old target was valid and different
                    if (currentTargetIsValid && currentTarget != clickable)
                    {
                         // Cast to MonoBehaviour *after* checking it's valid for accessing .name
                         Debug.Log($"[PointClick] New target clicked ({((MonoBehaviour)clickable).name}). Resetting old target ({(currentTarget as UnityEngine.Object).name}).");
                        currentTarget.ResetInteractionState(); // Reset the previous target's state
                        currentTarget = null; // Explicitly clear the reference *after* resetting the old state
                    }
                    else if (currentTargetIsValid && currentTarget == clickable)
                    {
                         // Re-clicking the *same* valid target. Ignore.
                         Debug.Log($"[PointClick] Re-clicked current target ({((MonoBehaviour)clickable).name}). Ignoring.");
                         return;
                    }
                    // Note: If currentTargetIsValid is false (old target was destroyed),
                    // we just proceed to set the new target below without trying to reset the old one.


                    // Set the new target (This happens if old target was null/destroyed OR was different)
                    currentTarget = clickable;
                     Debug.Log($"[PointClick] Initiating OnClick for target: {((MonoBehaviour)clickable).name}");
                    clickable.OnClick();
                }
                // --- Case 2: Clicked on Ground (or non-IClickable) ---
                else
                {
                     Debug.Log($"[PointClick] Clicked on ground/non-interactable at {hit.point}");

                    // If we were previously targeting something, cancel it, ONLY if it's still a valid object
                    bool currentTargetIsValid = currentTarget != null && !(currentTarget as UnityEngine.Object == null);
                    if (currentTargetIsValid)
                    {
                         Debug.Log($"[PointClick] Cancelling interaction with previous target: {((MonoBehaviour)currentTarget).name}");
                        currentTarget.ResetInteractionState();
                    }
                    // Always clear the target reference when clicking ground or a non-clickable object
                    currentTarget = null;

                    // Move the player to the clicked point on the ground
                     if (agent != null) { // Added agent null check for safety
                        agent.SetDestination(hit.point);
                     }
                }
            }
        }
    }

    public void LockMovement() /// Locks player movement input (e.g., when dialogue starts), called externally, typically by DialogueManager.
    {
        if (!isMovementLocked)
        {
             Debug.Log("[PointClick] Movement Locked.");
            isMovementLocked = true;
        }
    }
    public void UnlockMovement() ///Unlocks player movement input (e.g., when dialogue ends), called externally, typically by DialogueManager.
    {
        if (isMovementLocked)
        {
             Debug.Log("[PointClick] Movement Unlocked.");
            isMovementLocked = false;
        }
    }
    public void EndInteraction() // Public wrapper called by DialogueManager or other systems to signal interaction end
    {
        UnlockMovement();
    }

    private void HandleHoverEffects()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        int interactableLayerMask = LayerMask.GetMask("Interactable");
        // Fallback if "Interactable" layer doesn't exist, use the clickable layers mask
        if (interactableLayerMask == 0 || interactableLayerMask != (interactableLayerMask | (1 << gameObject.layer))) /// Check if layer exists/is valid
        { 
             interactableLayerMask = ~0; // Raycast against everything if no specific layer
        }


        if (Physics.Raycast(ray, out RaycastHit hoverHit, Mathf.Infinity, interactableLayerMask))
        {
            // Need to get component here because the mask might be broad now
            IClickable interactable = hoverHit.collider.GetComponent<IClickable>();
            if (interactable != null)
            {
                // Show hover on the new interactable
                interactable.WhenHovered();

                // If we hovered onto a *different* interactable than the last frame
                // And the lastHovered is still a valid object
                if (lastHovered != null && !(lastHovered as UnityEngine.Object == null) && interactable != lastHovered)
                {
                    lastHovered.HideHover(); // Hide hover on the old one
                }
                 else if (lastHovered != null && (lastHovered as UnityEngine.Object == null))
                 {
                     // If lastHovered was destroyed, just clear the reference
                     lastHovered = null;
                 }
                lastHovered = interactable; // Update the last hovered
            }
            else
            {
                // Hit something but it wasn't an IClickable. Hide hover if one was active.
                if (lastHovered != null && !(lastHovered as UnityEngine.Object == null))
                {
                    lastHovered.HideHover();
                }
                lastHovered = null; // Clear reference
            }
        }
        else
        {
            // Raycast didn't hit anything. Hide hover if one was active.
            if (lastHovered != null && !(lastHovered as UnityEngine.Object == null))
            {
                lastHovered.HideHover();
            }
            lastHovered = null; // Clear reference
        }
    }
}