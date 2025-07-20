// --- START OF FILE Interactable.cs ---

using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Collider))] // An interactable must have a collider to be clicked
public class Interactable : MonoBehaviour, IClickable
{
    [Header("Interaction Setup")]
    [Tooltip("The distance from which the player can interact with this object.")]
    public float interactionRange = 2f;

    // --- Private Fields ---
    private IInteractableAction actionToPerform;
    private Coroutine approachCoroutine;
    private Transform playerTransform;
    private NavMeshAgent playerAgent;

    private void Awake()
    {
        // Automatically find the action component on this same GameObject.
        actionToPerform = GetComponent<IInteractableAction>();
        if (actionToPerform == null)
        {
            Debug.LogError($"[{gameObject.name}] Interactable script has no action component (e.g., Action_PickupItem) that implements IInteractableAction.", this);
            this.enabled = false; // Disable if not set up correctly
        }

        GameObject playerObj = GameObject.FindWithTag("Player");
        playerTransform = playerObj?.transform;
        playerAgent = playerObj?.GetComponent<NavMeshAgent>();
    }

    /// <summary>
    /// Called by PointAndClickMovement when this object is clicked.
    /// </summary>
    public void OnClick()
    {
        if (playerTransform == null || playerAgent == null)
        {
            Debug.LogError($"[{name}] Cannot interact: Player references not found.");
            return;
        }

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance <= interactionRange)
        {
            // Already in range, execute immediately.
            ExecuteInteraction();
        }
        else
        {
            // Out of range, start the approach process.
            if (approachCoroutine != null) StopCoroutine(approachCoroutine);
            approachCoroutine = StartCoroutine(ApproachAndExecuteActionCoroutine());
        }
    }

    private IEnumerator ApproachAndExecuteActionCoroutine()
    {
        // 1. Lock the player's approach to this target. This signals to the PointAndClickMovement
        //    system that a programmatic move is in progress.
        PointAndClickMovement.Instance.LockPlayerApproach(this);

        // 2. Command the player to move. This also handles visuals like the click marker.
        PointAndClickMovement.Instance.SetPlayerDestination(transform.position, true);

        // Give the agent one frame to process the new path.
        yield return null;

        // --- Main Approach Loop (copied and generalized from BaseItem.cs) ---
        float stuckTimer = 0f;
        const float maxStuckTime = 3.0f;

        while ((object)PointAndClickMovement.currentTarget == this &&
               Vector3.Distance(playerTransform.position, transform.position) > interactionRange)
        {
            if (playerAgent.pathPending)
            {
                stuckTimer = 0f; // Reset timer while calculating path
            }
            else if (playerAgent.hasPath)
            {
                // If we have a path but are not moving, we might be stuck
                if (playerAgent.velocity.sqrMagnitude < 0.01f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > maxStuckTime)
                    {
                        Debug.LogWarning($"[{name}] Player agent seems stuck. Cancelling approach.");
                        ResetInteractionState();
                        yield break; // Exit the coroutine
                    }
                }
                else
                {
                    stuckTimer = 0f; // We are moving, so not stuck
                }
            }
            else // No path and not pending, something is wrong
            {
                Debug.LogError($"[{name}] Player agent lost its path while approaching. Cancelling.");
                ResetInteractionState();
                yield break; // Exit the coroutine
            }

            yield return null; // Wait for the next frame
        }

        // --- Loop Finished ---
        // Check if we are still the target and are now in range.
        if ((object)PointAndClickMovement.currentTarget == this &&
            Vector3.Distance(playerTransform.position, transform.position) <= interactionRange)
        {
            ExecuteInteraction();
        }
        else
        {
            // If we are here, the interaction was likely cancelled by the player clicking elsewhere.
            // ResetInteractionState() would have been called by PointAndClickMovement.
            Debug.Log($"[{name}] Approach coroutine ended because target changed or player did not arrive in range.");
        }

        approachCoroutine = null;
    }

    private void ExecuteInteraction()
    {
        // Stop the player and notify the system.
        PointAndClickMovement.Instance.StopPlayerMovementAndNotify();

        // Perform the specific action (pickup, talk, etc.).
        actionToPerform?.ExecuteAction();

        // Unlock player approach, as the interaction is now complete.
        PointAndClickMovement.Instance.UnlockPlayerApproach();

        // This Interactable has done its job.
        // The action itself (e.g., Action_PickupItem) might destroy the GameObject.
    }

    /// <summary>
    /// Called by PointAndClickMovement if the player clicks away, cancelling this interaction.
    /// </summary>
    public void ResetInteractionState()
    {
        if (approachCoroutine != null)
        {
            StopCoroutine(approachCoroutine);
            approachCoroutine = null;
        }

        // Tell the action to clean up if it needs to.
        actionToPerform?.ResetAction();

        // If we were the current target, unlock the player's approach logic.
        if ((object)PointAndClickMovement.currentTarget == this)
        {
            PointAndClickMovement.Instance.UnlockPlayerApproach();
        }
    }
}