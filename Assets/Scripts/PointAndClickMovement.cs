using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public static PointAndClickMovement Instance { get; private set; }

    [Header("Setup")]
    public Camera mainCamera;
    public LayerMask clickableLayers;
    private NavMeshAgent agent;

    public static IClickable currentTarget;
    private bool isPlayerInputLocked = false; // True when player cannot issue ANY move commands via clicks (Dialogue/Cutscene).
    private bool isApproachingTarget = false; // True when player is programmatically moving towards currentTarget (e.g. NPC).

    public static event System.Action<Vector3> OnMoveCommandIssued;
    public static event System.Action OnMovementStoppedOrCancelled;

    private bool isMovingToGroundPoint = false; // Tracks if last command was to ground
    private Vector3 currentGroundDestination; // <<< --- ADDED THIS DECLARATION

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError("[PointClick] NavMeshAgent component missing from Player.");
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("[PointClick] Main camera not found. Please assign in Inspector or tag a camera 'MainCamera'.");
    }

    public NavMeshAgent GetPlayerAgent() => agent;

    void Update()
    {
        HandleMovementInput();

        if (isMovingToGroundPoint && agent != null && agent.hasPath && !agent.pathPending)
        {
            // Using a slightly more generous check for ground points.
            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f || (currentGroundDestination - transform.position).sqrMagnitude < 0.05f)
            {
                // Debug.Log("[PointClick] Player arrived at ground destination. Stopping visuals.");
                isMovingToGroundPoint = false;
                OnMovementStoppedOrCancelled?.Invoke();
                // Optional: agent.ResetPath();
            }
        }
    }

    private void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isPlayerInputLocked)
            {
                Debug.Log("[PointClick] Click ignored, player input is (hard) locked.");
                return;
            }

            if (mainCamera == null || agent == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                IClickable newClickedInteractable = hit.collider.GetComponent<IClickable>();

                if (newClickedInteractable != null) // Clicked on an IClickable (NPC, Item)
                {
                    isMovingToGroundPoint = false;

                    if (isApproachingTarget)
                    {
                        if (newClickedInteractable == currentTarget)
                        {
                            Debug.Log($"[PointClick] Re-clicked current approach target ({((MonoBehaviour)currentTarget)?.name ?? "null"}). Letting it handle.");
                            currentTarget?.OnClick();
                        }
                        else
                        {
                            Debug.Log($"[PointClick] Clicked new target ({((MonoBehaviour)newClickedInteractable)?.name ?? "null"}) while approaching ({((MonoBehaviour)currentTarget)?.name ?? "null"}). Cancelling old, starting new.");
                            currentTarget?.ResetInteractionState();

                            currentTarget = newClickedInteractable;
                            currentTarget?.OnClick();
                        }
                    }
                    else
                    {
                        if (currentTarget != null && currentTarget != newClickedInteractable && !(currentTarget as UnityEngine.Object == null))
                        {
                            Debug.Log($"[PointClick] New target ({((MonoBehaviour)newClickedInteractable).name}). Resetting old ({((MonoBehaviour)currentTarget).name}).");
                            currentTarget.ResetInteractionState();
                        }
                        currentTarget = newClickedInteractable;
                        Debug.Log($"[PointClick] Standard click on target: {((MonoBehaviour)currentTarget).name}");
                        currentTarget.OnClick();
                    }
                }
                else // Clicked on Ground
                {
                    Debug.Log($"[PointClick] Clicked on ground at {hit.point}. CurrentTarget: {((MonoBehaviour)currentTarget)?.name}, IsApproaching: {isApproachingTarget}");

                    if (isApproachingTarget && currentTarget != null)
                    {
                        Debug.Log($"[PointClick] Ground click is cancelling approach to {((MonoBehaviour)currentTarget).name}.");
                        currentTarget.ResetInteractionState();
                        // The NPC's ResetInteractionState should call UnlockPlayerApproach()
                    }
                    else if (currentTarget != null) // Had a non-approaching target, just deselect
                    {
                        Debug.Log($"[PointClick] Ground click. Had target {((MonoBehaviour)currentTarget).name}, resetting it.");
                        currentTarget.ResetInteractionState();
                    }

                    currentTarget = null;
                    SetPlayerDestination(hit.point, false); // Player input, not programmatic
                    // isMovingToGroundPoint and currentGroundDestination are set inside SetPlayerDestination now
                }
            }
        }
    }

    public void SetPlayerDestination(Vector3 destination, bool isProgrammaticCall)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("[PointClick] Agent not available or not on NavMesh to set destination.");
            return;
        }

        if (isProgrammaticCall || !isPlayerInputLocked)
        {
            agent.SetDestination(destination);
            OnMoveCommandIssued?.Invoke(destination);

            isMovingToGroundPoint = !isProgrammaticCall; // True only if it's a direct ground click by player
            if (isProgrammaticCall)
            {
                currentGroundDestination = Vector3.zero; // Not relevant for programmatic moves
            }
            else
            {
                currentGroundDestination = destination; // Store for ground click arrival check
            }
            // Debug.Log($"[PointClick] SetPlayerDestination. Programmatic: {isProgrammaticCall}, Dest: {destination}, IsMovingToGround: {isMovingToGroundPoint}");
        }
        else
        {
            Debug.LogWarning($"[PointClick] SetPlayerDestination for {destination} blocked. Programmatic: {isProgrammaticCall}, PlayerInputLocked: {isPlayerInputLocked}");
        }
    }

    public void StopPlayerMovementAndNotify()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            bool wasMoving = agent.hasPath || agent.pathPending;
            isMovingToGroundPoint = false;
            agent.ResetPath();
            // Only fire event if we actually stopped something or intended to stop.
            // if (wasMoving) // Or always fire to ensure visuals are cleared. Let's try always.
            // {
            OnMovementStoppedOrCancelled?.Invoke();
            // }
            if (wasMoving) Debug.Log("[PointClick] Player movement and path explicitly reset.");
        }
    }

    public void HardLockPlayerMovement()
    {
        if (!isPlayerInputLocked)
        {
            Debug.Log("[PointClick] Player Movement Input HARD LOCKED (e.g., Dialogue/Cutscene).");
            isPlayerInputLocked = true;
            isApproachingTarget = false;
            StopPlayerMovementAndNotify();
        }
    }

    public void HardUnlockPlayerMovement()
    {
        if (isPlayerInputLocked)
        {
            Debug.Log("[PointClick] Player Movement Input HARD UNLOCKED.");
            isPlayerInputLocked = false;
        }
    }

    public void LockPlayerApproach(IClickable target)
    {
        // It's possible a hard lock is already in place (e.g. dialogue starting then NPC moves player slightly)
        // In that case, we don't want to override the hard lock's control, but we still note the approach.
        if (isPlayerInputLocked)
        {
            Debug.LogWarning($"[PointClick] LockPlayerApproach called for {((MonoBehaviour)target)?.name}, but player input is already HARD LOCKED. Approach will proceed programmatically.");
        }

        Debug.Log($"[PointClick] Player Approach soft-LOCKED for target {((MonoBehaviour)target).name}.");
        isApproachingTarget = true;
        currentTarget = target;

        bool wasPreviouslyMovingToGround = isMovingToGroundPoint;
        isMovingToGroundPoint = false;

        if (agent.hasPath && wasPreviouslyMovingToGround)
        {
            StopPlayerMovementAndNotify(); // Properly stop and notify if interrupting a ground move
        }
        else if (agent.hasPath)
        {
            agent.ResetPath(); // Quietly stop other paths
            Debug.Log("[PointClick] Player agent path reset (quietly) because NPC is taking control of movement.");
        }
    }

    public void UnlockPlayerApproach()
    {
        if (isApproachingTarget)
        {
            Debug.Log($"[PointClick] Player Approach UNLOCKED from target {((MonoBehaviour)currentTarget)?.name}.");
            isApproachingTarget = false;
            // If no new command is issued immediately, and we were approaching, the visuals should stop.
            // Check if player should be idle or if a ground click already re-targeted.
            // If currentTarget is now null (due to ground click during approach), visuals would have been handled.
            // If currentTarget is still the NPC (e.g. NPC reached player), and no new movement, notify.
            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance))
            {
                // Debug.Log("[PointClick] Approach unlocked and player is not actively pathing. Firing stop event for visuals.");
                // OnMovementStoppedOrCancelled?.Invoke(); // This might be too aggressive / cause flicker.
                // The PlayerApproachCoroutine ending in BaseNPC
                // OR the player clicking ground (which calls ResetInteractionState on NPC)
                // should be the primary drivers for stopping visuals/agent.
            }
        }
    }
}