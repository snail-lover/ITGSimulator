// NpcInteraction.cs

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages all communicative and direct player interactions for the NPC.
/// This includes initiating dialogue, handling barks, and reacting to player visibility.
/// It acts as the "public relations" department for the NPC.
/// </summary>
public class NpcInteraction : MonoBehaviour
{
    // --- COMPONENT REFERENCES (Set by NpcController) ---
    private NpcController _controller;
    private NpcConfig _config;
    private NpcBrain _brain;

    // --- DIALOGUE DATA (Moved from NpcController) ---
    private DialogueData _dialogueData;

    [Header("Bark System")]
    [Tooltip("The UI Prefab to instantiate for displaying barks.")]
    [SerializeField] private GameObject barkUiPrefab;
    [Tooltip("The vertical offset for positioning the bark UI above the NPC's head.")]
    [SerializeField] private float barkYOffset = 2.5f;
    [Tooltip("How often, in seconds, the NPC should try to say a generic bark when idle.")]
    [SerializeField] private float genericBarkInterval = 10f;
    [Tooltip("How long, in seconds, the NPC will wait before greeting the player again.")]
    [SerializeField] private float greetingCooldown = 15f;

    // --- INTERNAL STATE ---
    private Coroutine _barkingCoroutine;
    private float _lastGreetingTime = -100f;
    private bool _isPlayerVisible = false;

    /// <summary>
    /// Called by NpcController during its Awake phase to provide necessary references.
    /// </summary>
    public void Initialize(NpcController controller)
    {
        _controller = controller;
        _config = controller.npcConfig;
        _brain = GetComponent<NpcBrain>(); // Assumes Brain is on the same GameObject

        LoadDialogueData();

        if (_config == null || _dialogueData == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcInteraction initialization failed.", this);
            this.enabled = false;
        }
    }

    // This component can start its own coroutines.
    private void Start()
    {
        _barkingCoroutine = StartCoroutine(BarkingCoroutine());
    }

    #region Public API (Interaction Entry Points)

    /// <summary>
    /// This is the new, primary entry point for starting a conversation with this NPC.
    /// Called by Action_StartDialogue or any other system.
    /// </summary>
    public void InitiateDialogue(string startNodeID = null, string uniqueTriggerID = null)
    {
        if (_controller == null) return;

        // Tell the controller to switch to the "InScene" state.
        _controller.SetSuperState(NpcController.NpcSuperState.InScene);

        // Pass the call to the central DialogueManager.
        DialogueManager.Instance.StartDialogue(_controller, startNodeID, uniqueTriggerID);
    }

    /// <summary>
    /// Called by NpcPerception when the player's visibility status changes.
    /// This is the central point for triggering reactions to seeing the player.
    /// </summary>
    public void UpdatePlayerVisibility(bool isVisible)
    {
        if (_isPlayerVisible == isVisible) return;
        _isPlayerVisible = isVisible;

        if (_isPlayerVisible)
        {
            // Only greet if not already in a scene and cooldown has passed.
            if (_controller.CurrentSuperState != NpcController.NpcSuperState.InScene &&
                Time.time > _lastGreetingTime + greetingCooldown)
            {
                AttemptGreetingBark();
            }
        }
    }

    public DialogueData GetDialogueData() => _dialogueData;

    #endregion


    #region Bark System (Moved from NpcController)

    private IEnumerator BarkingCoroutine()
    {
        yield return new WaitForSeconds(Random.Range(genericBarkInterval * 0.5f, genericBarkInterval * 1.5f));

        while (true)
        {
            // Bark only if the NPC is in its autonomous, idle state.
            if (_controller.CurrentSuperState == NpcController.NpcSuperState.Autonomous &&
                _brain != null && _brain.CurrentState == NpcBrain.AutonomousState.Idle)
            {
                AttemptGenericBark();
            }
            yield return new WaitForSeconds(Random.Range(genericBarkInterval * 0.8f, genericBarkInterval * 1.2f));
        }
    }

    private void AttemptGenericBark()
    {
        if (barkUiPrefab == null || BarkManager.Instance == null) return;
        Bark bark = BarkManager.Instance.GetRandomBarkByType("generic");
        if (bark != null) DisplayBark(bark.text);
    }

    private void AttemptGreetingBark()
    {
        if (barkUiPrefab == null || BarkManager.Instance == null) return;
        Bark bark = BarkManager.Instance.GetRandomBarkByType("greeting");
        if (bark != null)
        {
            DisplayBark(bark.text);
            _lastGreetingTime = Time.time;
        }
    }

    public void DisplayBark(string text)
    {
        if (barkUiPrefab == null) return;
        Vector3 spawnPosition = transform.position + Vector3.up * barkYOffset;
        GameObject barkInstance = Instantiate(barkUiPrefab, spawnPosition, Quaternion.identity, transform);

        TextMeshProUGUI barkText = barkInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (barkText != null) barkText.text = text;

        Destroy(barkInstance, 4f);
    }

    #endregion


    #region Data Loading (Moved from NpcController)

    private void LoadDialogueData()
    {
        if (_config.dialogueFile == null)
        {
            Debug.LogError($"[{gameObject.name}] Dialogue File not assigned in npcConfig!");
            return;
        }
        try
        {
            _dialogueData = JsonUtility.FromJson<DialogueData>(_config.dialogueFile.text);
            if (_dialogueData == null) throw new System.ArgumentNullException("Parsed dialogue data is null.");

            // Simplified validation/dictionary creation
            _dialogueData.nodeDictionary = new Dictionary<string, DialogueNode>();
            foreach (var node in _dialogueData.nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.nodeID))
                {
                    _dialogueData.nodeDictionary[node.nodeID] = node;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error parsing Dialogue JSON: {e.Message}");
            _dialogueData = null;
        }
    }
    #endregion
}