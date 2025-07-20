// NpcBrain.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// This class is the "brain" for an NPC's AUTONOMOUS behavior.
/// It is responsible for making decisions based on needs and goals,
/// and executing the tasks to fulfill them. It operates as a state machine
/// that is only active when the NPC is in its "Autonomous" super-state.
/// </summary>
public class NpcBrain : MonoBehaviour
{
    // --- COMPONENT REFERENCES (Set by NpcController) ---
    private NpcController _controller;
    private NpcConfig _config;
    private NpcRuntimeData _runtimeData;
    private NavMeshAgent _agent;
    private Animator _animator;
    private NpcPerception _perception;

    // --- AUTONOMOUS STATE MACHINE ---
    // This enum has been moved from NpcController and renamed to be more specific.
    // It now ONLY defines states relevant to autonomous task execution.
    public enum AutonomousState
    {
        None,
        Idle,
        MovingToTask,
        PerformingTaskAction,
        WaitingAfterTask,
        SearchingForActivity,
        InDialogue, // Brain is paused for dialogue
        PausedBySystem, // Brain is paused for cutscenes, etc.
        WatchingPlayerAction // A reactive, temporary state
    }
    [Header("Brain State (Read-Only)")]
    [SerializeField] private AutonomousState _currentState = AutonomousState.None;
    public AutonomousState CurrentState => _currentState;
    private AutonomousState _previousState;


    // --- AI BEHAVIOR & DECISION MAKING ---
    [Header("AI Tuning")]
    [SerializeField] private float desperationThreshold = 50f;
    [SerializeField] private float searchEagerness = 1.2f;
    [SerializeField] private float searchFailureCooldown = 30f;
    [SerializeField] private LayerMask wallLayerMask;

    [SerializeField] private float searchMeanderWidth = 5f;
    [SerializeField] private float searchMeanderDistance = 8f;

    // --- INTERNAL STATE ---
    private Coroutine _taskExecutionCoroutine;
    private ActivityObject _currentActivity;
    private string _currentSearchNeed;
    private SearchableZone _currentSearchZone;
    private Transform _watchedTransform;

    private Dictionary<string, float> _recentlyFailedSearches = new Dictionary<string, float>();
    private bool _isFindingNewPoint = false;
    private float _timeSpentSearchingInCurrentZone;
    private const float MaxSearchTimePerZone = 15f;
    private Vector3 _currentSearchDestination;

    /// <summary>
    /// This is the new entry point. NpcController calls this to give the Brain
    /// all the references it needs to function. This is a clean alternative to
    /// manually assigning a dozen fields in the Inspector.
    /// </summary>
    public void Initialize(NpcController controller)
    {
        _controller = controller;
        _config = controller.npcConfig;
        _runtimeData = controller.runtimeData;
        _agent = controller.Agent;
        _animator = controller.NpcAnimator;
        _perception = controller.Perception;

        // Make sure we have the required data
        if (_config == null || _runtimeData == null || _agent == null || _animator == null || _perception == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcBrain initialization failed. One or more critical components are missing. Disabling brain.", this);
            this.enabled = false;
        }
        if (_runtimeData.needs == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcBrain initialization failed. The provided NpcRuntimeData has a null 'needs' collection. The brain cannot function without it. Please ensure it is initialized when the data is created. Disabling brain.", this);
            this.enabled = false;
            return; // Stop further initializatio
        }
    }

    /// <summary>
    /// Called by NpcController to "turn on" the brain's autonomous thinking process.
    /// </summary>
    public void ActivateBrain()
    {
        if (!this.enabled) return;
        Debug.Log($"[{_config.npcName}] Brain activated. Transitioning to Idle state.");
        TransitionToState(AutonomousState.Idle);
    }

    /// <summary>
    /// Called by NpcController to "turn off" the brain's autonomous thinking process.
    /// This is crucial for switching to Hangout mode or being controlled by a cutscene.
    /// </summary>
    public void DeactivateBrain()
    {
        Debug.Log($"[{_config.npcName}] Brain deactivated.");
        StopNpcTaskLoopAndMovement();
        TransitionToState(AutonomousState.None);
    }


    #region Public Methods to Influence Brain State
    // These methods are how the NpcController tells the brain about external events.

    public void OnDialogueStart()
    {
        // When dialogue starts, the brain should pause its current thinking and enter a "paused" state.
        TransitionToState(AutonomousState.InDialogue);
    }

    public void OnDialogueEnd()
    {
        // When dialogue ends, the brain should resume its normal life.
        if (_currentState == AutonomousState.InDialogue)
        {
            TransitionToState(AutonomousState.Idle);
        }
    }

