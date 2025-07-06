using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.Rendering.STP;

public class NpcController : MonoBehaviour, IClickable
{
    [Header("NPC Data")]
    [Tooltip("Assign the ScriptableObject that defines this NPC's data and personality.")]
    public NpcConfig npcConfig; // Holds all unique and savable data for this NPC

    public NpcRuntimeData runtimeData { get; private set; }

    // --- Scene/Runtime Fields ---
    private static List<NpcController> _allActiveNpcs = new List<NpcController>();
    public static List<NpcController> AllActiveNpcs => _allActiveNpcs;

    public NavMeshAgent agent;
    public Animator npcAnimator;
    public NavMeshAgent Agent => agent;
    public Animator NpcAnimator => npcAnimator;
    private Coroutine taskExecutionCoroutine;
    private TaskObject currentTask;

    private DialogueData dialogueData;
    private Transform playerTransform;
    private NavMeshAgent playerAgent;
    private Coroutine interactionCoroutine;

    [Header("Floor Management")]
    public FloorVisibilityManager.FloorLevel currentNpcFloorLevel = FloorVisibilityManager.FloorLevel.Lower;
    public Collider interactionCollider;

    private Renderer[] _npcRenderers;

    // --- State Machine ---
    public enum NpcSceneState
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
    [SerializeField] private NpcSceneState _currentState = NpcSceneState.None;
    public NpcSceneState CurrentnpcConfig => _currentState;
    private NpcSceneState _previousState;

    // =========================================================================
    // 1. Unity Lifecycle Methods
    // =========================================================================

    protected virtual void Awake()
    {
        // CRITICAL: Check for config first
        if (npcConfig == null)
        {
            Debug.LogError($"NPC Controller on '{gameObject.name}' is missing its npcConfig config asset! Disabling.", this);
            this.enabled = false;
            return;
        }


        // CRITICAL: Create the unique runtime data instance for this NPC
        runtimeData = WorldDataManager.Instance.GetOrCreateNpcData(npcConfig);


        CachePlayerReferences();
        InitializeNPC();
        LoadDialogueData();

        _npcRenderers = GetComponentsInChildren<Renderer>(true);

        SetupNpcCollisionProperties();
        ApplyNpcNpcCollisionIgnores();
        _allActiveNpcs.Add(this);

        if (interactionCollider == null)
        {
            Debug.LogError($"[{npcConfig.npcName}] NpcController is missing a reference to its 'interactionCollider'. Please assign it in the Inspector.", this);
        }
    }

