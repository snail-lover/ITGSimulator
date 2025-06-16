using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // Keep if you still use UI elements linked here

public class BaseNPC : MonoBehaviour, IClickable
{
    // --- Fields & Properties (unchanged) ---
    private static List<BaseNPC> _allActiveNpcs = new List<BaseNPC>();
    public static List<BaseNPC> AllActiveNpcs => _allActiveNpcs; // Public getter for all active NPCs

    [Header("Interaction Settings")]
    public float interactRange = 2.0f;

    [Header("Task Management")]
    public List<TaskObject> taskPool;
    [Tooltip("Delay in seconds after completing a task before choosing the next one.")]
    public float taskCompletionDelay = 2.0f;
    private Coroutine taskExecutionCoroutine;
    private TaskObject currentTask;

    [Header("NPC Movement")]
    private NavMeshAgent agent;
    [Tooltip("How quickly the NPC turns to face the task direction upon arrival.")]
    public float rotationSpeed = 5f;

    [Header("NPC Animation")]
    private Animator npcAnimator;
    [Tooltip("Name of the boolean parameter in the Animator to indicate walking.")]
    public string walkingParameterName = "IsWalking";

    [Header("NPC Info")]
    public string npcName = "NPC Name";
    public TextAsset dialogueFile;
    public int currentLove;
    public int npcAge;
    public string npcGender;
    public string npcBloodType;
    public string npcZodiacSign;
    public int npcCockLength;
    public string npcLikes;
    public string npcDislikes;
    public Sprite npcImage;

    [Header("Cutscene Settings")]
    [SerializeField] protected Cutscene finalLoveCutsceneAsset;
    private bool wasAgentStoppedBeforeCutscene;



    public NavMeshAgent Agent => agent;
    public Animator NpcAnimator => npcAnimator;

    // Cached References
    private DialogueData dialogueData;
    private Transform playerTransform;
    private NavMeshAgent playerAgent;
    private Coroutine interactionCoroutine;

    [Header("Floor Management")]
    [Tooltip("The current floor this NPC is on. Needs to be updated by game logic when NPC changes floors (e.g., via tasks).")]
    public FloorVisibilityManager.FloorLevel currentNpcFloorLevel = FloorVisibilityManager.FloorLevel.Lower;
    [Tooltip("Assign the main collider used for player interaction and general NPC presence. This will also be used for NPC-NPC collision ignoring.")]
    public Collider interactionCollider;

    private Renderer[] _npcRenderers;

    // --- State Machine ---
    public enum NpcState
    {
        None,
        Idle,
        MovingToTask,
        PerformingTaskAction,
        WaitingAfterTask,
        PlayerApproaching,
        InDialogue,
        PausedByCutscene
    }

    [Header("State Machine Debug")]
    [SerializeField] private NpcState _currentState = NpcState.None;
    public NpcState CurrentNpcState => _currentState;
    private NpcState _previousState;



    // =========================================================================
    // 1. Unity Lifecycle Methods
    // =========================================================================

    protected virtual void Awake()
    {
        CachePlayerReferences();
        InitializeNPC();
        LoadDialogueData();
        InitializeLove();

        _npcRenderers = GetComponentsInChildren<Renderer>(true);

        SetupNpcCollisionProperties();
        ApplyNpcNpcCollisionIgnores();
        _allActiveNpcs.Add(this);

        if (interactionCollider == null)
        {
            Debug.LogError($"[{npcName}] BaseNPC is missing a reference to its 'interactionCollider'. Please assign it in the Inspector. NPC-NPC collision ignoring will not work for this NPC.", this);
        }
        else
        {
            Debug.Log($"[{npcName}-Awake] Interaction Collider: {interactionCollider.name}. Initial floor: {currentNpcFloorLevel}");
        }
        Debug.Log($"[{npcName}-Awake] Cached {_npcRenderers.Length} renderers. Initial floor: {currentNpcFloorLevel}");
    }

