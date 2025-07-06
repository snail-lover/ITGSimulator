using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the lifecycle and execution of cutscenes, including player/NPC control,
/// camera overrides, and scene/world state changes.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static CutsceneManager Instance { get; private set; }

    private Cutscene currentCutscene;
    private int currentActionIndex;
    private bool isPlayingCutscene = false;

    private PointAndClickMovement playerMovement;
    private NpcController playerCharacter; // Reference to player as NPC if applicable
    private List<NpcController> activelyManagedNPCs = new List<NpcController>();
    private List<GameObject> temporarySceneObjects = new List<GameObject>(); // Tracks objects spawned during cutscenes
    private CameraFollow mainCameraFollowScript;
    private bool wasCameraFollowOriginallyEnabled;

    private void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            // Uncomment if persistence across scenes is needed:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        FindPlayerReferences();

        // Cache main camera's follow script for camera control during cutscenes
        if (Camera.main != null)
        {
            mainCameraFollowScript = Camera.main.GetComponent<CameraFollow>();
        }
        if (mainCameraFollowScript == null)
        {
            Debug.LogWarning("[CutsceneManager] CameraFollow script not found on the main camera. Cutscene camera control might conflict if another follow script is active.");
        }
    }

    /// <summary>
    /// Finds and caches references to the player movement and NPC controller components.
    /// </summary>
    private void FindPlayerReferences()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerMovement = playerObj.GetComponent<PointAndClickMovement>();
            playerCharacter = playerObj.GetComponent<NpcController>();

            if (playerMovement == null)
                Debug.LogWarning("[CutsceneManager] Player found, but PointAndClickMovement component is missing.");
            // playerCharacter can be null if the player GameObject doesn't have a NpcController component.
        }
        else
        {
            Debug.LogError("[CutsceneManager] Player object with tag 'Player' not found!");
        }
    }

    /// <summary>
    /// Returns true if a cutscene is currently playing.
    /// </summary>
    public bool IsCutscenePlaying => isPlayingCutscene;

    /// <summary>
    /// Begins playing a cutscene, locking player/NPC control and preparing camera as needed.
    /// </summary>
    /// <param name="cutsceneToPlay">The cutscene asset to play.</param>
    /// <param name="primaryInstigator">Optional: the main NPC involved in the cutscene.</param>
    public void StartCutscene(Cutscene cutsceneToPlay, NpcController primaryInstigator = null)
    {
        if (isPlayingCutscene)
        {
            Debug.LogWarning("[CutsceneManager] Tried to start a cutscene while one is already playing. Aborting new cutscene.");
            return;
        }
        if (cutsceneToPlay == null || cutsceneToPlay.actions.Count == 0)
        {
            Debug.LogError("[CutsceneManager] Cutscene is null or has no actions. Aborting.");
            return;
        }

        Debug.Log($"[CutsceneManager] Starting cutscene: {cutsceneToPlay.name}");
        currentCutscene = cutsceneToPlay;
        currentActionIndex = 0;
        isPlayingCutscene = true;
        temporarySceneObjects.Clear();

        // Check if any action requires camera control
        bool hasCameraControlAction = false;
        foreach (var action in currentCutscene.actions)
        {
            if (action.type == CutsceneActionType.CameraControl)
            {
                hasCameraControlAction = true;
                break;
            }
        }

        // If camera control is needed, set camera to manual mode
        if (mainCameraFollowScript != null && hasCameraControlAction)
        {
            wasCameraFollowOriginallyEnabled = mainCameraFollowScript.enabled;
            mainCameraFollowScript.SetManualControl(true);
            Debug.Log("[CutsceneManager] Disabled CameraFollow script for cutscene.");
        }

        // Lock player movement during cutscene
        if (playerMovement != null)
            playerMovement.HardLockPlayerMovement();
        else
            Debug.LogWarning("[CutsceneManager] PlayerMovement script not found; cannot lock player movement.");

        // Collect and pause all involved NPCs
        activelyManagedNPCs.Clear();
        if (primaryInstigator != null && !activelyManagedNPCs.Contains(primaryInstigator))
        {
            activelyManagedNPCs.Add(primaryInstigator);
        }

        // Add NPCs from override list
        foreach (var npcOverrideIdentifier in currentCutscene.involvedNPCsOverrideIdentifiers)
        {
            if (string.IsNullOrEmpty(npcOverrideIdentifier)) continue;
            NpcController npc = FindNPCByIdentifier(npcOverrideIdentifier);
            if (npc != null && !activelyManagedNPCs.Contains(npc))
            {
                activelyManagedNPCs.Add(npc);
            }
            else if (npc == null)
            {
                Debug.LogWarning($"[CutsceneManager] Involved NPC override '{npcOverrideIdentifier}' not found in scene.");
            }
        }

        // Add NPCs targeted by actions
        foreach (var action in currentCutscene.actions)
        {
            if (!action.targetIsPlayer && !string.IsNullOrEmpty(action.targetNPCIdentifier))
            {
                NpcController npc = FindNPCByIdentifier(action.targetNPCIdentifier);
                if (npc != null && !activelyManagedNPCs.Contains(npc))
                {
                    activelyManagedNPCs.Add(npc);
                }
            }
        }

        // Pause all collected NPCs
        foreach (var npc in activelyManagedNPCs)
        {
            npc.PauseAIForCutscene(true);
        }

        // Pause player as NPC if targeted by any action
        if (playerCharacter != null && !activelyManagedNPCs.Contains(playerCharacter))
        {
            bool playerIsTargeted = false;
            foreach (var action in currentCutscene.actions)
                if (action.targetIsPlayer) playerIsTargeted = true;

            if (playerIsTargeted)
            {
                playerCharacter.PauseAIForCutscene(true);
                if (!activelyManagedNPCs.Contains(playerCharacter)) activelyManagedNPCs.Add(playerCharacter);
            }
        }

        ExecuteNextAction();
    }

    /// <summary>
    /// Executes the next action in the current cutscene.
    /// </summary>
    private void ExecuteNextAction()
    {
        if (!isPlayingCutscene) return;

        if (currentActionIndex >= currentCutscene.actions.Count)
        {
            EndCutscene();
            return;
        }

        CutsceneAction action = currentCutscene.actions[currentActionIndex];
        Debug.Log($"[CutsceneManager] Executing Action {currentActionIndex}: {action.actionName} (Type: {action.type})");
        StartCoroutine(ProcessAction(action));
    }

    /// <summary>
    /// Processes a single cutscene action, handling all supported action types.
    /// </summary>
    private IEnumerator ProcessAction(CutsceneAction action)
    {
        NpcController characterForAction = null;
        NavMeshAgent agentForAction = null;
        Animator animatorForAction = null;

        // Resolve target: player or NPC
        if (action.targetIsPlayer)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                characterForAction = playerObj.GetComponent<NpcController>();
                agentForAction = playerObj.GetComponent<NavMeshAgent>();
                animatorForAction = playerObj.GetComponent<Animator>();

                if (characterForAction == null && agentForAction == null)
                {
                    Debug.LogWarning($"[CutsceneManager] Player targeted for action '{action.actionName}', but no NpcController or NavMeshAgent found on player.");
                }
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] Player object not found for action '{action.actionName}' targeting player.");
            }
        }
        else if (!string.IsNullOrEmpty(action.targetNPCIdentifier))
        {
            characterForAction = FindNPCByIdentifier(action.targetNPCIdentifier);
            if (characterForAction != null)
            {
                agentForAction = characterForAction.Agent;
                animatorForAction = characterForAction.NpcAnimator;
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] Could not find Target NPC '{action.targetNPCIdentifier}' for action '{action.actionName}'.");
            }
        }

        // Resolve target transform if using marker name
        Transform resolvedTargetTransform = action.targetTransform;
        if (!string.IsNullOrEmpty(action.targetTransformMarkerName))
        {
            GameObject markerObj = GameObject.Find(action.targetTransformMarkerName);
            if (markerObj != null)
            {
                resolvedTargetTransform = markerObj.transform;
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] Could not find targetTransform marker: '{action.targetTransformMarkerName}' for action '{action.actionName}'.");
            }
        }

        // Resolve GameObject to toggle if using name
        GameObject resolvedGameObjectToToggle = action.gameObjectToToggle;
        if (!string.IsNullOrEmpty(action.gameObjectToToggleName))
        {
            resolvedGameObjectToToggle = GameObject.Find(action.gameObjectToToggleName);
            if (resolvedGameObjectToToggle == null)
            {
                Debug.LogWarning($"[CutsceneManager] Could not find gameObjectToToggle by name: '{action.gameObjectToToggleName}' for action '{action.actionName}'.");
            }
        }

        // Handle each action type
        switch (action.type)
        {
            case CutsceneActionType.Wait:
                if (action.duration > 0) yield return new WaitForSeconds(action.duration);
                break;

            case CutsceneActionType.MoveCharacter:
                // Move NPC or player to a target position or marker
                if (agentForAction != null && agentForAction.isOnNavMesh)
                {
                    Vector3 destination = action.targetPosition;
                    if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                    {
                        destination = resolvedTargetTransform.position;
                    }

                    agentForAction.SetDestination(destination);
                    agentForAction.isStopped = false;

                    if (action.waitForCompletion)
                    {
                        while (isPlayingCutscene && (agentForAction.pathPending || (agentForAction.isOnNavMesh && agentForAction.remainingDistance > agentForAction.stoppingDistance)))
                        {
                            yield return null;
                        }
                        if (isPlayingCutscene && agentForAction.isOnNavMesh) agentForAction.ResetPath();
                    }
                }
                else if (agentForAction == null)
                    Debug.LogWarning($"MoveCharacter: Target for action '{action.actionName}' has no NavMeshAgent or could not be resolved.");
                else if (!agentForAction.isOnNavMesh)
                    Debug.LogWarning($"MoveCharacter: Agent for action '{action.actionName}' is not on a NavMesh.");
                break;

            case CutsceneActionType.TeleportCharacter:
                // Instantly move NPC or player to a position or marker
                Transform actorTransformToTeleport = null;
                NavMeshAgent agentToTeleport = null;

                if (action.targetIsPlayer)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                    if (playerObj != null)
                    {
                        actorTransformToTeleport = playerObj.transform;
                        agentToTeleport = playerObj.GetComponent<NavMeshAgent>();
                    }
                }
                else if (!string.IsNullOrEmpty(action.targetNPCIdentifier))
                {
                    NpcController npc = FindNPCByIdentifier(action.targetNPCIdentifier);
                    if (npc != null)
                    {
                        actorTransformToTeleport = npc.transform;
                        agentToTeleport = npc.Agent;
                    }
                }

                if (actorTransformToTeleport != null)
                {
                    Vector3 teleportDestination = action.targetPosition;
                    if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                    {
                        teleportDestination = resolvedTargetTransform.position;
                    }

                    // Teleport logic: disable agent, move, re-enable, then warp
                    if (agentToTeleport != null && agentToTeleport.isOnNavMesh)
                    {
                        bool agentWasEnabled = agentToTeleport.enabled;
                        agentToTeleport.enabled = false;
                        actorTransformToTeleport.position = teleportDestination;
                        if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                        {
                            actorTransformToTeleport.rotation = resolvedTargetTransform.rotation;
                        }
                        agentToTeleport.enabled = agentWasEnabled;
                        if (agentWasEnabled)
                        {
                            if (agentToTeleport.isOnNavMesh)
                            {
                                agentToTeleport.Warp(teleportDestination);
                                Debug.Log($"[CutsceneManager] Warped agent '{actorTransformToTeleport.name}' to {teleportDestination}");
                            }
                            else
                            {
                                Debug.LogWarning($"[CutsceneManager] Agent '{actorTransformToTeleport.name}' not on NavMesh after teleport and re-enable. Warp skipped.");
                            }
                        }
                    }
                    else
                    {
                        actorTransformToTeleport.position = teleportDestination;
                        if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                        {
                            actorTransformToTeleport.rotation = resolvedTargetTransform.rotation;
                        }
                    }
                    Debug.Log($"[CutsceneManager] Teleported '{actorTransformToTeleport.name}' to {teleportDestination}");

                    if (action.duration > 0)
                    {
                        yield return new WaitForSeconds(action.duration);
                    }
                }
                else
                {
                    Debug.LogWarning($"[CutsceneManager] TeleportCharacter: Target for action '{action.actionName}' could not be resolved.");
                }
                break;

            case CutsceneActionType.PlayAnimation:
                // Play an animation on the target character
                if (animatorForAction != null && !string.IsNullOrEmpty(action.stringParameter))
                {
                    animatorForAction.SetTrigger(action.stringParameter);
                    Debug.Log($"[CutsceneManager] Playing animation '{action.stringParameter}' on '{(characterForAction != null ? characterForAction.name : (agentForAction != null ? agentForAction.name : "Unknown Target"))}'");

                    if (action.waitForCompletion && action.duration > 0)
                    {
                        yield return new WaitForSeconds(action.duration);
                    }
                }
                else if (animatorForAction == null)
                    Debug.LogWarning($"PlayAnimation: Target for action '{action.actionName}' has no Animator or could not be resolved.");
                else
                    Debug.LogWarning($"PlayAnimation: Animation name (stringParameter) is empty for action '{action.actionName}'.");
                break;

            case CutsceneActionType.ShowDialogueSegment:
                // Show a dialogue segment for the target NPC
                if (DialogueManager.Instance != null && characterForAction != null)
                {
                    TextAsset dialogueFile = action.dialogueFile != null ? action.dialogueFile : characterForAction.npcConfig.dialogueFile;
                    if (dialogueFile != null && !string.IsNullOrEmpty(action.stringParameter))
                    {
                        bool segmentCompleted = false;
                        DialogueManager.Instance.StartDialogueSegment(characterForAction, dialogueFile, action.stringParameter, () => {
                            segmentCompleted = true;
                        });
                        yield return new WaitUntil(() => segmentCompleted || !isPlayingCutscene);
                        yield return null;
                    }
                    else
                        Debug.LogWarning($"ShowDialogueSegment: Dialogue file or start node ID missing for action '{action.actionName}'. NPC: {characterForAction.name}");
                }
                else if (DialogueManager.Instance == null)
                    Debug.LogWarning($"ShowDialogueSegment: DialogueManager instance not found for action '{action.actionName}'.");
                else if (characterForAction == null)
                    Debug.LogWarning($"ShowDialogueSegment: Target NPC (NpcController component) could not be resolved or is not suitable for dialogue for action '{action.actionName}'.");
                break;

            case CutsceneActionType.FaceDirection:
                // Rotate character to face a target transform
                Transform actorTransform = characterForAction?.transform ?? agentForAction?.transform;
                if (actorTransform != null && resolvedTargetTransform != null)
                {
                    Vector3 direction = (resolvedTargetTransform.position - actorTransform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        float time = 0;
                        float rotationDuration = 0.5f;
                        Quaternion startRotation = actorTransform.rotation;
                        while (time < rotationDuration && isPlayingCutscene)
                        {
                            actorTransform.rotation = Quaternion.Slerp(startRotation, lookRotation, time / rotationDuration);
                            time += Time.deltaTime;
                            yield return null;
                        }
                        if (isPlayingCutscene) actorTransform.rotation = lookRotation;
                    }
                }
                else if (actorTransform == null)
                    Debug.LogWarning($"FaceDirection: Target actor for action '{action.actionName}' could not be resolved.");
                else
                    Debug.LogWarning($"FaceDirection: TargetTransform for action '{action.actionName}' could not be resolved or is null.");
                break;

            case CutsceneActionType.ActivateGameObject:
                if (resolvedGameObjectToToggle != null)
                {
                    resolvedGameObjectToToggle.SetActive(action.activateState);
                }
                else Debug.LogWarning($"ActivateGameObject: gameObjectToToggle could not be resolved for action '{action.actionName}'.");
                break;

            case CutsceneActionType.CustomNPCMethod:
                if (characterForAction != null && !string.IsNullOrEmpty(action.stringParameter))
                {
                    characterForAction.PerformCutsceneAction(action.stringParameter);
                    if (action.waitForCompletion && action.duration > 0)
                    {
                        yield return new WaitForSeconds(action.duration);
                    }
                }
                else if (characterForAction == null) Debug.LogWarning($"CustomNPCMethod: Target NPC (NpcController component) could not be resolved for action '{action.actionName}'.");
                else Debug.LogWarning($"CustomNPCMethod: Method name (stringParameter) is empty for action '{action.actionName}'.");
                break;
            case CutsceneActionType.CameraControl:
                Camera cameraToControl = Camera.main; // Default
                if (!string.IsNullOrEmpty(action.cameraIdentifier))
                {
                    GameObject camObj = GameObject.Find(action.cameraIdentifier);
                    if (camObj != null)
                    {
                        cameraToControl = camObj.GetComponent<Camera>();
                        if (cameraToControl == null)
                        {
                            Debug.LogWarning($"[CutsceneManager] CameraControl: GameObject '{action.cameraIdentifier}' found, but it has no Camera component for action '{action.actionName}'. Defaulting to Camera.main.");
                            cameraToControl = Camera.main;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CutsceneManager] CameraControl: Camera GameObject '{action.cameraIdentifier}' not found for action '{action.actionName}'. Defaulting to Camera.main.");
                        cameraToControl = Camera.main;
                    }
                }

                if (cameraToControl == null)
                {
                    Debug.LogError($"[CutsceneManager] CameraControl: No camera could be resolved for action '{action.actionName}'. Main camera might be missing.");
                    break;
                }

                Vector3 targetPos = action.cameraTargetPosition;
                Quaternion targetRot = action.cameraTargetRotation;

                if (!string.IsNullOrEmpty(action.cameraTargetMarkerName))
                {
                    GameObject markerObj = GameObject.Find(action.cameraTargetMarkerName);
                    if (markerObj != null)
                    {
                        targetPos = markerObj.transform.position;
                        targetRot = markerObj.transform.rotation;
                    }
                    else
                    {
                        Debug.LogWarning($"[CutsceneManager] CameraControl: Camera target marker '{action.cameraTargetMarkerName}' not found for action '{action.actionName}'. Using direct position/rotation.");
                    }
                }

                if (action.cameraMoveDuration > 0) // Smooth move
                {
                    Vector3 startPos = cameraToControl.transform.position;
                    Quaternion startRot = cameraToControl.transform.rotation;
                    float time = 0f;

                    while (time < action.cameraMoveDuration && isPlayingCutscene)
                    {
                        time += Time.deltaTime;
                        float t = Mathf.Clamp01(time / action.cameraMoveDuration);
                        // You might want to add easing functions here for nicer movement (e.g., Mathf.SmoothStep)
                        cameraToControl.transform.position = Vector3.Lerp(startPos, targetPos, t);
                        cameraToControl.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                        yield return null;
                    }
                }

                // Ensure final position/rotation
                if (isPlayingCutscene) // Check if cutscene wasn't aborted
                {
                    cameraToControl.transform.position = targetPos;
                    cameraToControl.transform.rotation = targetRot;
                }


                if (action.cameraWaitForMoveCompletion && action.cameraMoveDuration > 0)
                {
                    // The loop above already waited if cameraMoveDuration > 0.
                    // If cameraMoveDuration was 0, it was an instant snap, so no extra wait needed.
                }
                else if (action.cameraMoveDuration <= 0) // Instant snap
                {
                    // Already done above
                }
                break;
            case CutsceneActionType.ChangeScene:
                if (!string.IsNullOrEmpty(action.sceneNameToLoad))
                {
                    Debug.Log($"[CutsceneManager] Changing scene to: {action.sceneNameToLoad}");

                    // --- IMPORTANT: Perform any necessary cleanup BEFORE changing the scene ---
                    // This might include:
                    // - Saving game state
                    // - Fading out the screen (you'd need a fade effect system)
                    // - Stopping any persistent audio that shouldn't carry over

                    // For a simple synchronous load:
                    // The cutscene itself will effectively end here because the current scene will be unloaded.
                    // No need to call ExecuteNextAction() after this.
                    isPlayingCutscene = false; // Mark cutscene as no longer playing to prevent EndCutscene issues if scene load is quick

                    // If you have DontDestroyOnLoad on CutsceneManager, it will persist.
                    // If not, it will be destroyed with the current scene.
                    // Make sure player movement is unlocked IF the new scene expects it unlocked by default.
                    // However, usually, the new scene's setup would handle player state.
                    if (playerMovement != null) playerMovement.HardLockPlayerMovement();


                    // Resume NPCs in the *current* scene before unloading, just in case something goes wrong
                    // or if the scene load is cancelled by some external factor (unlikely with direct load).
                    // This is more of a "just in case" for the current scene's state.
                    foreach (var npc in activelyManagedNPCs)
                    {
                        if (npc != null) npc.PauseAIForCutscene(false);
                    }
                    activelyManagedNPCs.Clear();
                    if (playerCharacter != null)
                    {
                        playerCharacter.PauseAIForCutscene(false);
                    }
                    if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActiveForCutscene())
                    {
                        DialogueManager.Instance.ForceEndCutsceneDialogue();
                    }
                    // Destroy any temporary objects spawned by the cutscene in *this* scene
                    foreach (var obj in temporarySceneObjects) { if (obj != null) Destroy(obj); }
                    temporarySceneObjects.Clear();


                    SceneManager.LoadScene(action.sceneNameToLoad);
                    yield break; // Stop processing further actions in this coroutine for this cutscene.
                                 // The cutscene is over.
                }
                else
                {
                    Debug.LogWarning($"[CutsceneManager] ChangeScene: Scene name to load is empty for action '{action.actionName}'.");
                }
                break;

            case CutsceneActionType.SetWorldState:
                if (WorldDataManager.Instance != null && !string.IsNullOrEmpty(action.worldStateKey))
                {
                    Debug.Log($"[CutsceneManager] Setting World State Flag '{action.worldStateKey}' to '{action.worldStateValue}'.");
                    WorldDataManager.Instance.SetGlobalFlag(action.worldStateKey, action.worldStateValue);
                }
                else if (WorldDataManager.Instance == null)
                {
                    Debug.LogError($"[CutsceneManager] SetWorldState: WorldDataManager instance not found for action '{action.actionName}'.");
                }
                else
                {
                    Debug.LogWarning($"[CutsceneManager] SetWorldState: worldStateKey is empty for action '{action.actionName}'.");
                }
                break;



            // Add other cases like SpawnObject, CameraControl etc. here

            default:
                Debug.LogWarning($"Unhandled cutscene action type: {action.type} for action '{action.actionName}'.");
                break;
        }

        if (isPlayingCutscene) // Ensure cutscene wasn't aborted during action
        {
            currentActionIndex++;
            ExecuteNextAction();
        }
    }

    public void EndCutscene()
    {
        if (!isPlayingCutscene) return;

        Debug.Log($"[CutsceneManager] Ending cutscene: {currentCutscene?.name}");

        if (mainCameraFollowScript != null && mainCameraFollowScript.isManuallyControlled) // Check if we set it to manual
        {
            mainCameraFollowScript.SetManualControl(false); // Use the new method
            // mainCameraFollowScript.enabled = wasCameraFollowOriginallyEnabled; // Restore original enabled state
            Debug.Log("[CutsceneManager] Re-enabled CameraFollow script after cutscene.");
        }

        isPlayingCutscene = false;


        if (playerMovement != null) playerMovement.HardUnlockPlayerMovement();
        else Debug.LogWarning("[CutsceneManager] PlayerMovement script not found; cannot unlock player movement.");

        // Resume NPCs
        foreach (var npc in activelyManagedNPCs)
        {
            if (npc != null) npc.PauseAIForCutscene(false);
        }
        activelyManagedNPCs.Clear();

        // If player character was paused explicitly and is a NpcController
        if (playerCharacter != null)
        {
            playerCharacter.PauseAIForCutscene(false);
        }


        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActiveForCutscene())
        {
            DialogueManager.Instance.ForceEndCutsceneDialogue();
        }

        // Destroy any temporary objects spawned by the cutscene
        foreach (var obj in temporarySceneObjects)
        {
            if (obj != null) Destroy(obj);
        }
        temporarySceneObjects.Clear();

        currentCutscene = null;
        currentActionIndex = 0; // Reset for next cutscene
        Debug.Log("[CutsceneManager] Cutscene finished and cleaned up.");
    }

    public void AbortCutscene()
    {
        if (isPlayingCutscene)
        {
            Debug.LogWarning("[CutsceneManager] Aborting current cutscene.");
            StopAllCoroutines(); // Stop action processing
            // isPlayingCutscene is set to false in EndCutscene
            EndCutscene(); // Perform cleanup
        }
    }

    // Helper to find NPC by identifier (GameObject name)
    private NpcController FindNPCByIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return null;
        GameObject npcObject = GameObject.Find(identifier);
        if (npcObject != null)
        {
            return npcObject.GetComponent<NpcController>();
        }
        return null;
    }

    // Call this from a cutscene action (e.g., SpawnObject) if you create objects
    public void RegisterTemporarySceneObject(GameObject obj)
    {
        if (obj != null && !temporarySceneObjects.Contains(obj))
        {
            temporarySceneObjects.Add(obj);
        }
    }
}