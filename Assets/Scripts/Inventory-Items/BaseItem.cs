using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BaseItem : MonoBehaviour, IClickable
{



    [Header("Item Data")]
    public CreateInventoryItem item;



    [Header("Configuration")]
    [Tooltip("A UNIQUE ID for this specific item instance in the world. This is crucial for the save system. E.g., 'Key_From_Guard_Tower' or 'Apple_On_Kitchen_Table'.")]
    public string worldItemID;
    public float pickupRange = 2f;


    private Transform playerTransform; // Renamed for clarity
    private NavMeshAgent playerAgent;
    private Coroutine pickupCoroutine;

    private void Awake()
    {
        // --- CRITICAL SAVE SYSTEM CHECK ---
        // If this item's ID is empty, warn the developer. It's required for saving.
        if (string.IsNullOrEmpty(worldItemID))
        {
            Debug.LogError($"[{gameObject.name}] This item has no 'World Item ID' set! It will not save correctly when picked up.", this);
        }

        // Ask the WorldDataManager if this item has already been picked up in a previous session.
        if (WorldDataManager.Instance != null && WorldDataManager.Instance.IsWorldItemPickedUp(worldItemID))
        {
            // If it has, destroy it immediately before it can do anything else.
            Debug.Log($"[{worldItemID}] has already been picked up. Destroying this instance.");
            Destroy(gameObject);
            return; // Stop execution of the rest of this script's initialization.
        }
    }


    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerAgent = playerObject.GetComponent<NavMeshAgent>();
        }
        else
        {
            Debug.LogError($"[{name}] Player object not found! BaseItem cannot initialize.", gameObject);
        }
    }

    public void OnClick()
    {
        Debug.Log($"[{name}] OnClick received. PointAndClickMovement.currentTarget is {((MonoBehaviour)PointAndClickMovement.currentTarget)?.name}. Is this item already being approached (pickupCoroutine not null)? {pickupCoroutine != null}");

        if (playerTransform == null || playerAgent == null)
        {
            Debug.LogError($"[{name}] Player/Agent not found, cannot process item click.");
            return;
        }

        // If this item is already the current target of PointAndClickMovement AND
        // its pickupCoroutine is already running (meaning we're already approaching it)
        if ((object)PointAndClickMovement.currentTarget == this && pickupCoroutine != null)
        {
            Debug.Log($"[{name}] Re-clicked while already approaching this item. Ensuring approach continues.");
            // The existing coroutine is already running and checking PointAndClickMovement.currentTarget.
            // No need to restart it or reset destination.
            // PointAndClickMovement.LockPlayerApproach(this) would have already been called.
            return; // Do nothing further, let the existing coroutine handle it.
        }

        // If we reach here, it's either a fresh click, or a re-click when not already in an active approach coroutine for THIS item.
        // PointAndClickMovement has already set itself to this item as currentTarget.

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        if (distance <= pickupRange)
        {
            Debug.Log($"[{name}] Player already in range. Picking up immediately.");
            AttemptPickup(); // This will also handle UnlockPlayerApproach
        }
        else
        {
            Debug.Log($"[{name}] Player out of range. Moving player to item.");
            MovePlayerToItem(); // This starts the coroutine and locks approach
        }
    }

    private void MovePlayerToItem()
    {
        if (playerAgent == null || !PointAndClickMovement.Instance) return;

        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }

        StartCoroutine(MovePlayerToItemCoroutine());
    }

    private IEnumerator MovePlayerToItemCoroutine()
    {
        // Stop any current path and wait a frame to let the agent reset
        if (playerAgent.hasPath)
        {
            playerAgent.ResetPath();
            yield return null; // Wait one frame
        }

        // Use PointAndClickMovement to set destination so visuals work
        PointAndClickMovement.Instance.SetPlayerDestination(transform.position, true /*isProgrammaticCall*/);
        PointAndClickMovement.Instance.LockPlayerApproach(this);

        pickupCoroutine = StartCoroutine(CheckDistanceAndPickupCoroutine());
    }

    public void ResetInteractionState() // Called by PointAndClickMovement if player clicks elsewhere
    {
        Debug.Log($"[{name}] ResetInteractionState called.");
        if (this == null || !gameObject.activeInHierarchy) return;

        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
            Debug.Log($"[{name}] Stopped pickup coroutine.");
        }

        // If this item was the one causing the player to approach, unlock the approach
        if ((object)PointAndClickMovement.currentTarget == this) // Check against the global target
        {
            PointAndClickMovement.Instance?.UnlockPlayerApproach();
            // PointAndClickMovement will null its currentTarget when ground/new interactable is clicked.
        }
    }

    // Renamed from Pickup to avoid confusion with the public Pickup() that might be called elsewhere
    private void AttemptPickup()
    {
        if ((object)PointAndClickMovement.currentTarget == this && Inventory.Instance != null)
        {
            Debug.Log($"[{name}] Attempting to add item to inventory.");
            Inventory.Instance.AddItem(item);

            // --- NEW: TELL THE WORLD DATA MANAGER THIS ITEM IS GONE ---
            if (WorldDataManager.Instance != null && !string.IsNullOrEmpty(worldItemID))
            {
                WorldDataManager.Instance.MarkWorldItemAsPickedUp(worldItemID);
            }

            PointAndClickMovement.Instance?.UnlockPlayerApproach();
            PointAndClickMovement.currentTarget = null;
            PointAndClickMovement.Instance?.StopPlayerMovementAndNotify();

            Destroy(gameObject);
        }

        // We only attempt pickup if this item is still the global target,
        // ensuring that if the player clicked away, we don't pick it up.
        if ((object)PointAndClickMovement.currentTarget == this && Inventory.Instance != null)
        {
            Debug.Log($"[{name}] Attempting to add item to inventory.");
            Inventory.Instance.AddItem(item);

            PointAndClickMovement.Instance?.UnlockPlayerApproach(); // Unlock approach as interaction is complete
            PointAndClickMovement.currentTarget = null; // Clear global target

            // It's good practice to notify PointAndClickMovement that its movement was effectively "completed" or "cancelled"
            // so visuals can clear if they haven't already due to arrival.
            PointAndClickMovement.Instance?.StopPlayerMovementAndNotify();


            Destroy(gameObject);
        }
        else if (Inventory.Instance == null)
        {
            Debug.LogError($"[{name}] Inventory.Instance is null. Cannot pick up item.");
        }
        else
        {
            Debug.LogWarning($"[{name}] Pickup attempt aborted. Not current target or target changed. Current PnC Target: {((MonoBehaviour)PointAndClickMovement.currentTarget)?.name}");
        }
    }

    private IEnumerator CheckDistanceAndPickupCoroutine()
    {
        if (playerTransform == null || playerAgent == null)
        {
            Debug.LogError($"[{name}] Player agent or transform is null in CheckDistanceAndPickupCoroutine.");
            if ((object)PointAndClickMovement.currentTarget == this) ResetInteractionState();
            yield break;
        }

        Debug.Log($"[{name}] Starting CheckDistanceAndPickupCoroutine for target: {name}. Destination set to: {playerAgent.destination}");

        // We expect SetPlayerDestination to have been called immediately before this coroutine starts.
        // Give the agent one frame to process that.
        yield return null;

        // After one frame, let's see the state.
        Debug.Log($"[{name}] After 1 frame yield: PathPending={playerAgent.pathPending}, HasPath={playerAgent.hasPath}, IsStopped={playerAgent.isStopped}, Velocity={playerAgent.velocity.magnitude}, RemainingDistance={playerAgent.remainingDistance}, Destination={playerAgent.destination}");

        if (!playerAgent.pathPending && !playerAgent.hasPath)
        {
            // If, after one frame, it's neither pending nor has a path, something is fundamentally wrong with reaching the destination.
            NavMeshPath testPath = new NavMeshPath();
            bool pathFound = NavMesh.CalculatePath(playerAgent.transform.position, transform.position, NavMesh.AllAreas, testPath);
            Debug.LogError($"[{name}] CRITICAL: No path and not pending after initial yield. NavMesh.CalculatePath to {transform.position} found: {pathFound}, Status: {testPath.status}. Cancelling pickup.");
            ResetInteractionState();
            yield break;
        }

        // --- Main Approach Loop ---
        float stuckTimer = 0f;
        const float maxStuckTimeWhilePathing = 3.0f;
        const float maxStuckTimeNoPathAfterInitial = 1.5f;

        while ((object)PointAndClickMovement.currentTarget == this &&
               Vector3.Distance(playerTransform.position, transform.position) > pickupRange)
        {
            if (playerAgent.pathPending)
            {
                stuckTimer = 0f;
            }
            else if (playerAgent.hasPath)
            {
                if (playerAgent.velocity.sqrMagnitude > 0.01f)
                {
                    stuckTimer = 0f;
                }
                else
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > maxStuckTimeWhilePathing)
                    {
                        Debug.LogWarning($"[{name}] Player agent seems stuck (has path, no velocity). Cancelling approach.");
                        ResetInteractionState();
                        yield break;
                    }
                }
            }
            else // No path and not pending
            {
                stuckTimer += Time.deltaTime;
                Debug.LogWarning($"[{name}] Coroutine: Path lost during approach. Stuck timer: {stuckTimer}");
                if (stuckTimer > maxStuckTimeNoPathAfterInitial)
                {
                    Debug.LogWarning($"[{name}] Player agent has lost path. Cancelling.");
                    ResetInteractionState();
                    yield break;
                }
            }
            yield return null;
        }

        // Final pickup logic
        if ((object)PointAndClickMovement.currentTarget == this &&
            Vector3.Distance(playerTransform.position, transform.position) <= pickupRange)
        {
            Debug.Log($"[{name}] Player arrived in pickup range. Attempting pickup.");
            if (playerAgent.isOnNavMesh)
            {
                PointAndClickMovement.Instance?.StopPlayerMovementAndNotify();
            }
            AttemptPickup();
        }
        else if ((object)PointAndClickMovement.currentTarget == this)
        {
            Debug.LogWarning($"[{name}] Coroutine ended, still target, but NOT in pickup range. Distance: {Vector3.Distance(playerTransform.position, transform.position)}. This indicates an issue with stuck detection or range. Resetting.");
            ResetInteractionState();
        }
        else
        {
            Debug.Log($"[{name}] Pickup coroutine ended because target changed. PnC Target: {((MonoBehaviour)PointAndClickMovement.currentTarget)?.name}");
            // ResetInteractionState for this item should have been called by PointAndClickMovement when the target changed.
        }
        pickupCoroutine = null;
    }

}
