using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI; // Keep if you still use UI elements linked here

public class BaseNPC : MonoBehaviour, IClickable
{
    [Header("Interaction Settings")]
    public float interactRange = 2.0f;
    private bool isInteracting = false;

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

    // Cached References
    private DialogueData dialogueData;
    private Transform playerTransform;
    private NavMeshAgent playerAgent;
    private Coroutine interactionCoroutine;

    [Header("Floor Management")]
    [Tooltip("The current floor this NPC is on. Needs to be updated by game logic when NPC changes floors (e.g., via tasks).")]
    public FloorVisibilityManager.FloorLevel currentNpcFloorLevel = FloorVisibilityManager.FloorLevel.Lower; // Default or set in Inspector

    private Renderer[] _npcRenderers;
    private Collider[] _npcColliders;

    protected virtual void Awake()
    {
        CachePlayerReferences();
        InitializeNPC();
        LoadDialogueData();
        InitializeLove(); // Needs dialogueData loaded first
        // Cache NPC's own renderers and colliders
        _npcRenderers = GetComponentsInChildren<Renderer>(true);
        _npcColliders = GetComponentsInChildren<Collider>(true); // Get all colliders, including the main one

       // Initial visibility check if FloorVisibilityManager is ready
        if (FloorVisibilityManager.Instance != null)
        {
            UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.Instance.CurrentVisibleFloor);
        }
        else
        {
            // Fallback: Make NPC visible by default if manager isn't ready yet (it will be corrected in FVM.Start)
            // Or, you could subscribe to an event from FVM once it's initialized.
            // For simplicity, FVM.Start() will call NotifyAllNpcsOfFloorChange, which will set it correctly.
            // So, no explicit action needed here for that case.
        }
    }

    private void Start()
    {
        StartTaskLoop();
    }

    private void Update()
    {
        UpdateAnimation(); // Update walking animation based on agent velocity
    }

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

    private void StartTaskLoop()
    {
        // Start the main loop ONLY if not already running
        if (agent != null && taskPool != null && taskPool.Count > 0 && !isInteracting && taskExecutionCoroutine == null)
        {
            // Debug.Log($"[{name}] Starting Task Loop.");
            taskExecutionCoroutine = StartCoroutine(TaskLoop());
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
            dialogueData = null; // Ensure it's null on error
        }
    }

    private void ValidateDialogueData()
    {
        // Basic validation already done implicitly by checking dialogueData != null after parse
        if (dialogueData.nodes == null)
        {
            Debug.LogError($"[{gameObject.name}] Invalid dialogue data in {dialogueFile.name}. 'nodes' array is missing or null.");
            dialogueData = null; // Invalidate data
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
    public void OnClick()
    {
        // Ignore click if already interacting with this NPC
        if (isInteracting)
        {
            // Debug.Log($"[{name}] Already interacting, ignoring click.");
            return;
        }

        // If player was interacting with someone else, reset that target
        if (PointAndClickMovement.currentTarget != null && PointAndClickMovement.currentTarget != this)
        {
            PointAndClickMovement.currentTarget.ResetInteractionState();
        }

        // Set this NPC as the new target
        PointAndClickMovement.currentTarget = this;
        InitiateInteraction();
    }

    public virtual void ResetInteractionState()
    {
        isInteracting = false;

        // Stop player movement towards this NPC if it was happening
        if (interactionCoroutine != null)
        {
            StopCoroutine(interactionCoroutine);
            interactionCoroutine = null;
        }

        // If dialogue was active with this NPC, end it
        if (DialogueManager.Instance != null && DialogueManager.Instance.GetCurrentNPC() == this)
        {
            DialogueManager.Instance.EndDialogue(); // Dialogue Manager should call ResumeNPC
        }
        else
        {
            // If dialogue wasn't active (e.g., player clicked away before reaching NPC), just resume NPC
            ResumeNPC();
        }
    }

    public virtual void WhenHovered() { /* Implement visual feedback if needed */ }
    public virtual void HideHover() { /* Implement visual feedback if needed */ }

    private void InitiateInteraction()
    {
        if (playerTransform == null || playerAgent == null)
        {
            Debug.LogError($"[{name}] Player/Agent not found. Cannot initiate interaction.");
            ResetInteractionState(); // Ensure clean state
            return;
        }

        // Debug.Log($"[{name}] Initiating interaction.");
        isInteracting = true;
        StopNPCForInteraction(); // Stop tasks and movement

        // Start moving the player towards the NPC
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(MovePlayerAndStartDialogue());
    }

    private IEnumerator MovePlayerAndStartDialogue()
    {
        // While this NPC is the target and player is too far away
        while (PointAndClickMovement.currentTarget == this && playerAgent != null &&
               Vector3.Distance(playerTransform.position, transform.position) > interactRange)
        {
            // Continuously set destination if path isn't being calculated
            if (!playerAgent.pathPending)
            {
                playerAgent.SetDestination(transform.position);
            }
            yield return null; // Wait for the next frame
        }

        // Check if we are still the target and close enough when the loop ends
        if (playerAgent != null && PointAndClickMovement.currentTarget == this &&
            Vector3.Distance(playerTransform.position, transform.position) <= interactRange)
        {
            // Debug.Log($"[{name}] Player arrived. Starting dialogue.");
            playerAgent.ResetPath(); // Stop the player agent
            playerAgent.velocity = Vector3.zero; // Ensure player stops moving immediately
            StartDialogueInternal();
        }
        else
        {
            // Player clicked away or something went wrong
             // Debug.Log($"[{name}] Interaction cancelled before dialogue started (Player moved away or target changed).");
            ResetInteractionState(); // This will call ResumeNPC
        }

        interactionCoroutine = null; // Mark coroutine as finished
    }

    private void StopNPCForInteraction()
    {
        // Debug.Log($"[{name}] Stopping NPC for interaction.");
        // Stop any ongoing task execution (movement, waiting, performing action)
        if (taskExecutionCoroutine != null)
        {
            StopCoroutine(taskExecutionCoroutine);
            taskExecutionCoroutine = null;
        }

        // Stop the NavMeshAgent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath(); // Clear any existing path
            agent.velocity = Vector3.zero; // Stop immediately
        }

        // Ensure walking animation stops
        if (npcAnimator != null && !string.IsNullOrEmpty(walkingParameterName))
        {
            npcAnimator.SetBool(walkingParameterName, false);
        }

        currentTask = null; // No longer pursuing the current task
    }

    private void StartDialogueInternal()
    {
        if (DialogueManager.Instance != null && dialogueData != null && dialogueData.nodeDictionary != null)
        {
            DialogueManager.Instance.StartDialogue(this);
            
        }
        else
        {
             Debug.LogError($"[{name}] Cannot start dialogue. DialogueManager or DialogueData is invalid.");
            // If dialogue fails to start, we should resume the NPC's routine
            ResumeNPC();
        }
    }


    // Called by DialogueManager when dialogue ends OR if interaction is cancelled prematurely
    public void ResumeNPC()
    {
        // Only resume if we were previously interacting
        if (isInteracting)
        {
           // Debug.Log($"[{name}] Resuming NPC routine.");
           isInteracting = false; // Ensure flag is reset
        }

        // Allow agent to move again if it exists and is on the mesh
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        // Restart the task loop IF it's not already running
        StartTaskLoop();
    }


    private IEnumerator TaskLoop()   /// Main loop deciding when to find and start the next task.
    {
        // Debug.Log($"[{name}] Task Loop Started.");
        while (true)
        {
            // If interacting, pause the loop until interaction is finished
            if (isInteracting)
            {
                // Debug.Log($"[{name}] Task Loop paused due to interaction.");
                yield return new WaitUntil(() => !isInteracting);
                // Debug.Log($"[{name}] Task Loop resumed after interaction.");
            }

            // --- Find and Execute Next Task ---
            if (currentTask == null && !isInteracting) // Ensure we don't have a task and aren't interacting
            {
                AssignNextTask(); // Find a task and set currentTask

                if (currentTask != null)
                {
                    // Start the combined Move + Perform coroutine
                    // Debug.Log($"[{name}] Starting execution for task: {currentTask.taskName}");
                    yield return StartCoroutine(MoveAndPerformTask(currentTask)); // Wait for this task to fully complete
                    // Debug.Log($"[{name}] Finished execution for task: {currentTask.taskName}");
                    currentTask = null; // Mark task as done *after* MoveAndPerformTask finishes

                    // Wait for the specified delay before picking the next task
                    if (!isInteracting) // Check again in case interaction started during task execution/delay
                    {
                       // Debug.Log($"[{name}] Waiting for task delay: {taskCompletionDelay} seconds.");
                       yield return new WaitForSeconds(taskCompletionDelay);
                    }
                }
                else
                {
                    // No tasks available or failed to assign, wait a bit before trying again
                    // Debug.Log($"[{name}] No task assigned. Waiting before retry.");
                    yield return new WaitForSeconds(taskCompletionDelay); // Use same delay? Or a different "idle" delay?
                }
            }
            else
            {
                 // If already have a task or are interacting, just yield to wait for next frame
                yield return null;
            }
        }
        // Note: The 'taskExecutionCoroutine = null' assignment is handled when the coroutine naturally ends or is stopped.
    }


    private void AssignNextTask()
    {
        if (isInteracting || taskPool == null || taskPool.Count == 0)
        {
            currentTask = null; // Ensure no task is assigned
            return;
        }
        currentTask = GetRandomTask();
        // Debug.Log($"[{name}] Assigned Task: {(currentTask != null ? currentTask.taskName : "None")}");
    }

    private TaskObject GetRandomTask()
    {
        if (taskPool == null || taskPool.Count == 0) return null;
        // Add logic here if you want tasks weighted or prevent repeating the same task immediately
        return taskPool[Random.Range(0, taskPool.Count)];
    }

    private IEnumerator MoveAndPerformTask(TaskObject task)     /// Coroutine to handle moving to the task location and then executing the task action.

    {
        if (task == null || agent == null || !agent.isOnNavMesh || isInteracting)
        {
            // Debug.LogWarning($"[{name}] Cannot move to task {task?.taskName}. Invalid state or task.");
            yield break; // Exit if conditions aren't met
        }

        Vector3 destination = task.GetTargetPosition();
        agent.SetDestination(destination);
        agent.isStopped = false; // Make sure agent is moving

        // Debug.Log($"[{name}] Moving to task: {task.taskName} at {destination}");

        // --- Wait for Arrival ---
        while (!isInteracting && agent.pathPending)
        {
            // Wait while the path is being calculated
            yield return null;
        }

        while (!isInteracting && agent.remainingDistance > agent.stoppingDistance)
        {
            // Wait while the agent is still moving towards the destination
             // UpdateAnimation(); // Ensure animation is updated while moving - moved to Update()
            yield return null;
        }

        // --- Arrived or Interrupted ---
        if (isInteracting)
        {
            // Debug.Log($"[{name}] Interaction started while moving to task {task.taskName}. Aborting task execution.");
            // StopNPCForInteraction should have already handled stopping the agent
            yield break; // Exit the coroutine
        }

        // --- Execute Task Action ---
        if (agent.remainingDistance <= agent.stoppingDistance) // Double check we actually arrived
        {
            // Debug.Log($"[{name}] Arrived at task: {task.taskName}. Performing action.");
            agent.isStopped = true; // Stop movement
            agent.velocity = Vector3.zero; // Explicitly zero out velocity

            // --- Rotate towards task orientation ---
            Quaternion targetRotation = task.GetTargetRotation();
            // If TaskObject doesn't specify rotation, maybe look forward along arrival path? Or default rotation?
            // For now, using the TaskObject's rotation or the specific target point's rotation.
            if (task.specificTargetPoint != null)
            {
                targetRotation = task.specificTargetPoint.rotation;
            }
            else // Fallback: Use TaskObject's root rotation
            {
                 targetRotation = task.transform.rotation;
            }
            // Optional Smooth Rotation:
            float startTime = Time.time;
            Quaternion startRotation = transform.rotation;
            while(Time.time < startTime + 1.0f && Quaternion.Angle(transform.rotation, targetRotation) > 1.0f) // Rotate for max 1 sec or until close
            {
                 if(isInteracting) yield break; // Allow interruption during rotation
                 transform.rotation = Quaternion.Lerp(startRotation, targetRotation, (Time.time - startTime) * rotationSpeed);
                 yield return null;
            }
             // Ensure final rotation if Lerp didn't quite reach it or if rotation is fast
             if(!isInteracting) transform.rotation = targetRotation;


             // --- Perform the Timed Action ---
             yield return StartCoroutine(ExecuteTaskAction(task));
        }
        else
        {
             // Debug.LogWarning($"[{name}] Did not reach destination for task {task.taskName}. Remaining distance: {agent.remainingDistance}");
        }

        // --- Task Action Finished (or was skipped) ---
        // Ensure agent is allowed to move again if not interacting
        if (!isInteracting && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

private IEnumerator ExecuteTaskAction(TaskObject task)     /// Coroutine that plays the task animation and waits for its duration.
{
    if (isInteracting) yield break;

    bool animationSet = false; // Flag to track if we set the bool
    if (npcAnimator != null && !string.IsNullOrEmpty(task.animationBoolName)) // Use bool name
    {
        // Debug.Log($"[{name}] Setting animation bool '{task.animationBoolName}' to true for task '{task.taskName}'. Duration: {task.duration}s");
        npcAnimator.SetBool(task.animationBoolName, true); // Set bool to TRUE
        animationSet = true;
    }
    else
    {
       // Debug.Log($"[{name}] Performing task '{task.taskName}' (no animation). Duration: {task.duration}s");
    }

    // Wait for the task duration
    float timer = 0f;
    while(timer < task.duration)
    {
        if(isInteracting)
        {
            // Debug.Log($"[{name}] Interaction started during task action '{task.taskName}'. Aborting wait.");
            yield break; // Exit if interaction starts
        }
        timer += Time.deltaTime;
        yield return null;
    }

   // Debug.Log($"[{name}] Finished task action: {task.taskName}");

    // --- IMPORTANT: Set the boolean back to false ---
    if (animationSet) // Only reset if we actually set it
    {
         // Debug.Log($"[{name}] Setting animation bool '{task.animationBoolName}' back to false.");
         npcAnimator.SetBool(task.animationBoolName, false); // Set bool to FALSE
    }
}
    private void UpdateAnimation()
    {
        if (npcAnimator != null && agent != null && agent.isOnNavMesh && !string.IsNullOrEmpty(walkingParameterName))
        {
            // Use agent's velocity magnitude to determine if walking
            // Use a small threshold to avoid flickering when stopping/starting
            bool isWalking = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            npcAnimator.SetBool(walkingParameterName, isWalking);
        }
    }
    public void UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.FloorLevel playerCurrentFloor)
    {
        bool shouldBeVisible = (this.currentNpcFloorLevel == playerCurrentFloor);

        // Debug.Log($"[{name}] On floor {currentNpcFloorLevel}. Player on {playerCurrentFloor}. Should be visible: {shouldBeVisible}");

        if (_npcRenderers != null)
        {
            foreach (Renderer rend in _npcRenderers)
            {
                rend.enabled = shouldBeVisible;
            }
        }

        if (_npcColliders != null)
        {
            foreach (Collider col in _npcColliders)
            {
                // Be careful if some colliders should always be active for physics reasons
                // other than clicking (e.g. a trigger for NPC internal logic).
                // For typical character controllers and click detection, this is usually fine.
                col.enabled = shouldBeVisible;
            }
        }
    }

    /// <summary>
    /// Call this method when game logic moves this NPC to a different floor.
    /// For example, after a "Use Stairs" task is completed.
    /// </summary>
    /// <param name="newFloor">The new floor the NPC is now on.</param>
    public void NotifyNpcChangedFloor(FloorVisibilityManager.FloorLevel newNpcFloor)
    {
        if (currentNpcFloorLevel == newNpcFloor) return; // No change

        Debug.Log($"[{name}] changed floor from {currentNpcFloorLevel} to {newNpcFloor}.");
        currentNpcFloorLevel = newNpcFloor;

        // Update visibility immediately based on the player's current floor
        if (FloorVisibilityManager.Instance != null)
        {
            UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.Instance.CurrentVisibleFloor);
        }
        else
        {
            Debug.LogWarning($"[{name}] FloorVisibilityManager not found when trying to update visibility after NPC floor change.");
        }
    }


    // --- Utility ---
    public DialogueData GetDialogueData() => dialogueData;
    public virtual string GetStats()
    {
        return $"Name: {npcName}\nAge: {npcAge}\nGender: {npcGender}\nBlood Type: {npcBloodType}\nZodiac Sign: {npcZodiacSign}\nCock Length: {npcCockLength}\nLikes: {npcLikes}\nDislikes: {npcDislikes}\nCurrent Love: {currentLove}";
    }
}