    public void OnPauseForSystem(bool isPaused)
    {
        if (isPaused)
        {
            TransitionToState(AutonomousState.PausedBySystem);
        }
        else
        {
            // Resume to the previous state before being paused, or Idle as a safe default.
            if (_currentState == AutonomousState.PausedBySystem)
            {
                AutonomousState stateToResume = _previousState != AutonomousState.PausedBySystem ? _previousState : AutonomousState.Idle;
                TransitionToState(stateToResume);
            }
        }
    }

    /// <summary>
    /// Called by NpcPerception when a player-dragged object is seen.
    /// </summary>
    public void NoticeDraggedObject(Draggable draggedObject)
    {
        if (_currentState == AutonomousState.InDialogue || _currentState == AutonomousState.PausedBySystem || _currentState == AutonomousState.WatchingPlayerAction)
        {
            return;
        }

        Debug.Log($"[{_config.npcName}] saw the player dragging '{draggedObject.name}' and is now distracted.");
        _watchedTransform = draggedObject.transform;
        TransitionToState(AutonomousState.WatchingPlayerAction);
    }

    /// <summary>
    /// The core learning method. Called by NpcPerception when a new activity is seen.
    /// </summary>
    public void LearnAboutActivity(ActivityObject activity)
    {
        // (Code moved directly from NpcController)
        if (_runtimeData.rememberedActivityLocations.ContainsKey(activity.activityID))
        {
            _runtimeData.rememberedActivityLocations[activity.activityID] = activity.GetTargetPosition();
        }
        else
        {
            _runtimeData.rememberedActivityLocations.Add(activity.activityID, activity.GetTargetPosition());
        }

        SearchableZone zone = GetZoneForPosition(activity.transform.position);
        if (zone != null)
        {
            if (!_runtimeData.learnedZoneContents.ContainsKey(zone.zoneName))
            {
                _runtimeData.learnedZoneContents.Add(zone.zoneName, new HashSet<string>());
            }
            foreach (var effect in activity.needEffects)
            {
                if (effect.effectValue < 0)
                    _runtimeData.learnedZoneContents[zone.zoneName].Add(effect.needName);
            }
            foreach (var tag in activity.activityTags)
            {
                _runtimeData.learnedZoneContents[zone.zoneName].Add(tag);
            }
        }
    }

    private bool DoesActivityMatchGoals(ActivityObject activity)
    {
        if (activity.activityTags == null || activity.activityTags.Count == 0 || _config.personalityGoals == null) return false;

        foreach (var goal in _config.personalityGoals)
        {
            foreach (var goalTag in goal.associatedTags)
            {
                if (activity.activityTags.Contains(goalTag))
                {
                    return true; // Found a match!
                }
            }
        }
        return false; // No matches found.
    }

    private string GetPrimaryReasonForActivity(ActivityObject activity)
    {
        string reason = "Personal Goal"; // Default reason if no need is met
        float highestNeedVal = -1f;
        foreach (var effect in activity.needEffects)
        {
            if (_runtimeData.needs.ContainsKey(effect.needName))
            {
                if (_runtimeData.needs[effect.needName].currentValue > highestNeedVal)
                {
                    highestNeedVal = _runtimeData.needs[effect.needName].currentValue;
                    reason = effect.needName;
                }
            }
        }
        return reason;
    }

    #endregion

    // =========================================================================
    // THE REST OF THIS SCRIPT IS LOGIC MOVED DIRECTLY FROM NpcController.cs
    // The only change is that "NpcSceneState" has been replaced with "AutonomousState"
    // and references to components are now the private fields (_agent, _animator, etc.)
    // =========================================================================

    #region State Machine (Core, Enter/Exit, State-specific, Coroutines)

    private void TransitionToState(AutonomousState newState)
    {
        if (_currentState == newState) return;

        _previousState = _currentState;
        OnExitState(_currentState);
        _currentState = newState;
        OnEnterState(_currentState);
    }