    private void Start()
    {
        if (FloorVisibilityManager.Instance == null)
        {
            Debug.LogWarning($"[{npcName}-Start] FloorVisibilityManager.Instance is null. Initial visibility might be incorrect until FVM initializes and notifies.");
        }
        else if (!FloorVisibilityManager.Instance.isActiveAndEnabled)
        {
            Debug.LogWarning($"[{npcName}-Start] FloorVisibilityManager.Instance found, but might not have run its Start() yet. Deferring explicit visibility update slightly or relying on FVM's initial broadcast.");
        }
        else
        {
            // See comment in original code
        }

        TransitionToState(NpcState.Idle);
    }

    private void Update()
    {
        UpdateStateBehavior();
        UpdateAnimation();
    }

    protected virtual void OnDestroy()
    {
        _allActiveNpcs.Remove(this);
        if (taskExecutionCoroutine != null) // This is the NpcTaskLoopCoroutine handle
        {
            StopCoroutine(taskExecutionCoroutine);
            taskExecutionCoroutine = null;
        }
        if (interactionCoroutine != null) // This is the PlayerApproachCoroutine handle
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }
    }

    // =========================================================================
    // 2. Initialization & Caching
    // =========================================================================

    private void CachePlayerReferences()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerAgent = playerObj.GetComponent<NavMeshAgent>();
            if (playerAgent == null)
                Debug.LogError($"[{gameObject.name}] Player '{playerObj.name}' missing NavMeshAgent.");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Player object with tag 'Player' not found.");
        }
    }

    private void InitializeNPC()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError($"[{gameObject.name}] NavMeshAgent component missing from this NPC.");

        npcAnimator = GetComponent<Animator>();
        if (npcAnimator == null)
            Debug.LogWarning($"[{gameObject.name}] Animator component missing from this NPC. Tasks requiring animation won't play animations.");
    }

    private void InitializeLove()
    {
        if (dialogueData != null)
        {
            currentLove = dialogueData.startingLove;
        }
        else
        {
            currentLove = 0;
        }
    }

    private void LoadDialogueData()
    {
        if (dialogueFile == null)
        {
            Debug.LogError($"[{gameObject.name}] Dialogue File not assigned!");
            return;
        }
        try
        {
            dialogueData = JsonUtility.FromJson<DialogueData>(dialogueFile.text);
            if (dialogueData == null) throw new System.ArgumentNullException("Parsed dialogue data is null.");
            ValidateDialogueData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error parsing Dialogue JSON for {dialogueFile.name}: {e.Message}\n{e.StackTrace}");
            dialogueData = null;
        }
    }

    private void ValidateDialogueData()
    {
        if (dialogueData.nodes == null)
        {
            Debug.LogError($"[{gameObject.name}] Invalid dialogue data in {dialogueFile.name}. 'nodes' array is missing or null.");
            dialogueData = null;
            return;
        }

        dialogueData.nodeDictionary = new Dictionary<string, DialogueNode>();
        foreach (var node in dialogueData.nodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.nodeID))
            {
                if (!dialogueData.nodeDictionary.ContainsKey(node.nodeID))
                {
                    dialogueData.nodeDictionary.Add(node.nodeID, node);
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Duplicate nodeID '{node.nodeID}' found in {dialogueFile.name}. Using the first occurrence.");
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Found invalid or empty nodeID in {dialogueFile.name}. Skipping node.");
            }
        }
    }

    // =========================================================================
    // 3. State Machine (Core, Enter/Exit, State-specific, Coroutines)
    // =========================================================================

    // --- Core ---
    private void TransitionToState(NpcState newState)
    {
        if (_currentState == newState && newState != NpcState.PlayerApproaching)
        {
            Debug.LogWarning($"[{npcName}] Tried to transition to the same state: {newState}.");
            if (newState == NpcState.PlayerApproaching) HandleEnterPlayerApproachingState();
            return;
        }
        //Debug.Log($"[{npcName}] Transitioning: {_currentState} -> {newState}");
        _previousState = _currentState;
        OnExitState(_currentState);
        _currentState = newState;
        OnEnterState(_currentState);
    }

    private void OnEnterState(NpcState state)
    {
        switch (state)
        {
            case NpcState.Idle:
                HandleEnterIdleState();
                break;
            case NpcState.MovingToTask:
                HandleEnterMovingToTaskState();
                break;
            case NpcState.PerformingTaskAction:
                HandleEnterPerformingTaskActionState();
                break;
            case NpcState.WaitingAfterTask:
                HandleEnterWaitingAfterTaskState();
                break;
            case NpcState.PlayerApproaching:
                HandleEnterPlayerApproachingState();
                break;
            case NpcState.InDialogue:
                HandleEnterInDialogueState();
                break;
            case NpcState.PausedByCutscene:
                HandleEnterPausedByCutsceneState();
                break;
        }
    }

    private void OnExitState(NpcState state)
    {
        switch (state)
        {
            case NpcState.MovingToTask:
                if (_currentState != NpcState.PerformingTaskAction)
                {
                    if (agent != null && agent.isOnNavMesh) agent.ResetPath();
                }
                break;
            case NpcState.PerformingTaskAction:
                if (currentTask != null && npcAnimator != null && !string.IsNullOrEmpty(currentTask.animationBoolName))
                {
                    npcAnimator.SetBool(currentTask.animationBoolName, false);
                }
                break;
            case NpcState.PlayerApproaching:
                if (interactionCoroutine != null)
                {
                    StopCoroutine(interactionCoroutine);
                    interactionCoroutine = null;
                }
                PointAndClickMovement.Instance?.LockPlayerApproach(this); // NEW: Use the "soft unlock"
                if (_currentState != NpcState.InDialogue)
                {
                    PointAndClickMovement.Instance?.LockPlayerApproach(this);
                }
                break;
            case NpcState.InDialogue:
                PointAndClickMovement.Instance?.LockPlayerApproach(this);
                break;
            case NpcState.PausedByCutscene:
                if (agent != null && agent.isOnNavMesh && _currentState != NpcState.None)
                {
                    agent.isStopped = wasAgentStoppedBeforeCutscene;
                }
                break;
        }
    }

    private void UpdateStateBehavior()
    {
        switch (_currentState)
        {
            case NpcState.MovingToTask:
                if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    TransitionToState(NpcState.PerformingTaskAction);
                }
                break;
        }
    }

    // --- State-specific Enter Logic ---
    private void HandleEnterIdleState()
    {
        currentTask = null;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        StartNpcTaskLoopIfNeeded();
    }

    private void HandleEnterMovingToTaskState()
    {
        if (currentTask == null || agent == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{npcName}] Cannot enter MovingToTask: Invalid task or agent. Reverting to Idle.");
            TransitionToState(NpcState.Idle);
            return;
        }
        agent.SetDestination(currentTask.GetTargetPosition());
        agent.isStopped = false;
    }

    private void HandleEnterPerformingTaskActionState()
    {
        if (currentTask == null || agent == null)
        {
            Debug.LogWarning($"[{npcName}] Cannot enter PerformingTaskAction: Invalid task or agent. Reverting to Idle.");
            TransitionToState(NpcState.Idle);
            return;
        }
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
        StartCoroutine(ExecuteTaskActionCoroutine(currentTask));
    }

    private void HandleEnterWaitingAfterTaskState()
    {
        currentTask = null;
        StartCoroutine(TaskCompletionDelayCoroutine());
    }

    private void HandleEnterPlayerApproachingState()
    {
        Debug.Log($"[{npcName}] Entering PlayerApproaching state.");
        StopNpcTaskLoopAndMovement(); // Stop NPC's own tasks
        currentTask = null;

        // 1. Notify PointAndClickMovement that we are starting an approach
        PointAndClickMovement.Instance?.LockPlayerApproach(this); // NEW: Use the "soft lock"

        // 2. Command the player to move to this NPC
        if (playerTransform != null && PointAndClickMovement.Instance != null)
        {
            PointAndClickMovement.Instance.SetPlayerDestination(transform.position, true /*isProgrammaticCall*/);
            Debug.Log($"[{npcName}] Instructing player to move to my position: {transform.position}");
        }
        else
        {
            Debug.LogError($"[{npcName}] Cannot command player to move: PlayerTransform or PointAndClickMovement instance is null.");
            TransitionToState(NpcState.Idle); // Abort approach if we can't move player
            return;
        }

        // 3. Start the coroutine to monitor arrival
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(PlayerApproachCoroutine());
    }

    private void HandleEnterInDialogueState()
    {
        Debug.Log($"[{name}] HandleEnterInDialogueState. My _currentState is now InDialogue. DialogueManager.currentNPC is {DialogueManager.Instance?.GetCurrentNPC()?.name}");
        DialogueManager.Instance.StartDialogue(this);
    }

    private void HandleEnterPausedByCutsceneState()
    {
        Debug.Log($"[{npcName}] Entering PausedByCutscene state.");
        StopNpcTaskLoopAndMovement();
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
            PointAndClickMovement.Instance?.LockPlayerApproach(this);
        }
        if (agent != null && agent.isOnNavMesh)
        {
            wasAgentStoppedBeforeCutscene = agent.isStopped;
            agent.isStopped = true;
        }
    }

    // --- State Coroutines ---
    private IEnumerator ExecuteTaskActionCoroutine(TaskObject task)
    {
        if (task == null) yield break;
        Quaternion targetRotation = task.GetTargetRotation();
        float turnTimer = 0f;
        Quaternion startRotation = transform.rotation;
        while (turnTimer < 1f && Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            if (_currentState != NpcState.PerformingTaskAction) yield break;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer * rotationSpeed);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        if (_currentState == NpcState.PerformingTaskAction) transform.rotation = targetRotation;

        bool animationSet = false;
        if (npcAnimator != null && !string.IsNullOrEmpty(task.animationBoolName))
        {
            npcAnimator.SetBool(task.animationBoolName, true);
            animationSet = true;
        }

        yield return new WaitForSeconds(task.duration);

        if (animationSet && npcAnimator != null)
        {
            npcAnimator.SetBool(task.animationBoolName, false);
        }

        if (_currentState == NpcState.PerformingTaskAction)
        {
            TransitionToState(NpcState.WaitingAfterTask);
        }
    }

    private IEnumerator TaskCompletionDelayCoroutine()
    {
        yield return new WaitForSeconds(taskCompletionDelay);
        if (_currentState == NpcState.WaitingAfterTask)
        {
            TransitionToState(NpcState.Idle);
        }
    }

    private IEnumerator PlayerApproachCoroutine()
    {
        if (playerTransform == null || playerAgent == null)
        {
            Debug.LogError($"[{npcName}] Player references missing. Cannot execute PlayerApproachCoroutine.");
            if (_currentState == NpcState.PlayerApproaching) TransitionToState(NpcState.Idle);
            yield break;
        }

        Debug.Log($"[{npcName}] PlayerApproachCoroutine started for {name}. Player should be moving towards me.");

        // Give the NavMeshAgent a brief moment to process the SetDestination command
        // from PointAndClickMovement and start path calculation.
        // Waiting for end of frame can often be enough.
        yield return new WaitForEndOfFrame();
        // You could even do yield return null; twice if WaitForEndOfFrame isn't enough on its own.

        bool canInteract = false;
        float stuckTimer = 0f; // Add a timer to detect if player is truly stuck
        const float maxStuckTime = 3.0f; // Max time to wait if player agent isn't moving

        while (_currentState == NpcState.PlayerApproaching && PointAndClickMovement.currentTarget == this)
        {
            float distance = Vector3.Distance(playerTransform.position, transform.position);
            if (distance <= interactRange)
            {
                Debug.Log($"[{npcName}] Player in range. Distance: {distance}");
                canInteract = true;
                break;
            }

            // Check if player agent is actively moving or still calculating path
            if (playerAgent.pathPending || (playerAgent.hasPath && playerAgent.velocity.sqrMagnitude > 0.01f))
            {
                // Player is moving or path is calculating - GOOD
                stuckTimer = 0f; // Reset stuck timer
                                 // Debug.Log($"[{npcName}] Player agent is moving or path pending. Velocity: {playerAgent.velocity.magnitude}, PathPending: {playerAgent.pathPending}, HasPath: {playerAgent.hasPath}, RemainingDist: {playerAgent.remainingDistance}");
            }
            // Check if player has a path but isn't moving (might have arrived at an intermediate point or is blocked)
            // Only consider it "stopped prematurely" if it has a path but no velocity AND is not yet in range.
            else if (playerAgent.hasPath && playerAgent.velocity.sqrMagnitude < 0.01f && playerAgent.remainingDistance > agent.stoppingDistance)
            {
                stuckTimer += Time.deltaTime;
                // Debug.LogWarning($"[{npcName}] Player agent has path but is not moving. Velocity: {playerAgent.velocity.magnitude}, RemainingDist: {playerAgent.remainingDistance}. Stuck timer: {stuckTimer}");
                if (stuckTimer > maxStuckTime)
                {
                    Debug.LogWarning($"[{npcName}] Player agent seems stuck or destination unreachable for too long. Cancelling approach.");
                    canInteract = false;
                    break;
                }
            }
            // Check if player has NO path and is NOT moving (could be SetDestination failed or player clicked away very fast)
            // Only consider it "stopped prematurely" if it has a path but no velocity AND is not yet in range.
            else if (!playerAgent.hasPath && playerAgent.velocity.sqrMagnitude < 0.01f)
            {
                // This case is tricky. If SetDestination truly failed, we should abort.
                // If it's just the initial frames, we might have already yielded.
                // Let's add a small delay for this specific case to be sure it's not a transient state.
                // However, the initial yield return new WaitForEndOfFrame(); should help a lot.
                stuckTimer += Time.deltaTime; // Also use stuck timer here
                Debug.LogWarning($"[{npcName}] Player agent has NO path and is NOT moving. Velocity: {playerAgent.velocity.magnitude}. Stuck timer: {stuckTimer}");
                if (stuckTimer > maxStuckTime / 2f) // Shorter timeout for no path at all
                {
                    Debug.LogWarning($"[{npcName}] Player agent has no path and isn't moving. Destination likely invalid or un Treachable. Cancelling approach.");
                    canInteract = false;
                    break;
                }
            }
            else
            {
                // Default case, likely still moving or just arrived at stopping distance but not interactRange
                stuckTimer = 0f;
            }

            yield return null;
        }

        if (_currentState == NpcState.PlayerApproaching)
        {
            if (canInteract && PointAndClickMovement.currentTarget == this)
            {
                Debug.Log($"[{npcName}] Player arrived and conditions met for dialogue.");
                if (playerAgent.isOnNavMesh)
                {
                    playerAgent.ResetPath();
                    playerAgent.velocity = Vector3.zero;
                }
                TransitionToState(NpcState.InDialogue);
            }
            else
            {
                Debug.LogWarning($"[{npcName}] PlayerApproachCoroutine ended. Conditions not met for dialogue. Reverting to Idle.");
                TransitionToState(NpcState.Idle);
            }
        }
        interactionCoroutine = null;
    }

    // =========================================================================
    // 4. Task Management
    // =========================================================================

    private void AssignNextTask()
    {
        if (taskPool == null || taskPool.Count == 0)
        {
            currentTask = null;
            return;
        }
        currentTask = GetRandomTask();
    }

    private TaskObject GetRandomTask()
    {
        if (taskPool == null || taskPool.Count == 0) return null;
        return taskPool[Random.Range(0, taskPool.Count)];
    }

    private void StartNpcTaskLoopIfNeeded()
    {
        if (taskExecutionCoroutine == null && taskPool != null && taskPool.Count > 0)
        {
            taskExecutionCoroutine = StartCoroutine(NpcTaskLoopCoroutine());
        }
    }

    private void StopNpcTaskLoopAndMovement()
    {
        if (taskExecutionCoroutine != null)
        {
            StopCoroutine(taskExecutionCoroutine);
            taskExecutionCoroutine = null;
        }
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        if (currentTask != null && npcAnimator != null && !string.IsNullOrEmpty(currentTask.animationBoolName))
        {
            npcAnimator.SetBool(currentTask.animationBoolName, false);
        }
        currentTask = null;
    }

    private IEnumerator NpcTaskLoopCoroutine()
    {
        while (true)
        {
            if (_currentState == NpcState.Idle)
            {
                AssignNextTask();
                if (currentTask != null)
                {
                    TransitionToState(NpcState.MovingToTask);
                }
                else
                {
                    yield return new WaitForSeconds(taskCompletionDelay);
                }
            }
            yield return null;
        }
    }

    // =========================================================================
    // 5. Interaction & Cutscene
    // =========================================================================

    public virtual void OnClick()
    {
        Debug.Log($"[{npcName}-{_currentState}] OnClick received.");

        switch (_currentState)
        {
            case NpcState.Idle:
            case NpcState.MovingToTask:
            case NpcState.PerformingTaskAction:
            case NpcState.WaitingAfterTask:
                PointAndClickMovement.currentTarget = this;
                TransitionToState(NpcState.PlayerApproaching);
                break;

            case NpcState.PlayerApproaching:
                Debug.Log($"[{npcName}-{_currentState}] Re-clicked. Player already approaching. Restarting approach logic.");
                HandleEnterPlayerApproachingState();
                break;

            case NpcState.InDialogue:
                Debug.Log($"[{npcName}-{_currentState}] Clicked while in dialogue. Ignoring click on NPC model.");
                break;

            case NpcState.PausedByCutscene:
                Debug.Log($"[{npcName}-{_currentState}] Clicked while paused by cutscene. Ignoring.");
                break;

            default:
                Debug.LogWarning($"[{npcName}-{_currentState}] OnClick in unhandled state.");
                break;
        }
    }

    public virtual void ResetInteractionState()
    {
        Debug.Log($"[{npcName}-{_currentState}] ResetInteractionState called.");

        switch (_currentState)
        {
            case NpcState.PlayerApproaching:
            case NpcState.InDialogue:
                TransitionToState(NpcState.Idle);
                break;
        }

        if (PointAndClickMovement.currentTarget == this)
        {
            PointAndClickMovement.currentTarget = null;
        }
    }

    public virtual void TriggerFinalCutscene()
    {
        if (finalLoveCutsceneAsset != null)
        {
            Debug.Log($"[{npcName}] Triggering final love cutscene: {finalLoveCutsceneAsset.name}");
            CutsceneManager.Instance.StartCutscene(finalLoveCutsceneAsset, this);
        }
        else
        {
            Debug.LogWarning($"[{npcName}] No final love cutscene asset assigned.");
        }
    }

    public virtual void PerformCutsceneAction(string methodName)
    {
        Debug.LogWarning($"[BaseNPC] PerformCutsceneAction called with '{methodName}' but not implemented in derived class {this.GetType().Name} or BaseNPC.");
    }

    // =========================================================================
    // 6. Floor/Visibility Management
    // =========================================================================

    public void UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.FloorLevel playerCurrentFloor)
    {
        bool shouldBeVisibleAndInteractable = (this.currentNpcFloorLevel == playerCurrentFloor);

        string rendererStatusBefore = "N/A";
        if (_npcRenderers != null && _npcRenderers.Length > 0 && _npcRenderers[0] != null)
        {
            rendererStatusBefore = _npcRenderers[0].enabled ? "ENABLED" : "DISABLED";
        }
        string interactionColliderStatusBefore = "N/A";
        if (interactionCollider != null)
        {
            interactionColliderStatusBefore = interactionCollider.enabled ? "ENABLED" : "DISABLED";
        }

        //Debug.Log($"[{npcName}-UpdateVisibility] NPC on floor {currentNpcFloorLevel}, Player on {playerCurrentFloor}. ShouldBeVisibleAndInteractable: {shouldBeVisibleAndInteractable}. Main Renderer was: {rendererStatusBefore}, InteractionCollider was: {interactionColliderStatusBefore}. Renderers found: {_npcRenderers?.Length ?? 0}");

        if (_npcRenderers != null)
        {
            foreach (Renderer rend in _npcRenderers)
            {
                if (rend != null) rend.enabled = shouldBeVisibleAndInteractable;
            }
        }
        else
        {
            Debug.LogWarning($"[{npcName}-UpdateVisibility] _npcRenderers array is null. Cannot update visibility.");
        }

        if (interactionCollider != null)
        {
            interactionCollider.enabled = shouldBeVisibleAndInteractable;
        }
        else
        {
            Debug.LogWarning($"[{npcName}-UpdateVisibility] 'interactionCollider' is not assigned. Cannot update interactability.", this);
        }

        if (_npcRenderers != null && _npcRenderers.Length > 0 && _npcRenderers[0] != null)
        {
            if (_npcRenderers[0].enabled != shouldBeVisibleAndInteractable)
            {
                Debug.LogError($"[{npcName}-UpdateVisibility-VERIFY_FAIL_RENDERER] Renderer '{_npcRenderers[0].gameObject.name}' state is {_npcRenderers[0].enabled}, but expected {shouldBeVisibleAndInteractable}!");
            }
        }
        if (interactionCollider != null)
        {
            if (interactionCollider.enabled != shouldBeVisibleAndInteractable)
            {
                Debug.LogError($"[{npcName}-UpdateVisibility-VERIFY_FAIL_COLLIDER] InteractionCollider '{interactionCollider.name}' state is {interactionCollider.enabled}, but expected {shouldBeVisibleAndInteractable}!");
            }
        }
    }

    public void NotifyNpcChangedFloor(FloorVisibilityManager.FloorLevel newNpcFloor)
    {
        if (currentNpcFloorLevel == newNpcFloor)
        {
            Debug.LogWarning($"[{npcName}-NotifyNpcChangedFloor] Called with same floor ({newNpcFloor}). Skipping actual change, but re-evaluating visibility.");
            if (FloorVisibilityManager.Instance != null)
            {
                UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.Instance.CurrentVisibleFloor);
            }
            return;
        }

        //Debug.Log($"[{npcName}-NotifyNpcChangedFloor] NPC changing floor FROM {currentNpcFloorLevel} TO {newNpcFloor}.");
        currentNpcFloorLevel = newNpcFloor;

        if (FloorVisibilityManager.Instance != null)
        {
            FloorVisibilityManager.FloorLevel playerFloor = FloorVisibilityManager.Instance.CurrentVisibleFloor;
            //Debug.Log($"[{npcName}-NotifyNpcChangedFloor] Player is currently on floor {playerFloor}. Updating NPC visibility accordingly.");
            UpdateVisibilityBasedOnPlayerFloor(playerFloor);
        }
        else
        {
            Debug.LogWarning($"[{npcName}-NotifyNpcChangedFloor] FloorVisibilityManager not found when trying to update visibility after NPC floor change. Visibility might be incorrect.");
        }
    }

    // --- NPC-NPC Collision ---
    private void SetupNpcCollisionProperties()
    {
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        else
        {
            Debug.LogWarning($"[{npcName}] NavMeshAgent component is null in SetupNpcCollisionProperties. Cannot set obstacleAvoidanceType.", this);
        }
    }

    private void ApplyNpcNpcCollisionIgnores()
    {
        if (this.interactionCollider == null)
        {
            return;
        }

        foreach (BaseNPC otherNpc in _allActiveNpcs)
        {
            if (otherNpc.interactionCollider != null)
            {
                Physics.IgnoreCollision(this.interactionCollider, otherNpc.interactionCollider, true);
            }
        }
    }

    // =========================================================================
    // 7. Utility & Public Methods
    // =========================================================================

    private void UpdateAnimation()
    {
        if (npcAnimator != null && agent != null && agent.isOnNavMesh && !string.IsNullOrEmpty(walkingParameterName))
        {
            bool isWalking = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            npcAnimator.SetBool(walkingParameterName, isWalking);
        }
    }

    public DialogueData GetDialogueData() => dialogueData;

    public virtual string GetStats()
    {
        return $"Name: {npcName}\nAge: {npcAge}\nGender: {npcGender}\nBlood Type: {npcBloodType}\nZodiac Sign: {npcZodiacSign}\nCock Length: {npcCockLength}\nLikes: {npcLikes}\nDislikes: {npcDislikes}\nCurrent Love: {currentLove}";
    }

    public void NpcDialogueEnded()
    {
        Debug.Log($"[{name}] NpcDialogueEnded called. Current state: {_currentState}");
        if (_currentState == NpcState.InDialogue)
        {
            TransitionToState(NpcState.Idle);
            Debug.Log($"[{name}] NpcDialogueEnded: Transitioned from InDialogue to Idle. New state: {_currentState}");
        }
        else
        {
            Debug.LogWarning($"[{name}] NpcDialogueEnded called, but was not in InDialogue state. Current state: {_currentState}");
        }
    }

    public virtual void PauseAIForCutscene(bool pause)
    {
        if (!pause)
        {
            Debug.Log($"[CutsceneManager->PauseAIForCutscene(false)] {npcName}: _currentState before call: {_currentState}");
        }
        Debug.Log($"[{npcName}-{_currentState}] PauseAIForCutscene({pause}) called.");
        if (pause)
        {
            if (_currentState == NpcState.PlayerApproaching || _currentState == NpcState.InDialogue)
            {
                Debug.LogWarning($"[{npcName}] Was in {_currentState} when PauseAIForCutscene(true) was called. Transitioning to Idle first.");
                ResetInteractionState(); // This calls TransitionToState(NpcState.Idle)
            }
            TransitionToState(NpcState.PausedByCutscene);
        }
        else // Resume from cutscene
        {
            if (_currentState == NpcState.PausedByCutscene)
            {
                NpcState stateToResumeTo = _previousState;
                if (stateToResumeTo == NpcState.PausedByCutscene ||
                    stateToResumeTo == NpcState.None ||
                    stateToResumeTo == NpcState.PlayerApproaching ||
                    stateToResumeTo == NpcState.InDialogue)
                {
                    stateToResumeTo = NpcState.Idle; // Default to Idle
                }
                Debug.Log($"[{npcName}] Resuming from PausedByCutscene. Previous logical state: {_previousState}. Resuming to: {stateToResumeTo}.");
                TransitionToState(stateToResumeTo);
            }
            else
            {
                Debug.LogWarning($"[{npcName}] PauseAIForCutscene(false) called, but NPC was not in PausedByCutscene state. Current: {_currentState}. No transition performed by this call.");
            }
        }
    }

    // =========================================================================
    // 8. Interface & Hover Methods
    // =========================================================================

    public virtual void WhenHovered() { /* Implement visual feedback if needed */ }
    public virtual void HideHover() { /* Implement visual feedback if needed */ }
}