    private void OnEnable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave += CaptureStateBeforeSave;
        }
    }

    // --- NEW: Unsubscribe when disabled to prevent errors ---
    private void OnDisable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave -= CaptureStateBeforeSave;
        }
    }

    // --- NEW: This method is called by the OnBeforeSave event ---
    private void CaptureStateBeforeSave()
    {
        if (runtimeData != null)
        {
            runtimeData.lastKnownPosition = transform.position;
            Debug.Log($"[NpcController] Captured position for {npcConfig.npcName}: {transform.position}");
        }
    }





    private void Start()
    {
        if (runtimeData.lastKnownPosition != Vector3.zero)
        {
            transform.position = runtimeData.lastKnownPosition;
            Debug.Log($"[NpcController] Applied loaded position to {npcConfig.npcName}: {transform.position}");
        }

        TransitionToState(NpcSceneState.Idle);
    }

    private void Update()
    {
        UpdateStateBehavior();
        UpdateAnimation();
    }

    protected virtual void OnDestroy()
    {
        _allActiveNpcs.Remove(this);
        if (taskExecutionCoroutine != null)
        {
            StopCoroutine(taskExecutionCoroutine);
            taskExecutionCoroutine = null;
        }
        if (interactionCoroutine != null)
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
        }
    }

    private void InitializeNPC()
    {
        agent = GetComponent<NavMeshAgent>();
        npcAnimator = GetComponent<Animator>();
    }

    private void LoadDialogueData()
    {
        if (npcConfig.dialogueFile == null)
        {
            Debug.LogError($"[{gameObject.name}] Dialogue File not assigned in npcConfig!");
            return;
        }
        try
        {
            dialogueData = JsonUtility.FromJson<DialogueData>(npcConfig.dialogueFile.text);
            if (dialogueData == null) throw new System.ArgumentNullException("Parsed dialogue data is null.");
            ValidateDialogueData();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error parsing Dialogue JSON for {npcConfig.dialogueFile.name}: {e.Message}\n{e.StackTrace}");
            dialogueData = null;
        }
    }

    private void ValidateDialogueData()
    {
        if (dialogueData.nodes == null)
        {
            Debug.LogError($"[{gameObject.name}] Invalid dialogue data in {npcConfig.dialogueFile.name}. 'nodes' array is missing or null.");
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
            }
        }
    }

    // =========================================================================
    // 3. State Machine (Core, Enter/Exit, State-specific, Coroutines)
    // =========================================================================

    private void TransitionToState(NpcSceneState newState)
    {
        if (_currentState == newState && newState != NpcSceneState.PlayerApproaching)
        {
            if (newState == NpcSceneState.PlayerApproaching) HandleEnterPlayerApproachingState();
            return;
        }
        _previousState = _currentState;
        OnExitState(_currentState);
        _currentState = newState;
        OnEnterState(_currentState);
    }

    private void OnEnterState(NpcSceneState state)
    {
        switch (state)
        {
            case NpcSceneState.Idle:
                HandleEnterIdleState();
                break;
            case NpcSceneState.MovingToTask:
                HandleEnterMovingToTaskState();
                break;
            case NpcSceneState.PerformingTaskAction:
                HandleEnterPerformingTaskActionState();
                break;
            case NpcSceneState.WaitingAfterTask:
                HandleEnterWaitingAfterTaskState();
                break;
            case NpcSceneState.PlayerApproaching:
                HandleEnterPlayerApproachingState();
                break;
            case NpcSceneState.InDialogue:
                HandleEnterInDialogueState();
                break;
            case NpcSceneState.PausedByCutscene:
                HandleEnterPausedByCutsceneState();
                break;
        }
    }

    private void OnExitState(NpcSceneState state)
    {
        switch (state)
        {
            case NpcSceneState.MovingToTask:
                if (_currentState != NpcSceneState.PerformingTaskAction)
                {
                    if (agent != null && agent.isOnNavMesh) agent.ResetPath();
                }
                break;
            case NpcSceneState.PerformingTaskAction:
                if (currentTask != null && npcAnimator != null && !string.IsNullOrEmpty(currentTask.animationBoolName))
                {
                    npcAnimator.SetBool(currentTask.animationBoolName, false);
                }
                break;
            case NpcSceneState.PlayerApproaching:
                if (interactionCoroutine != null)
                {
                    StopCoroutine(interactionCoroutine);
                    interactionCoroutine = null;
                }
                PointAndClickMovement.Instance?.LockPlayerApproach(this);
                break;
            case NpcSceneState.InDialogue:
                PointAndClickMovement.Instance?.LockPlayerApproach(this);
                break;
            case NpcSceneState.PausedByCutscene:
                if (agent != null && agent.isOnNavMesh && _currentState != NpcSceneState.None)
                {
                    agent.isStopped = false;
                }
                break;
        }
    }

    private void UpdateStateBehavior()
    {
        switch (_currentState)
        {
            case NpcSceneState.MovingToTask:
                if (agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    TransitionToState(NpcSceneState.PerformingTaskAction);
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
            TransitionToState(NpcSceneState.Idle);
            return;
        }
        agent.SetDestination(currentTask.GetTargetPosition());
        agent.isStopped = false;
    }

    private void HandleEnterPerformingTaskActionState()
    {
        if (currentTask == null || agent == null)
        {
            TransitionToState(NpcSceneState.Idle);
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
        StopNpcTaskLoopAndMovement();
        currentTask = null;

        PointAndClickMovement.Instance?.LockPlayerApproach(this);

        if (playerTransform != null && PointAndClickMovement.Instance != null)
        {
            PointAndClickMovement.Instance.SetPlayerDestination(transform.position, true);
        }
        else
        {
            TransitionToState(NpcSceneState.Idle);
            return;
        }

        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(PlayerApproachCoroutine());
    }

    private void HandleEnterInDialogueState()
    {
        DialogueManager.Instance.StartDialogue(this);
    }

    private void HandleEnterPausedByCutsceneState()
    {
        StopNpcTaskLoopAndMovement();
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
            PointAndClickMovement.Instance?.LockPlayerApproach(this);
        }
        if (agent != null && agent.isOnNavMesh)
        {
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
            if (_currentState != NpcSceneState.PerformingTaskAction) yield break;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, turnTimer * npcConfig.rotationSpeed);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        if (_currentState == NpcSceneState.PerformingTaskAction) transform.rotation = targetRotation;

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

        if (_currentState == NpcSceneState.PerformingTaskAction)
        {
            TransitionToState(NpcSceneState.WaitingAfterTask);
        }
    }

    private IEnumerator TaskCompletionDelayCoroutine()
    {
        yield return new WaitForSeconds(npcConfig.taskCompletionDelay);
        if (_currentState == NpcSceneState.WaitingAfterTask)
        {
            TransitionToState(NpcSceneState.Idle);
        }
    }

    private IEnumerator PlayerApproachCoroutine()
    {
        if (playerTransform == null || playerAgent == null)
        {
            if (_currentState == NpcSceneState.PlayerApproaching) TransitionToState(NpcSceneState.Idle);
            yield break;
        }

        yield return new WaitForEndOfFrame();

        bool canInteract = false;
        float stuckTimer = 0f;
        const float maxStuckTime = 3.0f;

        while (_currentState == NpcSceneState.PlayerApproaching && (object)PointAndClickMovement.currentTarget == this)
        {
            float distance = Vector3.Distance(playerTransform.position, transform.position);
            if (distance <= npcConfig.interactRange)
            {
                canInteract = true;
                break;
            }

            if (playerAgent.pathPending || (playerAgent.hasPath && playerAgent.velocity.sqrMagnitude > 0.01f))
            {
                stuckTimer = 0f;
            }
            else if (playerAgent.hasPath && playerAgent.velocity.sqrMagnitude < 0.01f && playerAgent.remainingDistance > agent.stoppingDistance)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > maxStuckTime)
                {
                    canInteract = false;
                    break;
                }
            }
            else if (!playerAgent.hasPath && playerAgent.velocity.sqrMagnitude < 0.01f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > maxStuckTime / 2f)
                {
                    canInteract = false;
                    break;
                }
            }
            else
            {
                stuckTimer = 0f;
            }

            yield return null;
        }

        if (_currentState == NpcSceneState.PlayerApproaching)
        {
            if (canInteract && (object)PointAndClickMovement.currentTarget == this)
            {
                if (playerAgent.isOnNavMesh)
                {
                    playerAgent.ResetPath();
                    playerAgent.velocity = Vector3.zero;
                }
                TransitionToState(NpcSceneState.InDialogue);
            }
            else
            {
                TransitionToState(NpcSceneState.Idle);
            }
        }
        interactionCoroutine = null;
    }

    // =========================================================================
    // 4. Task Management
    // =========================================================================

    private void AssignNextTask()
    {
        if (npcConfig.taskIDPool == null || npcConfig.taskIDPool.Count == 0)
        {
            currentTask = null;
            return;
        }
        currentTask = GetRandomTask();
    }

    private TaskObject GetRandomTask()
    {
        // <<< THIS IS THE CRITICAL CHANGE
        if (npcConfig.taskIDPool == null || npcConfig.taskIDPool.Count == 0) return null;

        // 1. Pick a random task ID from the state data
        string randomTaskID = npcConfig.taskIDPool[Random.Range(0, npcConfig.taskIDPool.Count)];

        // 2. Ask the TaskManager to find the actual TaskObject in the scene with that ID
        if (TaskManager.Instance == null)
        {
            Debug.LogError($"[{npcConfig.npcName}] cannot get a task because TaskManager.Instance is null!");
            return null;
        }

        return TaskManager.Instance.GetTaskByID(randomTaskID);
    }

    private void StartNpcTaskLoopIfNeeded()
    {
        // We now check the taskIDPool
        if (taskExecutionCoroutine == null && npcConfig.taskIDPool != null && npcConfig.taskIDPool.Count > 0)
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
            if (_currentState == NpcSceneState.Idle)
            {
                AssignNextTask();
                if (currentTask != null)
                {
                    TransitionToState(NpcSceneState.MovingToTask);
                }
                else
                {
                    yield return new WaitForSeconds(npcConfig.taskCompletionDelay);
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
        switch (_currentState)
        {
            case NpcSceneState.Idle:
            case NpcSceneState.MovingToTask:
            case NpcSceneState.PerformingTaskAction:
            case NpcSceneState.WaitingAfterTask:
                PointAndClickMovement.currentTarget = this;
                TransitionToState(NpcSceneState.PlayerApproaching);
                break;
            case NpcSceneState.PlayerApproaching:
                HandleEnterPlayerApproachingState();
                break;
            case NpcSceneState.InDialogue:
            case NpcSceneState.PausedByCutscene:
                break;
            default:
                break;
        }
    }

    public virtual void ResetInteractionState()
    {
        switch (_currentState)
        {
            case NpcSceneState.PlayerApproaching:
            case NpcSceneState.InDialogue:
                TransitionToState(NpcSceneState.Idle);
                break;
        }

        if ((object)PointAndClickMovement.currentTarget == this)
        {
            PointAndClickMovement.currentTarget = null;
        }
    }

    public virtual void TriggerFinalCutscene()
    {
        if (npcConfig.finalLoveCutsceneAsset != null)
        {
            CutsceneManager.Instance.StartCutscene(npcConfig.finalLoveCutsceneAsset, this);
        }
    }

    public virtual void PerformCutsceneAction(string methodName)
    {
        // Implement as needed in derived classes
    }

    // =========================================================================
    // 6. Floor/Visibility Management
    // =========================================================================

    public void UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.FloorLevel playerCurrentFloor)
    {
        bool shouldBeVisibleAndInteractable = (this.currentNpcFloorLevel == playerCurrentFloor);

        if (_npcRenderers != null)
        {
            foreach (Renderer rend in _npcRenderers)
            {
                if (rend != null) rend.enabled = shouldBeVisibleAndInteractable;
            }
        }

        if (interactionCollider != null)
        {
            interactionCollider.enabled = shouldBeVisibleAndInteractable;
        }
    }

    public void NotifyNpcChangedFloor(FloorVisibilityManager.FloorLevel newNpcFloor)
    {
        if (currentNpcFloorLevel == newNpcFloor)
        {
            if (FloorVisibilityManager.Instance != null)
            {
                UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.Instance.CurrentVisibleFloor);
            }
            return;
        }
        currentNpcFloorLevel = newNpcFloor;

        if (FloorVisibilityManager.Instance != null)
        {
            FloorVisibilityManager.FloorLevel playerFloor = FloorVisibilityManager.Instance.CurrentVisibleFloor;
            UpdateVisibilityBasedOnPlayerFloor(playerFloor);
        }
    }

    // --- NPC-NPC Collision ---
    private void SetupNpcCollisionProperties()
    {
        if (agent != null)
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
    }

    private void ApplyNpcNpcCollisionIgnores()
    {
        if (this.interactionCollider == null)
        {
            return;
        }

        foreach (NpcController otherNpc in _allActiveNpcs)
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
        if (npcAnimator != null && agent != null && agent.isOnNavMesh && !string.IsNullOrEmpty(npcConfig.walkingParameterName))
        {
            bool isWalking = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            npcAnimator.SetBool(npcConfig.walkingParameterName, isWalking);
        }
    }

    public DialogueData GetDialogueData() => dialogueData;

    public string GetCurrentStats()
    {
        // It pulls static data from the config...
        string staticInfo = $"Name: {npcConfig.npcName}\nAge: {npcConfig.npcAge}\nGender: {npcConfig.npcGender}\nBlood Type: {npcConfig.npcBloodType}\nZodiac Sign: {npcConfig.npcZodiacSign}\nCock Length: {npcConfig.npcCockLength}\nLikes: {npcConfig.npcLikes}\nDislikes: {npcConfig.npcDislikes}";

        // ...and it pulls DYNAMIC data from the runtimeData object!
        string dynamicInfo = $"Current Love: {runtimeData.currentLove}";

        return $"{staticInfo}\n{dynamicInfo}";
    }

    public void NpcDialogueEnded()
    {
        if (_currentState == NpcSceneState.InDialogue)
        {
            TransitionToState(NpcSceneState.Idle);
        }
    }

    public virtual void PauseAIForCutscene(bool pause)
    {
        if (pause)
        {
            if (_currentState == NpcSceneState.PlayerApproaching || _currentState == NpcSceneState.InDialogue)
            {
                ResetInteractionState();
            }
            TransitionToState(NpcSceneState.PausedByCutscene);
        }
        else
        {
            if (_currentState == NpcSceneState.PausedByCutscene)
            {
                NpcSceneState stateToResumeTo = _previousState;
                if (stateToResumeTo == NpcSceneState.PausedByCutscene ||
                    stateToResumeTo == NpcSceneState.None ||
                    stateToResumeTo == NpcSceneState.PlayerApproaching ||
                    stateToResumeTo == NpcSceneState.InDialogue)
                {
                    stateToResumeTo = NpcSceneState.Idle;
                }
                TransitionToState(stateToResumeTo);
            }
        }
    }

}