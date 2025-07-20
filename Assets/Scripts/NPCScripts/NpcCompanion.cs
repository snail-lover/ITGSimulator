// NpcCompanion.cs

using UnityEngine;
using System.Collections;

/// <summary>
/// This class manages all NPC behaviors when in "Hangout Mode" with the player.
/// Its responsibilities include following the player, engaging in cooperative actions,
/// and mirroring the player's state. It is only active when the NpcController's
/// super-state is set to InHangout.
/// </summary>
public class NpcCompanion : MonoBehaviour
{
    // --- COMPONENT REFERENCES (Set by NpcController) ---
    private NpcController _controller;
    private NpcConfig _config;
    private NpcRuntimeData _runtimeData;
    private NpcMovement _movement;
    private NpcPerception _perception;
    private Transform _playerTransform;
    public NpcMovement Movement { get; private set; }

    // --- COMPANION STATE ---
    private bool _isActive = false;

    [Header("Companion Tuning")]
    [Tooltip("The ideal distance to maintain from the player when following.")]
    [SerializeField] private float followDistance = 2.5f;

    [Tooltip("The distance at which the NPC will stop trying to catch up and just teleport.")]
    [SerializeField] private float leashDistance = 20f;


    /// <summary>
    /// Called by NpcController during its Awake phase to provide necessary references.
    /// </summary>
    public void Initialize(NpcController controller)
    {
        _controller = controller;
        _config = controller.npcConfig;
        _runtimeData = controller.runtimeData;
        _playerTransform = controller.PlayerTransform;
        _movement = controller.Movement;

        _perception = controller.Perception;

        if (_config == null || _runtimeData == null || _playerTransform == null || _perception == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcCompanion initialization failed. Critical components are missing.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// Called by NpcController to "turn on" the companion behavior.
    /// This is the entry point for starting a hangout.
    /// </summary>
    public void Activate()
    {
        if (!this.enabled) return;

        Debug.Log($"[{_config.npcName}] Companion mode ACTIVATED. Starting hangout behavior.");
        _isActive = true;

        // Future logic:
        // - Start a coroutine for the main follow behavior.
        // - Subscribe to player action events.
    }

    /// <summary>
    /// Called by NpcController to "turn off" the companion behavior.
    /// This is the exit point for ending a hangout.
    /// </summary>
    public void Deactivate()
    {
        Debug.Log($"[{_config.npcName}] Companion mode DEACTIVATED. Ending hangout behavior.");
        _isActive = false;
        if (_movement != null)
        {
            _movement.Stop();
        }


        // Future logic:
        // - Stop all companion-related coroutines.
        // - Tell the NpcMovement component to stop.
        // - Unsubscribe from player action events.

        // For now, ensure movement is stopped when deactivating.
        // if (_movement != null)
        // {
        //     _movement.Stop();
        // }
    }


    /// <summary>
    /// The Update loop for companion mode. It will handle the continuous logic
    /// of following the player and checking for interactions.
    /// </summary>
    private void Update()
    {
        // This entire loop only runs if the companion mode is active.
        if (!_isActive)
        {
            return;
        }

        // --- CORE HANGOUT LOGIC WILL GO HERE ---
        // 1. Check distance to player.
        // 2. If too far, command NpcMovement to move closer.
        // 3. If close enough, command NpcMovement to stop and idle.
        // 4. Look at the player.
        // 5. Scan for cooperative objects nearby.
    }


    #region Public API for Cooperative Actions
    // These methods will be called by other game systems (like interactable objects)
    // to trigger cooperative behavior from the NPC.

    /// <summary>
    /// Called by an interactable object when the player initiates a cooperative action.
    /// </summary>
    /// <param name="coopObject">The object needing cooperation.</param>
    public void OnCooperativeActionInitiated(GameObject coopObject)
    {
        if (!_isActive) return;

        Debug.Log($"[{_config.npcName}] received a request to help with '{coopObject.name}'.");

        // Future logic:
        // - Find the designated "helper spot" on the coopObject.
        // - Command NpcMovement to move to that spot.
        // - Once arrived, play a "ready" animation and notify the coopObject.
    }

    #endregion
}