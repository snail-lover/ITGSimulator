using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using Game.Core;

/// <summary>
/// The NpcController is the central COORDINATOR for an NPC. It is the main entry point
/// and holds references to all functional components (Brain, Movement, Interaction, etc.).
/// Its primary job is to manage the NPC's overall "Super State" to dictate which
/// behavior component is currently active.
/// </summary>
[RequireComponent(typeof(NpcBrain))]
[RequireComponent(typeof(NpcMovement))]
[RequireComponent(typeof(NpcCompanion))]
[RequireComponent(typeof(BehaviorTree))] 
public class NpcController : MonoBehaviour, IHangoutPartner, IFloorVisibilityAware
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
    private NpcCompanion _companion;
    private BehaviorTree _behaviorTree; 

    // --- PUBLIC ACCESSORS (For other components and systems to use) ---
    public NavMeshAgent Agent { get; private set; }
    public Animator NpcAnimator { get; private set; }
    public NpcPerception Perception { get; private set; }
    public Transform PlayerTransform { get; private set; }
    public NpcMovement Movement { get; private set; }
    public NpcBrain Brain { get; private set; }

    // --- SCENE MANAGEMENT ---
    private static List<NpcController> _allActiveNpcs = new List<NpcController>();
    public static List<NpcController> AllActiveNpcs => _allActiveNpcs;

    [Header("Floor Management")]
    public FloorLevel currentNpcFloorLevel = FloorLevel.Lower;
    public Collider interactionCollider;
    private Renderer[] _npcRenderers;

    // =========================================================================
    // 1. Unity Lifecycle & Initialization
    // =========================================================================

    protected virtual void Awake()
    {
        if (npcConfig == null) { Debug.LogError($"NPC '{gameObject.name}' is missing its NpcConfig!", this); this.enabled = false; return; }

        runtimeData = WorldDataManager.Instance.GetOrCreateNpcData(npcConfig.name, () =>
        {
            // This code only runs if the WorldDataManager can't find existing data.
            // It uses the config to create new runtime data and returns it.
            return new NpcRuntimeData(
                npcConfig.name,
                npcConfig.initialLove,
                npcConfig.hungerDecay,
                npcConfig.energyDecay,
                npcConfig.bladderDecay,
                npcConfig.funDecay
            );
        });
        CachePlayerReferences();

        // --- Cache all components ---
        Agent = GetComponent<NavMeshAgent>();
        NpcAnimator = GetComponent<Animator>();
        Perception = GetComponentInChildren<NpcPerception>();
        Brain = GetComponent<NpcBrain>();
        Movement = GetComponent<NpcMovement>();
        _companion = GetComponent<NpcCompanion>();
        _behaviorTree = GetComponent<BehaviorTree>(); // --- NEW ---

        // --- Initialize all components ---
        Brain.Initialize(this);
        Movement.Initialize(this);
        _companion.Initialize(this);

        if (_behaviorTree != null && npcConfig.autonomousBehaviorTree != null)
        {
            _behaviorTree.ExternalBehavior = npcConfig.autonomousBehaviorTree;
        }

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
        // Start in Autonomous state by default
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
        Debug.Log($"[SetSuperState] Received request to change state from {_superState} to {newState}.", this);

        if (_superState == newState)
        {
            // --- DEBUG CHECK 2: Is it exiting early because the state is already set? ---
            Debug.LogWarning($"[SetSuperState] Aborting: New state ({newState}) is the same as the current state.", this);
            return;
        }

        // --- ON EXIT ---
        // First, disable the current behavior tree no matter what state we are leaving.
        if (_behaviorTree != null && _behaviorTree.enabled)
        {
            Debug.Log($"[SetSuperState] Disabling current behavior tree: {_behaviorTree.ExternalBehavior.name}");
            _behaviorTree.DisableBehavior(true); // Disable and reset the current tree
        }

        switch (_superState)
        {
            // case NpcSuperState.Autonomous:
            //     // The generic disable above handles this.
            //     break;
            case NpcSuperState.InHangout:
                _companion?.Deactivate(); // You can still keep this for other hangout logic
                break;
            case NpcSuperState.InScene:
                Movement?.Stop();
                break;
        }

        _superState = newState;

        // --- ON ENTER ---
        // Now, set the new tree and enable it.
        switch (_superState)
        {
            case NpcSuperState.Autonomous:
                if (_behaviorTree != null && npcConfig.autonomousBehaviorTree != null)
                {
                    if (Agent.isOnNavMesh) Agent.isStopped = false;
                    _behaviorTree.ExternalBehavior = npcConfig.autonomousBehaviorTree; // Set the tree
                    _behaviorTree.EnableBehavior(); // Start the tree
                }
                break;
            case NpcSuperState.InHangout:
                if (_behaviorTree != null && npcConfig.hangoutBehaviorTree != null)
                {
                    if (Agent.isOnNavMesh) Agent.isStopped = false;
                    Debug.Log($"[SetSuperState] SUCCESS: Applying hangout tree '{npcConfig.hangoutBehaviorTree.name}' and enabling behavior.", this);
                    _behaviorTree.ExternalBehavior = npcConfig.hangoutBehaviorTree;

                    // Don't forget to re-inject the brain reference!
                    var sharedBrain = (SharedNpcBrain)_behaviorTree.GetVariable("NpcBrain");
                    if (sharedBrain != null)
                    {
                        sharedBrain.Value = this.Brain;
                    }

                    _behaviorTree.EnableBehavior();
                }
                else
                {
                    // If this message appears, we've found the problem.
                    Debug.LogError($"[SetSuperState] FAILED TO START HANGOUT! " +
                                   $"_behaviorTree is {(_behaviorTree == null ? "NULL" : "assigned")}. " +
                                   $"npcConfig.hangoutBehaviorTree is {(npcConfig.hangoutBehaviorTree == null ? "NULL" : "assigned")}.", this);
                }
                break;
            case NpcSuperState.InScene:
                // No behavior tree runs during a cutscene.
                break;
        }
    }



    // =========================================================================
    // 3. Public API (Methods called by other systems)
    // =========================================================================

    // This method is called by the HangoutTrigger UI button
    public void RequestHangout()
    {
        // The NPC doesn't start the hangout itself. It ASKS the manager to start one WITH IT.
        HangoutManager.Instance.StartHangout(this);
    }

    // --- IHangoutPartner Implementation ---

    public void BeginHangoutState()
    {
        // The HangoutManager is telling us to start. Now we manage our own state.
        SetSuperState(NpcSuperState.InHangout);
    }

    public void EndHangoutState()
    {
        // The HangoutManager is telling us to stop.
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

    public void UpdateVisibilityBasedOnPlayerFloor(FloorLevel playerCurrentFloor)
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

    public void NotifyNpcChangedFloor(FloorLevel newNpcFloor) // Core enum
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
            FloorLevel playerFloor = FloorVisibilityManager.Instance.CurrentVisibleFloor;
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