    private void OnEnterState(AutonomousState state)
    {
        switch (state)
        {
            case AutonomousState.Idle:
                HandleEnterIdleState();
                break;
            case AutonomousState.MovingToTask:
                HandleEnterMovingToTaskState();
                break;
            case AutonomousState.PerformingTaskAction:
                HandleEnterPerformingTaskActionState();
                break;
            case AutonomousState.WaitingAfterTask:
                HandleEnterWaitingAfterTaskState();
                break;
            case AutonomousState.SearchingForActivity:
                HandleEnterSearchingForActivityState();
                break;
            case AutonomousState.WatchingPlayerAction:
                StopNpcTaskLoopAndMovement(); // We get distracted
                break;
            case AutonomousState.InDialogue:
                StopNpcTaskLoopAndMovement(); // Pause for dialogue
                if (_controller.PlayerTransform != null) // Turn to face player
                {
                    Vector3 directionToPlayer = (_controller.PlayerTransform.position - transform.position).normalized;
                    directionToPlayer.y = 0;
                    transform.rotation = Quaternion.LookRotation(directionToPlayer);
                }
                break;
            case AutonomousState.PausedBySystem:
                StopNpcTaskLoopAndMovement();
                if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
                break;
        }
    }

    private void Update()
    {
        // Needs still decay globally regardless of brain state
        foreach (var need in _runtimeData.needs.Values)
        {
            need.UpdateNeed(Time.deltaTime);
        }

        // The state-specific Update logic ONLY runs if the brain is active.
        if (_currentState == AutonomousState.None) return;

        UpdateStateBehavior();
    }


