using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; 
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    private Cutscene currentCutscene;
    private int currentActionIndex;
    private bool isPlayingCutscene = false;

    private PointAndClickMovement playerMovement;
    private BaseNPC playerCharacter; // If your player is a BaseNPC or can be controlled like one
    private List<BaseNPC> activelyManagedNPCs = new List<BaseNPC>();
    private List<GameObject> temporarySceneObjects = new List<GameObject>(); // For dynamically spawned objects
    private CameraFollow mainCameraFollowScript;
    private bool wasCameraFollowOriginallyEnabled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); // If your CutsceneManager needs to persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        FindPlayerReferences();
        if (Camera.main != null)
        {
            mainCameraFollowScript = Camera.main.GetComponent<CameraFollow>();
        }
        if (mainCameraFollowScript == null)
        {
            Debug.LogWarning("[CutsceneManager] CameraFollow script not found on the main camera. Cutscene camera control might conflict if another follow script is active.");
        }
    }

    private void FindPlayerReferences()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerMovement = playerObj.GetComponent<PointAndClickMovement>();
            playerCharacter = playerObj.GetComponent<BaseNPC>(); // Assign if player has BaseNPC

            if (playerMovement == null) Debug.LogWarning("[CutsceneManager] Player found, but PointAndClickMovement component is missing.");
            // playerCharacter can be null if the player GameObject doesn't have a BaseNPC component.
        }
        else
        {
            Debug.LogError("[CutsceneManager] Player object with tag 'Player' not found!");
        }
    }

    public bool IsCutscenePlaying => isPlayingCutscene;

    public void StartCutscene(Cutscene cutsceneToPlay, BaseNPC primaryInstigator = null)
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
        temporarySceneObjects.Clear(); // Clear any objects from previous cutscenes

        bool hasCameraControlAction = false;
        foreach (var action in currentCutscene.actions)
        {
            if (action.type == CutsceneActionType.CameraControl /* || action.type == CutsceneActionType.ActivateCinemachineVirtualCamera */)
            {
                hasCameraControlAction = true;
                break;
            }
        }

        if (mainCameraFollowScript != null && hasCameraControlAction)
        {
            wasCameraFollowOriginallyEnabled = mainCameraFollowScript.enabled; // Store if it was enabled
            mainCameraFollowScript.SetManualControl(true); // Use the new method
            // mainCameraFollowScript.enabled = false; // Alternative: just disable the component
            Debug.Log("[CutsceneManager] Disabled CameraFollow script for cutscene.");
        }

        if (playerMovement != null) playerMovement.LockMovement();
        else Debug.LogWarning("[CutsceneManager] PlayerMovement script not found; cannot lock player movement.");


        // Pause involved NPCs
        activelyManagedNPCs.Clear();
        if (primaryInstigator != null && !activelyManagedNPCs.Contains(primaryInstigator))
        {
            activelyManagedNPCs.Add(primaryInstigator);
        }

        // Collect NPCs from override list
        foreach (var npcOverrideIdentifier in currentCutscene.involvedNPCsOverrideIdentifiers)
        {
            if (string.IsNullOrEmpty(npcOverrideIdentifier)) continue;
            BaseNPC npc = FindNPCByIdentifier(npcOverrideIdentifier);
            if (npc != null && !activelyManagedNPCs.Contains(npc))
            {
                activelyManagedNPCs.Add(npc);
            }
            else if (npc == null)
            {
                Debug.LogWarning($"[CutsceneManager] Involved NPC override '{npcOverrideIdentifier}' not found in scene.");
            }
        }

        // Collect NPCs from individual actions (if not already added)
        foreach (var action in currentCutscene.actions)
        {
            if (!action.targetIsPlayer && !string.IsNullOrEmpty(action.targetNPCIdentifier))
            {
                BaseNPC npc = FindNPCByIdentifier(action.targetNPCIdentifier);
                if (npc != null && !activelyManagedNPCs.Contains(npc))
                {
                    activelyManagedNPCs.Add(npc);
                }
                // Warning for not found NPC will be handled during action processing
            }
        }

        // Pause all collected NPCs
        foreach (var npc in activelyManagedNPCs)
        {
            npc.PauseAIForCutscene(true);
        }
        if (playerCharacter != null && !activelyManagedNPCs.Contains(playerCharacter)) // Also pause player if it's an NPC and targeted implicitly
        {
            // Check if player needs pausing (e.g., if any action targets the player specifically)
            bool playerIsTargeted = false;
            foreach (var action in currentCutscene.actions) if (action.targetIsPlayer) playerIsTargeted = true;

            if (playerIsTargeted)
            {
                playerCharacter.PauseAIForCutscene(true);
                if (!activelyManagedNPCs.Contains(playerCharacter)) activelyManagedNPCs.Add(playerCharacter);
            }
        }


        ExecuteNextAction();
    }

    private void ExecuteNextAction()
    {
        if (!isPlayingCutscene) return; // Cutscene might have been aborted

        if (currentActionIndex >= currentCutscene.actions.Count)
        {
            EndCutscene();
            return;
        }

        CutsceneAction action = currentCutscene.actions[currentActionIndex];
        Debug.Log($"[CutsceneManager] Executing Action {currentActionIndex}: {action.actionName} (Type: {action.type})");
        StartCoroutine(ProcessAction(action));
    }

    private IEnumerator ProcessAction(CutsceneAction action)
    {
        BaseNPC characterForAction = null;
        NavMeshAgent agentForAction = null; // For non-BaseNPC player movement
        Animator animatorForAction = null; // For non-BaseNPC player animation

        // --- Determine the target character/agent ---
        if (action.targetIsPlayer)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                characterForAction = playerObj.GetComponent<BaseNPC>(); // This can be null
                agentForAction = playerObj.GetComponent<NavMeshAgent>();
                animatorForAction = playerObj.GetComponent<Animator>();

                if (characterForAction == null && agentForAction == null) // Need at least an agent for movement
                {
                    Debug.LogWarning($"[CutsceneManager] Player targeted for action '{action.actionName}', but no BaseNPC or NavMeshAgent found on player.");
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

        // Resolve targetTransform if using marker name
        Transform resolvedTargetTransform = action.targetTransform; // Default (used if prefab is assigned directly for example)
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

        // Resolve gameObjectToToggle if using name
        GameObject resolvedGameObjectToToggle = action.gameObjectToToggle; // Default
        if (!string.IsNullOrEmpty(action.gameObjectToToggleName))
        {
            resolvedGameObjectToToggle = GameObject.Find(action.gameObjectToToggleName);
            if (resolvedGameObjectToToggle == null)
            {
                Debug.LogWarning($"[CutsceneManager] Could not find gameObjectToToggle by name: '{action.gameObjectToToggleName}' for action '{action.actionName}'.");
            }
        }

        // --- Execute Action ---
        switch (action.type)
        {
            case CutsceneActionType.Wait:
                if (action.duration > 0) yield return new WaitForSeconds(action.duration);
                break;

            case CutsceneActionType.MoveCharacter:
                if (agentForAction != null && agentForAction.isOnNavMesh)
                {
                    Vector3 destination = action.targetPosition; // Default to Vector3
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
                        if (isPlayingCutscene && agentForAction.isOnNavMesh) agentForAction.ResetPath(); // Stop once arrived or if cutscene still playing
                    }
                }
                else if (agentForAction == null) Debug.LogWarning($"MoveCharacter: Target for action '{action.actionName}' has no NavMeshAgent or could not be resolved.");
                else if (!agentForAction.isOnNavMesh) Debug.LogWarning($"MoveCharacter: Agent for action '{action.actionName}' is not on a NavMesh.");
                break;
            case CutsceneActionType.TeleportCharacter:
                Transform actorTransformToTeleport = null;
                NavMeshAgent agentToTeleport = null; // To potentially disable/re-enable or warp

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
                    BaseNPC npc = FindNPCByIdentifier(action.targetNPCIdentifier);
                    if (npc != null)
                    {
                        actorTransformToTeleport = npc.transform;
                        agentToTeleport = npc.Agent;
                    }
                }

                if (actorTransformToTeleport != null)
                {
                    Vector3 teleportDestination = action.targetPosition; // Default to Vector3
                    if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                    {
                        teleportDestination = resolvedTargetTransform.position;
                    }

                    // --- Teleport Logic ---
                    if (agentToTeleport != null && agentToTeleport.isOnNavMesh)
                    {
                        // It's good practice to disable the agent, teleport, then re-enable.
                        // This prevents potential issues with the agent trying to fight the teleportation.
                        bool agentWasEnabled = agentToTeleport.enabled;
                        agentToTeleport.enabled = false; // Disable agent
                        actorTransformToTeleport.position = teleportDestination;
                        // Optionally set rotation too if your marker/target has it
                        if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                        {
                            actorTransformToTeleport.rotation = resolvedTargetTransform.rotation;
                        }
                        agentToTeleport.enabled = agentWasEnabled; // Re-enable agent

                        // If the agent was active, warping it is better after re-enabling
                        // to ensure it snaps correctly to the NavMesh at the new location.
                        if (agentWasEnabled)
                        {
                            if (agentToTeleport.isOnNavMesh)
                            { // Double check after re-enable
                                agentToTeleport.Warp(teleportDestination);
                                Debug.Log($"[CutsceneManager] Warped agent '{actorTransformToTeleport.name}' to {teleportDestination}");
                            }
                            else
                            {
                                Debug.LogWarning($"[CutsceneManager] Agent '{actorTransformToTeleport.name}' not on NavMesh after teleport and re-enable. Warp skipped.");
                            }
                        }
                    }
                    else // If no agent or not on NavMesh, just set transform position
                    {
                        actorTransformToTeleport.position = teleportDestination;
                        if (action.useTargetTransformForPosition && resolvedTargetTransform != null)
                        {
                            actorTransformToTeleport.rotation = resolvedTargetTransform.rotation;
                        }
                    }
                    Debug.Log($"[CutsceneManager] Teleported '{actorTransformToTeleport.name}' to {teleportDestination}");

                    // Optional small delay after teleport if needed for systems to catch up
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
                if (animatorForAction != null && !string.IsNullOrEmpty(action.stringParameter))
                {
                    // Assuming stringParameter is an animation trigger name. Adapt if using bools/floats.
                    animatorForAction.SetTrigger(action.stringParameter);
                    Debug.Log($"[CutsceneManager] Playing animation '{action.stringParameter}' on '{(characterForAction != null ? characterForAction.name : (agentForAction != null ? agentForAction.name : "Unknown Target"))}'");


                    if (action.waitForCompletion && action.duration > 0) // Simple wait, or use animation events
                    {
                        yield return new WaitForSeconds(action.duration);
                    }
                }
                else if (animatorForAction == null) Debug.LogWarning($"PlayAnimation: Target for action '{action.actionName}' has no Animator or could not be resolved.");
                else Debug.LogWarning($"PlayAnimation: Animation name (stringParameter) is empty for action '{action.actionName}'.");
                break;

            case CutsceneActionType.ShowDialogueSegment:
                // For dialogue, we generally need a BaseNPC as the speaker.
                if (DialogueManager.Instance != null && characterForAction != null)
                {
                    TextAsset dialogueFile = action.dialogueFile != null ? action.dialogueFile : characterForAction.dialogueFile;
                    if (dialogueFile != null && !string.IsNullOrEmpty(action.stringParameter))
                    {
                        bool segmentCompleted = false;
                        DialogueManager.Instance.StartDialogueSegment(characterForAction, dialogueFile, action.stringParameter, () => {
                            segmentCompleted = true;
                        });
                        yield return new WaitUntil(() => segmentCompleted || !isPlayingCutscene);
                    }
                    else Debug.LogWarning($"ShowDialogueSegment: Dialogue file or start node ID missing for action '{action.actionName}'. NPC: {characterForAction.name}");
                }
                else if (DialogueManager.Instance == null) Debug.LogWarning($"ShowDialogueSegment: DialogueManager instance not found for action '{action.actionName}'.");
                else if (characterForAction == null) Debug.LogWarning($"ShowDialogueSegment: Target NPC (BaseNPC component) could not be resolved or is not suitable for dialogue for action '{action.actionName}'.");
                break;

            case CutsceneActionType.FaceDirection:
                Transform actorTransform = characterForAction?.transform ?? agentForAction?.transform;
                if (actorTransform != null && resolvedTargetTransform != null)
                {
                    Vector3 direction = (resolvedTargetTransform.position - actorTransform.position).normalized;
                    direction.y = 0; // Keep upright
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        // Can Lerp for smoothness if desired
                        float time = 0;
                        float rotationDuration = 0.5f; // Adjust as needed
                        Quaternion startRotation = actorTransform.rotation;
                        while (time < rotationDuration && isPlayingCutscene)
                        {
                            actorTransform.rotation = Quaternion.Slerp(startRotation, lookRotation, time / rotationDuration);
                            time += Time.deltaTime;
                            yield return null;
                        }
                        if (isPlayingCutscene) actorTransform.rotation = lookRotation; // Ensure final rotation
                    }
                }
                else if (actorTransform == null) Debug.LogWarning($"FaceDirection: Target actor for action '{action.actionName}' could not be resolved.");
                else Debug.LogWarning($"FaceDirection: TargetTransform for action '{action.actionName}' could not be resolved or is null.");
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
                else if (characterForAction == null) Debug.LogWarning($"CustomNPCMethod: Target NPC (BaseNPC component) could not be resolved for action '{action.actionName}'.");
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
                            if (playerMovement != null) playerMovement.UnlockMovement();


                            // Resume NPCs in the *current* scene before unloading, just in case something goes wrong
                            // or if the scene load is cancelled by some external factor (unlikely with direct load).
                            // This is more of a "just in case" for the current scene's state.
                            foreach (var npc in activelyManagedNPCs)
                            {
                                if (npc != null) npc.PauseAIForCutscene(false);
                            }
                            activelyManagedNPCs.Clear();
                            if (playerCharacter != null && playerCharacter.IsPausedByCutscene)
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


        if (playerMovement != null) playerMovement.UnlockMovement();
        else Debug.LogWarning("[CutsceneManager] PlayerMovement script not found; cannot unlock player movement.");

        // Resume NPCs
        foreach (var npc in activelyManagedNPCs)
        {
            if (npc != null) npc.PauseAIForCutscene(false);
        }
        activelyManagedNPCs.Clear();

        // If player character was paused explicitly and is a BaseNPC
        if (playerCharacter != null && playerCharacter.IsPausedByCutscene)
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
    private BaseNPC FindNPCByIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return null;
        GameObject npcObject = GameObject.Find(identifier);
        if (npcObject != null)
        {
            return npcObject.GetComponent<BaseNPC>();
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