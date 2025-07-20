// NpcController.cs (Corrected)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The NpcController is the central COORDINATOR for an NPC. It is the main entry point
/// and holds references to all functional components (Brain, Movement, Interaction, etc.).
/// Its primary job is to manage the NPC's overall "Super State" to dictate which
/// behavior component is currently active.
/// </summary>
[RequireComponent(typeof(NpcBrain))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcInteraction))]
[RequireComponent(typeof(NpcCompanion))]
public class NpcController : MonoBehaviour
{
    // --- SUPER STATE ---
    public enum NpcSuperState
    {
        None,
        Autonomous,
        InHangout,
        InScene
    }

    [Header("Super State (Read-Only)")]
    [SerializeField] private NpcSuperState _superState = NpcSuperState.None;
    public NpcSuperState CurrentSuperState => _superState;

    [Header("NPC Data")]
    public NpcConfig npcConfig;
    public NpcRuntimeData runtimeData { get; private set; }

    // --- CORE COMPONENTS ---
    // The Controller holds direct references to its "organs".
    // REMOVED redundant private fields _brain, _movement, and _interaction
    private NpcCompanion _companion;

    // --- PUBLIC ACCESSORS (For other components and systems to use) ---
    public NavMeshAgent Agent { get; private set; }
    public Animator NpcAnimator { get; private set; }
    public NpcPerception Perception { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public NpcMovement Movement { get; private set; }
    public NpcInteraction Interaction { get; private set; }
    public NpcBrain Brain { get; private set; }

    // --- SCENE MANAGEMENT ---
    private static List<NpcController> _allActiveNpcs = new List<NpcController>();
    public static List<NpcController> AllActiveNpcs => _allActiveNpcs;

    [Header("Floor Management")]
    public FloorVisibilityManager.FloorLevel currentNpcFloorLevel = FloorVisibilityManager.FloorLevel.Lower;
    public Collider interactionCollider;
    private Renderer[] _npcRenderers;


    // =========================================================================
    // 1. Unity Lifecycle & Initialization
    // =========================================================================

    protected virtual void Awake()
    {
        if (npcConfig == null) { Debug.LogError($"NPC '{gameObject.name}' is missing its NpcConfig!", this); this.enabled = false; return; }

        runtimeData = WorldDataManager.Instance.GetOrCreateNpcData(npcConfig);
        CachePlayerReferences();

        Agent = GetComponent<NavMeshAgent>();
        NpcAnimator = GetComponent<Animator>();
        Perception = GetComponentInChildren<NpcPerception>();
        Brain = GetComponent<NpcBrain>();
        Movement = GetComponent<NpcMovement>();
        Interaction = GetComponent<NpcInteraction>();
        _companion = GetComponent<NpcCompanion>();

        Brain.Initialize(this);
        Movement.Initialize(this);
        Interaction.Initialize(this);
        _companion.Initialize(this);

        _npcRenderers = GetComponentsInChildren<Renderer>(true);
        SetupNpcCollisionProperties();
        _allActiveNpcs.Add(this);
        if (!AllActiveNpcs.Contains(this)) ApplyNpcNpcCollisionIgnores();
    }

    private void Start()
    {
        if (runtimeData.lastKnownPosition != Vector3.zero)
        {
            Agent.Warp(runtimeData.lastKnownPosition);
        }
        SetSuperState(NpcSuperState.Autonomous);
    }

    private void OnEnable()
    {
        if (WorldDataManager.Instance != null) WorldDataManager.Instance.OnBeforeSave += CaptureStateBeforeSave;
    }

    private void OnDisable()
    {
        if (WorldDataManager.Instance != null) WorldDataManager.Instance.OnBeforeSave -= CaptureStateBeforeSave;
    }

    protected virtual void OnDestroy()
    {
        _allActiveNpcs.Remove(this);
    }


    // =========================================================================
    // 2. Super State Management (The New Core Logic)
    // =========================================================================

    public void SetSuperState(NpcSuperState newState)
    {
        if (_superState == newState) return;

        // --- ON EXIT ---
        switch (_superState)
        {
            case NpcSuperState.Autonomous:
                // *** FIX: Use the public property 'Brain' which was correctly assigned. ***
                Brain?.DeactivateBrain();
                break;
            case NpcSuperState.InHangout:
                _companion?.Deactivate();
                break;
            case NpcSuperState.InScene:
                // *** FIX: Use the public property 'Movement' which was correctly assigned. ***
                Movement?.Stop();
                break;
        }

        _superState = newState;

        // --- ON ENTER ---
        switch (_superState)
        {
            case NpcSuperState.Autonomous:
                Brain?.ActivateBrain();
                break;
            case NpcSuperState.InHangout:
                _companion?.Activate();
                break;
            case NpcSuperState.InScene:
                break;
        }
    }


    // =========================================================================
    // 3. Public API (Methods called by other systems)
    // =========================================================================

    public void StartHangout()
    {
        Debug.Log($"[{npcConfig.npcName}] is now starting a hangout session with the player.");
        SetSuperState(NpcSuperState.InHangout);
    }

    public void EndHangout()
    {
        Debug.Log($"[{npcConfig.npcName}] is now ending the hangout session.");
        SetSuperState(NpcSuperState.Autonomous);
    }

    public void NpcDialogueEnded()
    {
        SetSuperState(NpcSuperState.Autonomous);
    }

    public virtual void PauseAIForCutscene(bool pause)
    {
        if (pause)
        {
            SetSuperState(NpcSuperState.InScene);
        }
        else
        {
            SetSuperState(NpcSuperState.Autonomous);
        }
    }


    // =========================================================================
    // 4. Utility & Data Management (Methods that stayed with the controller)
    // =========================================================================

    private void CachePlayerReferences()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) PlayerTransform = playerObj.transform;
    }

    private void CaptureStateBeforeSave()
    {
        if (runtimeData != null) runtimeData.lastKnownPosition = transform.position;
    }

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

    private void SetupNpcCollisionProperties()
    {
        if (Agent != null)
        {
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
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
}