    private void UpdateStateBehavior()
    {
        switch (_currentState)
        {
            case AutonomousState.MovingToTask:
                if (_agent != null && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    TransitionToState(AutonomousState.PerformingTaskAction);
                }
                break;

            case AutonomousState.WatchingPlayerAction:
                Draggable watchedItem = _watchedTransform?.GetComponent<Draggable>();
                if (_watchedTransform != null && _perception.CanSeeTarget(_watchedTransform) && watchedItem != null && watchedItem == Draggable.CurrentlyDraggedItem)
                {
                    Vector3 direction = (_watchedTransform.position - transform.position).normalized;
                    direction.y = 0;
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _config.rotationSpeed);
                }
                else
                {
                    TransitionToState(AutonomousState.Idle);
                }
                break;
            case AutonomousState.SearchingForActivity:
                UpdateSearchingBehavior();
                break;
        }
    }

    private void OnExitState(AutonomousState state)
    {
        switch (state)
        {
            case AutonomousState.MovingToTask:
                if (_currentState != AutonomousState.PerformingTaskAction && _agent != null && _agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                }
                break;
            case AutonomousState.PerformingTaskAction:
                if (_currentActivity != null && _animator != null && !string.IsNullOrEmpty(_currentActivity.animationBoolName))
                {
                    _animator.SetBool(_currentActivity.animationBoolName, false);
                }
                break;
            case AutonomousState.PausedBySystem:
                if (_agent != null && _agent.isOnNavMesh && _currentState != AutonomousState.None)
                {
                    _agent.isStopped = false;
                }
                break;
            case AutonomousState.WatchingPlayerAction:
                _watchedTransform = null;
                break;
        }
    }

    // --- State-specific Enter Logic ---
    private void HandleEnterIdleState()
    {
        _currentActivity = null;
        if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
        StartNpcTaskLoopIfNeeded();
    }

    private void HandleEnterMovingToTaskState()
    {
        if (_currentActivity == null || _agent == null || !_agent.isOnNavMesh)
        {
            TransitionToState(AutonomousState.Idle);
            return;
        }

        if (!_runtimeData.rememberedActivityLocations.ContainsKey(_currentActivity.activityID))
        {
            Debug.LogError($"[AI] Decided to perform '{_currentActivity.activityID}' but has no memory of its location! This shouldn't happen. Going idle.");
            TransitionToState(AutonomousState.Idle);
            return;
        }

        StartCoroutine(ExecuteMoveToTaskCoroutine());
    }

    private void HandleEnterPerformingTaskActionState()
    {
        if (_currentActivity == null || _agent == null)
        {
            TransitionToState(AutonomousState.Idle);
            return;
        }
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }
        StartCoroutine(ExecuteTaskActionCoroutine(_currentActivity));
    }

    private void HandleEnterSearchingForActivityState()
    {
        _timeSpentSearchingInCurrentZone = 0f;
        _isFindingNewPoint = false;
        Debug.Log($"<color=orange>Starting search in '{_currentSearchZone.zoneName}' for '{_currentSearchNeed}'. Will search for a max of {MaxSearchTimePerZone}s.</color>");
        MoveToRandomPointInZone();
    }

    private void HandleEnterWaitingAfterTaskState()
    {
        _currentActivity = null;
        StartCoroutine(TaskCompletionDelayCoroutine());
    }

    private IEnumerator ExecuteMoveToTaskCoroutine()
    {
        // 1. Get the remembered location from memory ONCE. This is a static point in space.
        if (_currentActivity == null || !_runtimeData.rememberedActivityLocations.ContainsKey(_currentActivity.activityID))
        {
            Debug.LogError($"[AI] Started moving to '{_currentActivity?.activityID}' but have no memory of it. Aborting.");
            TransitionToState(AutonomousState.Idle);
            yield break;
        }
        Vector3 rememberedPosition = _runtimeData.rememberedActivityLocations[_currentActivity.activityID];

        // 2. Travel to that static point.
        _agent.SetDestination(rememberedPosition);
        _agent.isStopped = false;

        // Wait until the NPC arrives at the destination.
        while (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance)
        {
            if (_currentState != AutonomousState.MovingToTask)
            {
                // If the state was changed externally (e.g., by OnActivityObjectMoved), abort.
                yield break;
            }
            yield return null;
        }

        // 3. We have arrived. Now, VERIFY the object is actually here.
        float verificationRadius = 3f; // A forgiving radius around the destination.
        Collider[] nearbyObjects = Physics.OverlapSphere(_agent.destination, verificationRadius);
        bool activityFound = false;
        foreach (var col in nearbyObjects)
        {
            ActivityObject foundActivity = col.GetComponentInParent<ActivityObject>();
            if (foundActivity != null && foundActivity == _currentActivity)
            {
                activityFound = true;
                break;
            }
        }

        if (activityFound)
        {
            // SUCCESS: The object was where we thought it would be.
            //Debug.Log($"[AI] Successfully arrived at '{_currentActivity.activityID}' and confirmed its location.");
            TransitionToState(AutonomousState.PerformingTaskAction);
        }
        else
        {
            // FAILURE: The object is gone. This is where the memory is cleared.
            Debug.LogWarning($"[AI] Arrived at last known location of '{_currentActivity.activityID}', but it's not here! My memory was wrong.");
            if (_runtimeData.rememberedActivityLocations.ContainsKey(_currentActivity.activityID))
            {
                _runtimeData.rememberedActivityLocations.Remove(_currentActivity.activityID);
            }
            _currentActivity = null;
            TransitionToState(AutonomousState.Idle); // Go idle to force a new decision.
        }
    }

    private IEnumerator ExecuteTaskActionCoroutine(ActivityObject activity)
    {
        if (activity == null) yield break;

        // --- THIS IS THE FIX ---
        // 1. Get the potentially tilted rotation from the activity as before.
        Quaternion initialTargetRotation = activity.GetTargetRotation();

        // 2. Create a direction vector from that rotation.
        Vector3 directionToLook = initialTargetRotation * Vector3.forward;

        // 3. CRITICAL: Flatten the direction vector to the horizontal plane.
        // This ensures the NPC only considers the direction and stays upright.
        directionToLook.y = 0;

        // 4. Create the final, safe rotation for the NPC to use.
        Quaternion safeTargetRotation;

        // Only create a new rotation if the direction is valid (i.e., not pointing straight up or down).
        if (directionToLook.sqrMagnitude > 0.001f)
        {
            safeTargetRotation = Quaternion.LookRotation(directionToLook.normalized);
        }
        else
        {
            // Fallback: If the activity's target points straight up or down, the NPC will not rotate.
            // This prevents LookRotation from throwing an error and keeps the NPC from doing something strange.
            safeTargetRotation = transform.rotation;
        }

        float turnTimer = 0f;
        Quaternion startRotation = transform.rotation;
        while (turnTimer < 1f && Quaternion.Angle(transform.rotation, safeTargetRotation) > 1f)
        {
            if (_currentState != AutonomousState.PerformingTaskAction) yield break;
            transform.rotation = Quaternion.Slerp(startRotation, safeTargetRotation, turnTimer * _config.rotationSpeed);
            turnTimer += Time.deltaTime;
            yield return null;
        }
        if (_currentState == AutonomousState.PerformingTaskAction) transform.rotation = safeTargetRotation;

        bool animationSet = false;
        if (_animator != null && !string.IsNullOrEmpty(activity.animationBoolName))
        {
            _animator.SetBool(activity.animationBoolName, true);
            animationSet = true;
        }

        yield return new WaitForSeconds(activity.duration);

        if (animationSet && _animator != null)
        {
            _animator.SetBool(activity.animationBoolName, false);
        }

        Debug.Log($"{_config.npcName} finished '{activity.activityID}'. Applying effects.");
        foreach (var effect in activity.needEffects)
        {
            if (_runtimeData.needs.ContainsKey(effect.needName))
            {
                var need = _runtimeData.needs[effect.needName];
                need.currentValue = Mathf.Clamp(need.currentValue + effect.effectValue, 0, 100);
                //Debug.Log($"   -> Need '{need.name}' is now {need.currentValue}");
            }
        }


        if (_currentState == AutonomousState.PerformingTaskAction)
        {
            TransitionToState(AutonomousState.WaitingAfterTask);
        }
    }
    private IEnumerator TaskCompletionDelayCoroutine()
    {
        yield return new WaitForSeconds(_config.taskCompletionDelay);
        if (_currentState == AutonomousState.WaitingAfterTask)
        {
            TransitionToState(AutonomousState.Idle);
        }
    }

    private void UpdateSearchingBehavior()
    {
        // 1. SUCCESS CONDITION: Have we found what we were looking for?
        foreach (var rememberedActivity in _runtimeData.rememberedActivityLocations)
        {
            ActivityObject activity = ActivityManager.Instance.GetActivityByID(rememberedActivity.Key);
            foreach (var effect in activity.needEffects)
            {
                if (effect.needName == _currentSearchNeed && effect.effectValue < 0)
                {
                    Debug.Log($"<color=green>SUCCESS!</color> Found '{activity.activityID}' while searching for '{_currentSearchNeed}'. Stopping search.");
                    TransitionToState(AutonomousState.Idle);
                    return;
                }
            }
        }

        // 2. FAILURE CONDITION: Have we been searching this zone for too long?
        _timeSpentSearchingInCurrentZone += Time.deltaTime;
        if (_timeSpentSearchingInCurrentZone > MaxSearchTimePerZone)
        {
            Debug.LogWarning($"[AI] Gave up searching in '{_currentSearchZone.zoneName}' for '{_currentSearchNeed}' after {MaxSearchTimePerZone}s.");
            TransitionToState(AutonomousState.Idle);
            return;
        }

        // 3. CONTINUATION: If we have arrived and are NOT already finding a new point, start the process.
        if (_agent != null && !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            // THIS IS THE CRITICAL GUARD:
            // "Are we currently busy finding a point? No? OK, let's start."
            if (!_isFindingNewPoint)
            {
                // Set the flag to true to "lock" this process.
                _isFindingNewPoint = true;
                // Start the coroutine that handles turning, waiting, AND finding the next point.
                StartCoroutine(WaitAndFindNewPoint());
            }
        }
    }

    private IEnumerator WaitAndFindNewPoint()
    {
        // --- The "Scan" Phase: Find the most open direction to look ---
        Vector3 bestDirection = transform.forward;
        float maxDistance = 0f;
        const int rayCount = 9;
        const float scanAngle = 120f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -scanAngle / 2 + (scanAngle / (rayCount - 1)) * i;
            Quaternion rotation = Quaternion.AngleAxis(angle, transform.up);
            Vector3 direction = rotation * transform.forward;

            float distance = searchMeanderDistance;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit hit, distance, wallLayerMask))
            {
                distance = hit.distance;
            }

            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestDirection = direction;
            }
        }

        // --- The "Turn" Phase: Smoothly rotate the transform to face the best direction ---
        float turnDuration = 1.5f; // How long the turn should take.
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = transform.rotation; // Default to current rotation

        // Only set a new rotation if we found a valid direction
        if (bestDirection != Vector3.zero)
        {
            endRotation = Quaternion.LookRotation(bestDirection);
        }
        else
        {
            // Fallback: If something went wrong, just do a random small turn.
            float randomAngle = Random.Range(-90f, 90f);
            endRotation = startRotation * Quaternion.Euler(0, randomAngle, 0);
        }

        float timer = 0f;
        while (timer < turnDuration)
        {
            // Smoothly interpolate from the start to the end rotation over the duration.
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, timer / turnDuration);
            timer += Time.deltaTime;
            yield return null; // Wait for the next frame
        }
        transform.rotation = endRotation; // Snap to the final rotation to ensure accuracy.

        // --- The "Act" Phase: Now that we have turned, find the next point to move to ---
        if (_currentState == AutonomousState.SearchingForActivity)
        {
            MoveToRandomPointInZone();
        }

        // CRITICAL: Unlock the guard flag so this process can be triggered again later.
        _isFindingNewPoint = false;
    }

    private void MoveToRandomPointInZone()
    {
        if (_currentSearchZone == null)
        {
            TransitionToState(AutonomousState.Idle);
            return;
        }

        BoxCollider zoneBounds = _currentSearchZone.GetComponent<BoxCollider>();

        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 forwardDirection = transform.forward;
            Vector3 rightDirection = transform.right;
            Vector3 randomForwardOffset = forwardDirection * Random.Range(searchMeanderDistance * 0.5f, searchMeanderDistance);
            Vector3 randomRightOffset = rightDirection * Random.Range(-searchMeanderWidth, searchMeanderWidth);
            Vector3 desiredPoint = transform.position + randomForwardOffset + randomRightOffset;

            float distanceToPoint = Vector3.Distance(transform.position, desiredPoint);
            if (Physics.Linecast(transform.position + Vector3.up * 0.5f, desiredPoint + Vector3.up * 0.5f, wallLayerMask))
            {
                if (i == maxAttempts - 1) Debug.LogWarning($"[AI] Meander check failed after {maxAttempts} attempts. The area might be too cramped.");
                continue;
            }

            desiredPoint = zoneBounds.bounds.ClosestPoint(desiredPoint);

            if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                _currentSearchDestination = hit.position;
                _agent.SetDestination(_currentSearchDestination);
                _agent.isStopped = false;
                Debug.Log($"Exploring '{_currentSearchZone.zoneName}' by <color=lightblue>meandering</color> to a new point: {hit.position}");
                // The line "_isFindingNewPoint = false;" has been correctly removed from this spot.
                return;
            }
        }

        Debug.LogWarning($"Could not find a valid, non-obstructed NavMesh point in {_currentSearchZone.zoneName} while meandering. Stopping search.");

        if (!string.IsNullOrEmpty(_currentSearchNeed))
        {
            _recentlyFailedSearches[_currentSearchNeed] = Time.time + searchFailureCooldown;
            Debug.LogWarning($"[AI] Adding '{_currentSearchNeed}' to failed search cooldown. Will not re-attempt for {searchFailureCooldown} seconds.");
        }

        TransitionToState(AutonomousState.Idle);
    }

    private void OnActivityObjectMoved(ActivityObject movedActivity)
    {
        // If I have a memory of this activity, that memory is now WRONG.
        // Invalidate the memory by removing it from my brain.
        if (_runtimeData.rememberedActivityLocations.ContainsKey(movedActivity.activityID))
        {
            Debug.Log($"[AI Memory] Heard that '{movedActivity.activityID}' moved. My memory of its location is now invalid. Forgetting it.");
            _runtimeData.rememberedActivityLocations.Remove(movedActivity.activityID);

            // If this is the activity I am currently heading towards, I must abort my current task!
            if (_currentActivity == movedActivity)
            {
                Debug.LogWarning("[AI] The activity I was walking towards just moved! Aborting task and going back to idle.");
                // Reset path and go back to idle to force a re-think.
                _agent.ResetPath();
                TransitionToState(AutonomousState.Idle);
            }
        }
    }

    #endregion

    #region AI Brain & Decision Making

    private void StartNpcTaskLoopIfNeeded()
    {
        if (_taskExecutionCoroutine == null)
        {
            _taskExecutionCoroutine = StartCoroutine(NpcTaskLoopCoroutine());
        }
    }

    private void StopNpcTaskLoopAndMovement()
    {
        if (_taskExecutionCoroutine != null)
        {
            StopCoroutine(_taskExecutionCoroutine);
            _taskExecutionCoroutine = null;
        }
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
        if (_currentActivity != null && _animator != null && !string.IsNullOrEmpty(_currentActivity.animationBoolName))
        {
            _animator.SetBool(_currentActivity.animationBoolName, false);
        }
        _currentActivity = null;
    }

    private IEnumerator NpcTaskLoopCoroutine()
    {
        while (true)
        {
            if (_currentState == AutonomousState.Idle)
            {
                // This is the main "thinking" tick.
                yield return new WaitForSeconds(_perception.perceptionTickRate + 0.1f);

                // No longer checks for dialogue here, NpcController will tell us.

                AiDecision decision = MakeBestDecision();

                switch (decision.type)
                {
                    case AiDecisionType.PerformActivity:
                        _currentActivity = (ActivityObject)decision.targetObject;
                        TransitionToState(AutonomousState.MovingToTask);
                        break;

                    case AiDecisionType.SearchZone:
                        _currentSearchZone = (SearchableZone)decision.targetObject;
                        _currentSearchNeed = decision.reason;
                        TransitionToState(AutonomousState.SearchingForActivity);
                        break;

                    case AiDecisionType.DoNothing:
                        yield return new WaitForSeconds(_config.taskCompletionDelay);
                        break;
                }
            }
            yield return null; // Wait for the next frame if not idle.
        }
    }

    private AiDecision MakeBestDecision()
    {
        Debug.Log($"==================== {_config.npcName} IS THINKING... ====================");
        string needsStatus = "";
        foreach (var need in _runtimeData.needs) { needsStatus += $"{need.Key}: {need.Value.currentValue:F1} | "; }
        //Debug.Log($"[STATUS] Current Needs -> {needsStatus}");

        AiDecision bestDecision = new AiDecision(AiDecisionType.DoNothing, null, "Bored");
        float highestScore = 0f;
        const float minimumScoreToAct = 0.01f;

        // The ONLY source of activities for direct action is the NPC's memory.
        // Perception updates memory, and this function reads from it.
        Debug.Log($"[MEMORY] Knows the location of {_runtimeData.rememberedActivityLocations.Count} activities.");

        // Create a copy of the keys to prevent modification errors during iteration.
        List<string> rememberedActivityIDs = new List<string>(_runtimeData.rememberedActivityLocations.Keys);

        // === PASS 1: FULFILL URGENT NEEDS WITH A KNOWN ACTIVITY ===
        Debug.Log("--- PASS 1: Evaluating direct actions based on memory. ---");
        foreach (string activityID in rememberedActivityIDs)
        {
            ActivityObject activity = ActivityManager.Instance.GetActivityByID(activityID);
            if (activity == null)
            {
                // The activity was likely destroyed. Clean up our memory.
                _runtimeData.rememberedActivityLocations.Remove(activityID);
                Debug.LogWarning($"[AI Memory] Purged memory of '{activityID}' because it no longer exists.");
                continue;
            }

            float score = ScoreKnownActivity(activity);

            if (score > highestScore && score > minimumScoreToAct)
            {
                //Debug.Log($"[PASS 1] Considering '{activity.activityID}' for need '{GetPrimaryReasonForActivity(activity)}'. Score: {score:F2}");
                //Debug.Log($"<color=green>*** NEW BEST DECISION (DIRECT ACTION) ***</color> -> '{activity.activityID}' with score {score:F2}.");
                highestScore = score;
                bestDecision = new AiDecision(AiDecisionType.PerformActivity, activity, GetPrimaryReasonForActivity(activity));
            }
        }

        // === PASS 2: SEARCH FOR A SOLUTION TO AN URGENT NEED ===
        //Debug.Log("--- PASS 2: Evaluating searching a zone for needs. ---");
        foreach (var needEntry in _runtimeData.needs)
        {
            SearchableZone targetZone;
            float score = ScoreSearchAction(needEntry.Key, out targetZone);

            // *** FIX: Add a check to ensure targetZone is not null before using it. ***
            if (targetZone != null && score > highestScore && score > minimumScoreToAct)
            {
                string searchType = _runtimeData.learnedZoneContents.ContainsKey(targetZone.zoneName) && _runtimeData.learnedZoneContents[targetZone.zoneName].Contains(needEntry.Key) ? "Learned" : "Desperate";
                Debug.Log($"[PASS 2] Considering '{searchType}' search for '{needEntry.Key}' in zone '{targetZone.zoneName}'. Score: {score:F2}");
                Debug.Log($"<color=cyan>*** NEW BEST DECISION (SEARCH) ***</color> -> Search '{targetZone.zoneName}' with score {score:F2}.");
                highestScore = score;
                bestDecision = new AiDecision(AiDecisionType.SearchZone, targetZone, needEntry.Key);
            }
        }

        // === PASS 3: IDLE/GOAL BEHAVIORS ===
        const float idleThreshold = 0.1f;
        if (highestScore < idleThreshold)
        {
            //Debug.Log($"--- PASS 3: No urgent needs. Evaluating idle/goal behaviors. ---");
            // We re-use the same list of remembered activity IDs.
            foreach (string activityID in rememberedActivityIDs)
            {
                ActivityObject activity = ActivityManager.Instance.GetActivityByID(activityID);
                if (activity == null) continue;

                // Check for activities matching personal goals
                if (DoesActivityMatchGoals(activity))
                {
                    float goalScore = 0.08f;
                    if (goalScore > highestScore)
                    {
                        highestScore = goalScore;
                        bestDecision = new AiDecision(AiDecisionType.PerformActivity, activity, "Pursuing personal goals");
                    }
                }

                // Check for ambient activities (no needs) to pass the time
                if (activity.needEffects.Count == 0)
                {
                    float ambientScore = 0.05f;
                    if (ambientScore > highestScore)
                    {
                        highestScore = ambientScore;
                        bestDecision = new AiDecision(AiDecisionType.PerformActivity, activity, "Passing the time");
                    }
                }
            }
        }

        string targetName = bestDecision.targetObject != null ? ((MonoBehaviour)bestDecision.targetObject).name : "None";
        //Debug.Log($"<color=yellow>--- FINAL DECISION: {bestDecision.type} | Target: {targetName} | Reason: {bestDecision.reason} | Final Score: {highestScore:F2} ---</color>\n");
        return bestDecision;
    }


    // --- UTILITY ---
    private SearchableZone GetZoneForPosition(Vector3 position)
    {
        foreach (var zone in SearchableZone.AllZones)
        {
            if (zone.GetComponent<Collider>().bounds.Contains(position))
            {
                return zone;
            }
        }
        return null;
    }

    // NpcBrain.cs (Replace this one method)

    private float ScoreKnownActivity(ActivityObject activity)
    {
        // 1. Check if the activity or its effects list is null. If so, it has a score of 0.
        if (activity == null || activity.needEffects == null)
        {
            return 0f;
        }
        // --- End of Fix ---

        float highestScore = 0f;

        foreach (var effect in activity.needEffects)
        {
            if (_runtimeData.needs.ContainsKey(effect.needName) && effect.effectValue < 0)
            {
                float needValue = _runtimeData.needs[effect.needName].currentValue;
                float urgency = needValue / 100f;
                float score = Mathf.Pow(urgency, 2);

                if (score > highestScore)
                {
                    highestScore = score;
                }
            }
        }
        return highestScore;
    }

    private float ScoreSearchAction(string needToSolve, out SearchableZone bestZone)
    {
        bestZone = null;
        if (_recentlyFailedSearches.ContainsKey(needToSolve) && Time.time < _recentlyFailedSearches[needToSolve])
        {
            return 0; // This action failed recently, so ignore it for now. Score is 0.
        }
        if (!_runtimeData.needs.ContainsKey(needToSolve)) return 0;

        float needValue = _runtimeData.needs[needToSolve].currentValue;
        if (needValue < desperationThreshold) return 0;

        float urgency = needValue / 100f;
        float score = Mathf.Pow(urgency, 2); // Base score from desperation

        SearchableZone bestLearnedZone = null;
        float closestLearnedDist = float.MaxValue;

        // First, try to find the best ZONE we have LEARNED about
        foreach (var zone in SearchableZone.AllZones)
        {
            if (_runtimeData.learnedZoneContents.ContainsKey(zone.zoneName) &&
                _runtimeData.learnedZoneContents[zone.zoneName].Contains(needToSolve))
            {
                float distance = Vector3.Distance(transform.position, zone.transform.position);
                if (distance < closestLearnedDist)
                {
                    closestLearnedDist = distance;
                    bestLearnedZone = zone;
                }
            }
        }

        if (bestLearnedZone != null)
        {
            // We found a zone we know about. This is a confident, high-value search.
            bestZone = bestLearnedZone;
            // The score remains high because we are confident.
        }
        else
        {
            // We are desperate but have NO IDEA where to go. We must make a desperate guess.
            if (SearchableZone.AllZones.Count > 0)
            {
                //Debug.LogWarning($"[AI] NPC '{_config.npcName}' is desperate for '{needToSolve}' but has no learned zone. Picking a random zone to search.");
                // Pick any random zone as a last resort.
                bestZone = SearchableZone.AllZones[Random.Range(0, SearchableZone.AllZones.Count)];
                // Penalize the score because this is a blind guess, but keep it high enough to be worthwhile.
                score *= 0.5f;
            }
            else
            {
                return 0; // No zones exist in the world to even search.
            }
        }

        // --- THIS IS THE FIX ---
        // Anti-Override Logic: Penalize searching if we already have a direct solution in our memory.
        // We check _runtimeData.rememberedActivityLocations instead of perception.KnownActivities.
        foreach (var rememberedActivity in _runtimeData.rememberedActivityLocations)
        {
            ActivityObject activity = ActivityManager.Instance.GetActivityByID(rememberedActivity.Key);
            if (activity == null) continue; // Skip if activity was destroyed

            foreach (var effect in activity.needEffects)
            {
                if (effect.needName == needToSolve && effect.effectValue < 0)
                {
                    // We know a direct solution! Drastically reduce the desire to search.
                    //Debug.Log($"[AI] ScoreSearchAction is penalizing the search for '{needToSolve}' because it already knows about '{activity.activityID}'.");
                    score *= 0.1f;
                    // We only need to find one solution to penalize the score, so we can break.
                    goto FoundKnownSolution; // Exit both loops
                }
            }
        }

        FoundKnownSolution:
        return score;
    }

    #endregion


    // --- trash region ---

    public virtual void PerformCutsceneAction(string methodName)
    {
        // Implement as needed in derived classes
    }



}