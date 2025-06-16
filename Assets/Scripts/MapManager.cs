// MapManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Core Settings")]
    public KeyCode mapToggleKey = KeyCode.M;
    public float cameraTransitionDuration = 1.0f;

    [Header("References")]
    public Camera mainCamera;
    public CameraFollow playerCameraFollow;
    public FloorVisibilityManager floorManager;
    public PointAndClickMovement playerMovement; // <<< ADD THIS REFERENCE
    public GameObject npcNameTagPrefab;
    public Transform nameTagParent;
    public DialogueManager dialogueManagerInstance; // Optional: Assign for direct access, or use DialogueManager.Instance

    [Header("Floor Map Configurations")]
    public List<MapFloorConfig> floorConfigs;

    [Header("Tag Settings")]
    public float tagNameYOffset = 2.5f;

    private bool isMapActive = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool originalCameraProjectionIsOrthographic;
    private float originalCameraOrthoSize;
    private float originalCameraFieldOfView;
    private int originalCameraCullingMask;

    private bool isCameraTransitioning = false; // <<< ADDED FLAG

    private Dictionary<BaseNPC, GameObject> activeNameTagObjects = new Dictionary<BaseNPC, GameObject>();

    [System.Serializable]
    public class MapFloorConfig
    {
        public FloorVisibilityManager.FloorLevel floorLevel;
        public Vector3 mapCameraPosition = new Vector3(0, 50, 0);
        public float orthographicSize = 10f;
        public Vector3 mapCameraRotationEuler = new Vector3(90, 0, 0);
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (playerCameraFollow == null && mainCamera != null) playerCameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (floorManager == null) floorManager = FloorVisibilityManager.Instance;
        if (playerMovement == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerMovement = playerObj.GetComponent<PointAndClickMovement>();
        }
        if (dialogueManagerInstance == null) dialogueManagerInstance = DialogueManager.Instance; // Find instance if not assigned

        bool criticalError = false;
        if (mainCamera == null) { Debug.LogError("[MapManager-Start] Main Camera is not assigned and Camera.main is null!"); criticalError = true; }
        if (playerCameraFollow == null) { Debug.LogError("[MapManager-Start] Player Camera Follow script not found on Main Camera!"); criticalError = true; }
        if (floorManager == null) { Debug.LogError("[MapManager-Start] FloorVisibilityManager instance not found!"); criticalError = true; }
        if (playerMovement == null) { Debug.LogError("[MapManager-Start] PointAndClickMovement instance not found on Player!"); criticalError = true; }
        if (npcNameTagPrefab == null) { Debug.LogError("[MapManager-Start] NPC Name Tag Prefab is not assigned!"); criticalError = true; }

        if (criticalError)
        {
            Debug.LogError("[MapManager-Start] Critical references missing. Disabling map functionality.");
            enabled = false;
            return;
        }

        if (nameTagParent == null)
        {
            GameObject parentGO = new GameObject("NPC_MapNameTags_Runtime");
            parentGO.transform.SetParent(this.transform);
            nameTagParent = parentGO.transform;
        }
        try { LayerMask.NameToLayer("MapUI"); }
        catch { Debug.LogError("[MapManager-Start] Layer 'MapUI' does not exist! Please create it. Disabling."); enabled = false; return; }

        _lastPolledPlayerFloorForMap = floorManager.CurrentVisibleFloor;
    }

    void Update()
    {
        if (!enabled) return;
        if (Input.GetKeyDown(mapToggleKey))
        {
            // --- CHECK DIALOGUE STATE ---
            if (isCameraTransitioning) // <<< CHECK THE FLAG
            {
                Debug.Log("[MapManager] Map key pressed, but camera is already transitioning. Ignoring.");
                return;
            }
            if (dialogueManagerInstance != null && dialogueManagerInstance.IsDialogueUIVisible)
            {
                Debug.Log("[MapManager] Map key pressed, but dialogue is active. Ignoring.");
                return; // Don't toggle map if dialogue is active
            }
            // ---------------------------
            ToggleMap();
        }

        if (isMapActive)
        {
            UpdateActiveNameTagPositions();
            PollNpcFloorChangesAndRefreshTags();
        }
    }

    public void ToggleMap()
    {
        if (isMapActive) DeactivateMap();
        else ActivateMap();
    }

    private void ActivateMap()
    {
        if (isMapActive || isCameraTransitioning) return; // Also check here just in case
        Debug.Log("[MapManager-ActivateMap] Activating Map...");
        isCameraTransitioning = true; // <<< SET FLAG AT START OF ACTIVATION PROCESS

        // --- Stop and Lock Player Movement ---
        if (playerMovement != null)
        {
            playerMovement.StopPlayerMovementAndNotify();
            playerMovement.HardLockPlayerMovement(); // Prevents new clicks
            Debug.Log("[MapManager-ActivateMap] Player movement stopped and input locked.");
        }
        else
        {
            Debug.LogWarning("[MapManager-ActivateMap] PlayerMovement reference is null. Cannot control player movement.");
        }
        // ------------------------------------

        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;
        originalCameraProjectionIsOrthographic = mainCamera.orthographic;
        originalCameraOrthoSize = mainCamera.orthographicSize;
        originalCameraFieldOfView = mainCamera.fieldOfView;
        originalCameraCullingMask = mainCamera.cullingMask;

        playerCameraFollow.SetManualControl(true);

        FloorVisibilityManager.FloorLevel currentFloor = floorManager.CurrentVisibleFloor;
        _lastPolledPlayerFloorForMap = currentFloor;
        _lastPolledNpcFloorsForMap.Clear();

        MapFloorConfig config = GetConfigForFloor(currentFloor);
        if (config == null)
        {
            Debug.LogError($"[MapManager-ActivateMap] No map configuration for floor: {currentFloor}. Aborting.");
            playerCameraFollow.SetManualControl(false);
            if (playerMovement != null) playerMovement.HardUnlockPlayerMovement(); // Unlock if we abort
            isCameraTransitioning = false; // <<< CLEAR FLAG IF ABORTING
            return;
        }

        StartCoroutine(TransitionCameraCoroutine(config.mapCameraPosition, Quaternion.Euler(config.mapCameraRotationEuler), true, config.orthographicSize, originalCameraFieldOfView, () =>
        {
            mainCamera.cullingMask |= (1 << LayerMask.NameToLayer("MapUI"));
            RefreshNpcNameTags(currentFloor);
            isCameraTransitioning = false; // <<< CLEAR FLAG ON COMPLETION
            Debug.Log("[MapManager-ActivateMap] Camera transition complete. isCameraTransitioning = false.");
        }));

        isMapActive = true;
    }

    private void DeactivateMap()
    {
        if (!isMapActive || isCameraTransitioning) return; // Also check here
        Debug.Log("[MapManager-DeactivateMap] Deactivating Map...");
        isCameraTransitioning = true; // <<< SET FLAG AT START OF DEACTIVATION PROCESS

        ClearAllNpcNameTags();

        StartCoroutine(TransitionCameraCoroutine(originalCameraPosition, originalCameraRotation, originalCameraProjectionIsOrthographic, originalCameraOrthoSize, originalCameraFieldOfView, () =>
        {
            playerCameraFollow.SetManualControl(false);
            mainCamera.cullingMask = originalCameraCullingMask;

            // --- Unlock Player Movement ---
            if (playerMovement != null)
            {
                playerMovement.HardUnlockPlayerMovement(); // Re-enables clicks
                Debug.Log("[MapManager-DeactivateMap] Player movement input unlocked.");
            }
            // -----------------------------
            isCameraTransitioning = false; // <<< CLEAR FLAG ON COMPLETION
            Debug.Log("[MapManager-DeactivateMap] Camera transition complete. isCameraTransitioning = false.");
        }));

        isMapActive = false;
    }

    // ... (GetConfigForFloor, TransitionCameraCoroutine, RefreshNpcNameTags, SetLayerRecursively, UpdateActiveNameTagPositions, positionNameTag, ClearAllNpcNameTags, PollNpcFloorChangesAndRefreshTags, LayerMaskToString remain the same as the previous "smooth transition" version)
    // Make sure to copy the LATEST version of TransitionCameraCoroutine from the previous response.
    private string LayerMaskToString(LayerMask mask)
    {
        string s = "";
        for (int i = 0; i < 32; i++) { if ((mask.value & (1 << i)) != 0) { s += LayerMask.LayerToName(i) + " | "; } }
        return s == "" ? "Nothing" : s;
    }
    private MapFloorConfig GetConfigForFloor(FloorVisibilityManager.FloorLevel floorLevel)
    {
        return floorConfigs.Find(config => config.floorLevel == floorLevel);
    }
    private IEnumerator TransitionCameraCoroutine(
    Vector3 targetPosition, Quaternion targetRotation,
    bool toOrthographic, float targetOrthoSizeConfig, float targetFovConfig, // Renamed for clarity
    System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        Vector3 startingPosition = mainCamera.transform.position; // Actual current position
        Quaternion startingRotation = mainCamera.transform.rotation; // Actual current rotation

        // --- CAPTURE ACTUAL CURRENT CAMERA STATE FOR LERP START ---
        float actualCurrentOrthoSize = mainCamera.orthographicSize;
        float actualCurrentFov = mainCamera.fieldOfView;
        bool cameraIsCurrentlyOrthographic = mainCamera.orthographic;

        // The 'targetOrthoSizeConfig' is the desired size IF we are going to Ortho.
        // The 'targetFovConfig' is the desired FOV IF we are going to Perspective.

        if (toOrthographic && !cameraIsCurrentlyOrthographic)
        {
            // Switching Perspective -> Orthographic
            mainCamera.orthographic = true;
        }
        // If going Orthographic -> Perspective, mainCamera.orthographic = false will be set at the end.

        while (elapsedTime < cameraTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / cameraTransitionDuration);
            t = t * t * (3f - 2f * t); // SmoothStep easing

            mainCamera.transform.position = Vector3.Lerp(startingPosition, targetPosition, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startingRotation, targetRotation, t);

            if (mainCamera.orthographic) // If currently rendering orthographically (either started ortho or switched to it)
            {
                // Lerp from its actual current ortho size to the target ortho size for the map
                mainCamera.orthographicSize = Mathf.Lerp(actualCurrentOrthoSize, targetOrthoSizeConfig, t);
            }
            else // If currently rendering perspectively
            {
                // Lerp from its actual current FOV to the target FOV for the player view
                mainCamera.fieldOfView = Mathf.Lerp(actualCurrentFov, targetFovConfig, t);
            }
            yield return null;
        }

        // --- Final state setting ---
        mainCamera.transform.position = targetPosition;
        mainCamera.transform.rotation = targetRotation;

        if (toOrthographic)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = targetOrthoSizeConfig;
        }
        else // Transitioning to perspective
        {
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = targetFovConfig;
        }

        onComplete?.Invoke();
    }

    private void RefreshNpcNameTags(FloorVisibilityManager.FloorLevel currentMapFloor)
    {
        ClearAllNpcNameTags(); // Clears activeNameTagObjects dictionary and Destroys previous GameObjects

        // Debug.Log($"[MapManager-Refresh] Refreshing for floor: {currentMapFloor}. Cam Culling: MapUI visible = {(mainCamera.cullingMask & (1 << LayerMask.NameToLayer("MapUI"))) != 0}");
        // Debug.Log($"[MapManager-Refresh] Target Parent for tags: {nameTagParent?.name ?? "NULL"}, is active: {nameTagParent?.gameObject.activeInHierarchy ?? false}");
        // Debug.Log($"[MapManager-Refresh] NPC Name Tag Prefab to instantiate: {npcNameTagPrefab?.name ?? "NULL"}");

        if (npcNameTagPrefab == null) { Debug.LogError("[MapManager-Refresh] npcNameTagPrefab is NULL. Cannot create tags."); return; }
        if (nameTagParent == null || !nameTagParent.gameObject.activeInHierarchy) { Debug.LogError("[MapManager-Refresh] nameTagParent is NULL or INACTIVE. Tags won't appear correctly."); return; }

        List<BaseNPC> allNpcs = BaseNPC.AllActiveNpcs;
        if (allNpcs == null) { Debug.LogError("[MapManager-Refresh] Cannot get NPC list."); return; }

        int tagsCreatedThisRefresh = 0;
        foreach (BaseNPC npc in allNpcs)
        {
            if (npc.currentNpcFloorLevel == currentMapFloor)
            {
                // Debug.Log($"[MapManager-Refresh] NPC: {npc.npcName} is on target floor {currentMapFloor}. Attempting to create tag...");

                GameObject tagInstance = Instantiate(npcNameTagPrefab, nameTagParent.transform); // Explicitly use .transform

                if (tagInstance == null)
                {
                    Debug.LogError($"[MapManager-Refresh] INSTANTIATE FAILED for {npc.npcName}! Prefab: {npcNameTagPrefab.name}, Parent: {nameTagParent.name}. THIS IS A MAJOR ISSUE.");
                    continue;
                }
                tagInstance.name = $"MapTag_{npc.npcName}";
                // Debug.Log($"[MapManager-Refresh] >>>> INSTANTIATED '{tagInstance.name}'. Parent: '{tagInstance.transform.parent?.name ?? "NULL"}'. ActiveInHierarchy: {tagInstance.activeInHierarchy}. WorldPos: {tagInstance.transform.position}, Scale: {tagInstance.transform.localScale}");

                Canvas canvas = tagInstance.GetComponent<Canvas>(); // Prefab root is the Canvas
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.WorldSpace) canvas.worldCamera = mainCamera;
                    else Debug.LogWarning($"[MapManager-Refresh] Tag '{tagInstance.name}' Canvas not WorldSpace.");
                }
                else Debug.LogError($"[MapManager-Refresh] Tag '{tagInstance.name}' PREFAB MISCONFIGURATION: Root GameObject is NOT a Canvas, or GetComponent<Canvas> failed.");


                TextMeshProUGUI tmpText = tagInstance.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null)
                {
                    tmpText.text = npc.npcName;
                    if (string.IsNullOrEmpty(npc.npcName)) Debug.LogWarning($"[MapManager-Refresh] NPC {npc.gameObject.name} has empty npcName.");

                    positionNameTag(tagInstance.transform, npc.transform);
                    SetLayerRecursively(tagInstance, LayerMask.NameToLayer("MapUI"));

                    activeNameTagObjects[npc] = tagInstance;
                    tagsCreatedThisRefresh++;
                }
                else
                {
                    Debug.LogWarning($"[MapManager-Refresh] Tag '{tagInstance.name}' PREFAB MISCONFIGURATION: Missing TextMeshProUGUI in children. Destroying instance.");
                    Destroy(tagInstance);
                }
            }
        }
        // Debug.Log($"[MapManager-Refresh] Finished. Created {tagsCreatedThisRefresh} tags for {currentMapFloor}. Dict count: {activeNameTagObjects.Count}");
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void UpdateActiveNameTagPositions()
    {
        List<BaseNPC> toRemove = new List<BaseNPC>();
        foreach (var pair in activeNameTagObjects)
        {
            BaseNPC npc = pair.Key;
            GameObject tagObject = pair.Value;

            if (npc == null || npc.gameObject == null || !npc.gameObject.activeInHierarchy ||
                tagObject == null || !tagObject.activeInHierarchy)
            {
                if (tagObject != null) Destroy(tagObject);
                toRemove.Add(npc);
                continue;
            }
            positionNameTag(tagObject.transform, npc.transform);
        }
        foreach (var npc in toRemove) activeNameTagObjects.Remove(npc);
    }

    private void positionNameTag(Transform tagTransform, Transform npcTransform)
    {
        tagTransform.position = npcTransform.position + Vector3.up * tagNameYOffset;
        tagTransform.rotation = Quaternion.Euler(90f, 0f, 180f);
    }

    private void ClearAllNpcNameTags()
    {
        foreach (var pair in activeNameTagObjects)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }
        activeNameTagObjects.Clear();
    }
    private FloorVisibilityManager.FloorLevel _lastPolledPlayerFloorForMap;
    private Dictionary<BaseNPC, FloorVisibilityManager.FloorLevel> _lastPolledNpcFloorsForMap = new Dictionary<BaseNPC, FloorVisibilityManager.FloorLevel>();

    private void PollNpcFloorChangesAndRefreshTags()
    {
        if (!isMapActive || floorManager == null || isCameraTransitioning) return; // <<< ADD isCameraTransitioning CHECK HERE

        bool refreshTagsNeeded = false;
        FloorVisibilityManager.FloorLevel currentPlayersActualFloor = floorManager.CurrentVisibleFloor;

        if (currentPlayersActualFloor != _lastPolledPlayerFloorForMap)
        {
            //Debug.Log($"[MapManager-Poll] Player floor changed (was {_lastPolledPlayerFloorForMap}, now {currentPlayersActualFloor}). Updating map view & tags.");
            _lastPolledPlayerFloorForMap = currentPlayersActualFloor;

            MapFloorConfig newConfig = GetConfigForFloor(currentPlayersActualFloor);
            if (newConfig != null)
            {
                isCameraTransitioning = true; // <<< SET FLAG
                StartCoroutine(TransitionCameraCoroutine(newConfig.mapCameraPosition, Quaternion.Euler(newConfig.mapCameraRotationEuler), true, newConfig.orthographicSize, originalCameraFieldOfView, () => // Pass original FOV for consistency
                {
                    RefreshNpcNameTags(currentPlayersActualFloor);
                    isCameraTransitioning = false; // <<< CLEAR FLAG
                    Debug.Log("[MapManager-Poll] Floor-change camera transition complete. isCameraTransitioning = false.");
                }));
            }
            else
            {
                RefreshNpcNameTags(currentPlayersActualFloor);
            }
            _lastPolledNpcFloorsForMap.Clear();
            return;
        }

        List<BaseNPC> allNpcs = BaseNPC.AllActiveNpcs;
        if (allNpcs == null) return;

        if (_lastPolledNpcFloorsForMap.Count != allNpcs.Count)
        {
            refreshTagsNeeded = true;
        }

        Dictionary<BaseNPC, FloorVisibilityManager.FloorLevel> currentFrameNpcFloors = new Dictionary<BaseNPC, FloorVisibilityManager.FloorLevel>();
        foreach (BaseNPC npc in allNpcs)
        {
            currentFrameNpcFloors[npc] = npc.currentNpcFloorLevel;

            if (!_lastPolledNpcFloorsForMap.ContainsKey(npc))
            {
                if (npc.currentNpcFloorLevel == _lastPolledPlayerFloorForMap)
                {
                    refreshTagsNeeded = true;
                }
            }
            else
            {
                if (_lastPolledNpcFloorsForMap[npc] != npc.currentNpcFloorLevel)
                {
                    if (_lastPolledNpcFloorsForMap[npc] == _lastPolledPlayerFloorForMap || npc.currentNpcFloorLevel == _lastPolledPlayerFloorForMap)
                    {
                        refreshTagsNeeded = true;
                    }
                }
            }
        }
        _lastPolledNpcFloorsForMap = currentFrameNpcFloors;

        if (refreshTagsNeeded)
        {
            RefreshNpcNameTags(_lastPolledPlayerFloorForMap);
        }
    }
}