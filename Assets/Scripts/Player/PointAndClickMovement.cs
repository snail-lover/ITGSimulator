// --- START OF FILE PointAndClickMovement.cs (Refactored) ---

using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public static PointAndClickMovement Instance { get; private set; }

    [Header("Setup")]
    public Camera mainCamera;
    public LayerMask clickableLayers;
    private NavMeshAgent agent;

    // --- State ---
    public static IClickable currentTarget;
    private bool isPlayerInputLocked = false; // True during cutscenes/dialogue
    private bool isApproachingTarget = false; // True when moving programmatically towards a target
    private bool isMovingToGroundPoint = false; // True when the last command was a ground click
    private Vector3 currentGroundDestination;

    // --- Events ---
    public static event System.Action<Vector3> OnMoveCommandIssued;
    public static event System.Action OnMovementStoppedOrCancelled;
    public static event System.Action<IClickable> OnNewTargetSet;
    public static event System.Action OnTargetCleared;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError("[PointClick] NavMeshAgent component missing from Player.");
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) Debug.LogError("[PointClick] Main camera not found.");
    }

    private void Start()
    {
        // (No changes to Start, OnEnable, OnDisable, CaptureStateBeforeSave)
        if (WorldDataManager.Instance != null)
        {
            Vector3 savedPosition = WorldDataManager.Instance.saveData.playerState.lastKnownPosition;
            if (savedPosition != Vector3.zero)
            {
                agent.enabled = false;
                transform.position = savedPosition;
                agent.enabled = true;
                Debug.Log($"[PointClick] Player position loaded: {transform.position}");
            }
        }
    }

    private void OnEnable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave += CaptureStateBeforeSave;
        }
    }

    private void OnDisable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave -= CaptureStateBeforeSave;
        }
    }

    private void CaptureStateBeforeSave()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.saveData.playerState.lastKnownPosition = transform.position;
        }
    }

    public NavMeshAgent GetPlayerAgent() => agent;

    void Update()
    {
        HandleMovementInput();

        // Check for arrival at a ground point
        if (isMovingToGroundPoint && agent != null && agent.hasPath && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                isMovingToGroundPoint = false;
                OnMovementStoppedOrCancelled?.Invoke();
            }
        }
    }

    // =========================================================================
    // --- THIS IS THE FULLY REFACTORED AND SIMPLIFIED CLICK HANDLING LOGIC ---
    // =========================================================================
    private void HandleMovementInput()
    {
        if (!Input.GetMouseButtonDown(0) || mainCamera == null) return;

        if (isPlayerInputLocked)
        {
            //Debug.Log("[PointClick] Click ignored: Player input is locked.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
        {
            // The new system is much simpler. We just check for the IClickable interface.
            // This works for Interactable.cs, Draggable.cs, NpcController.cs, etc.
            IClickable newClickedTarget = hit.collider.GetComponent<IClickable>();

            if (newClickedTarget != null)
            {
                // --- CASE 1: We clicked an interactable object ---
                isMovingToGroundPoint = false;

                if (isApproachingTarget && newClickedTarget == currentTarget)
                {
                    // This is a re-click on the target we are already approaching.
                    // We pass the click along to the target, which might use it to
                    // register a drag intent (like Draggable.cs does).
                    Debug.Log($"[PointClick] Re-clicked current target: {((MonoBehaviour)currentTarget)?.name}.");
                    currentTarget?.OnClick();
                }
                else
                {
                    // This is a click on a new target, or the first click.
                    // Cancel any previous interaction.
                    if (currentTarget != null)
                    {
                        Debug.Log($"[PointClick] New target clicked. Cancelling old interaction with {((MonoBehaviour)currentTarget)?.name}.");
                        currentTarget.ResetInteractionState();
                    }

                    // Set the new target and tell it to start its interaction process.
                    currentTarget = newClickedTarget;
                    OnNewTargetSet?.Invoke(currentTarget);
                    Debug.Log($"[PointClick] New target set: {((MonoBehaviour)currentTarget)?.name}.");
                    currentTarget.OnClick();
                }
            }
            else
            {
                // --- CASE 2: We clicked the ground (or a non-interactive object) ---

                // If we were interacting with anything, cancel it.
                if (currentTarget != null)
                {
                    //Debug.Log($"[PointClick] Ground clicked. Resetting previous target: {((MonoBehaviour)currentTarget)?.name}.");
                    currentTarget.ResetInteractionState();
                    currentTarget = null;
                    OnTargetCleared?.Invoke();
                }

                // Issue a standard move command to the clicked point.
                SetPlayerDestination(hit.point, false);
            }
        }
    }

    public void SetPlayerDestination(Vector3 destination, bool isProgrammaticCall)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (isProgrammaticCall || !isPlayerInputLocked)
        {
            agent.SetDestination(destination);
            OnMoveCommandIssued?.Invoke(destination);

            isMovingToGroundPoint = !isProgrammaticCall;
            currentGroundDestination = isProgrammaticCall ? Vector3.zero : destination;
        }
    }

    public void StopPlayerMovementAndNotify()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            isMovingToGroundPoint = false;
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
            OnMovementStoppedOrCancelled?.Invoke();
        }
    }

    public void HardLockPlayerMovement()
    {
        if (!isPlayerInputLocked)
        {
            isPlayerInputLocked = true;
            isApproachingTarget = false;
            StopPlayerMovementAndNotify();
        }
    }

    public void HardUnlockPlayerMovement()
    {
        if (isPlayerInputLocked)
        {
            isPlayerInputLocked = false;
        }
    }

    public void LockPlayerApproach(IClickable target)
    {
        if (isPlayerInputLocked)
        {
            Debug.LogWarning($"[PointClick] LockPlayerApproach called for {((MonoBehaviour)target)?.name}, but input is already HARD LOCKED.");
        }

        isApproachingTarget = true;
        currentTarget = target; // Ensure current target is set.
        isMovingToGroundPoint = false;

        // Quietly stop any previous movement path
        if (agent.hasPath)
        {
            agent.ResetPath();
        }
    }

    public void UnlockPlayerApproach()
    {
        if (isApproachingTarget)
        {
            isApproachingTarget = false;
            // The responsibility for stopping movement is now on the IClickable itself
            // when it finishes or is cancelled, so this method is much simpler.
        }
    }
}