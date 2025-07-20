// NpcMovement.cs

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// This class is the "body" of the NPC, responsible for all physical movement and related animations.
/// It receives commands from other components (like NpcBrain or NpcCompanion) and executes them.
/// It does not make any decisions on its own.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class NpcMovement : MonoBehaviour
{
    // --- COMPONENT REFERENCES ---
    private NpcConfig _config;
    private NavMeshAgent _agent;
    private Animator _animator;

    // --- STATE ---
    private Coroutine _turningCoroutine;

    /// <summary>
    /// A public property to let other components easily check if the NPC is currently moving.
    /// This is cleaner than exposing the raw NavMeshAgent.
    /// </summary>
    public bool IsMoving => _agent != null && _agent.hasPath && (_agent.pathPending || _agent.remainingDistance > _agent.stoppingDistance);


    /// <summary>
    /// Called by NpcController during its Awake phase to provide necessary references.
    /// </summary>
    public void Initialize(NpcController controller)
    {
        _config = controller.npcConfig;
        _agent = controller.Agent;
        _animator = controller.NpcAnimator;

        if (_config == null || _agent == null || _animator == null)
        {
            Debug.LogError($"[{gameObject.name}] NpcMovement initialization failed. Critical components are missing.", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// The main Update loop is responsible for translating agent velocity into animation.
    /// </summary>
    private void Update()
    {
        UpdateLocomotionAnimation();
    }


    #region Public Movement Commands (The API)
    // --- These are the "verbs" that other components will use to command the body ---

    /// <summary>
    /// Commands the NPC to move to a specific destination.
    /// </summary>
    /// <param name="destination">The world-space position to move to.</param>
    public void MoveTo(Vector3 destination)
    {
        if (!this.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    /// <summary>
    /// Commands the NPC to stop all movement immediately.
    /// </summary>
    public void Stop()
    {
        if (!this.enabled || !_agent.isOnNavMesh) return;

        if (_agent.hasPath)
        {
            _agent.ResetPath();
        }
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero; // Immediately zero out velocity for animation
    }

    /// <summary>
    /// Commands the NPC to smoothly turn to face a specific direction over time.
    /// </summary>
    /// <param name="direction">The direction to face.</param>
    /// <param name="duration">How long the turn should take.</param>
    public void FaceDirection(Vector3 direction, float duration = 1.0f)
    {
        if (!this.enabled) return;

        if (_turningCoroutine != null)
        {
            StopCoroutine(_turningCoroutine);
        }
        _turningCoroutine = StartCoroutine(ExecuteTurningCoroutine(direction, duration));
    }

    #endregion


    #region Internal Logic

    /// <summary>
    /// Handles the smooth rotation of the NPC's transform.
    /// </summary>
    private IEnumerator ExecuteTurningCoroutine(Vector3 direction, float duration)
    {
        // Ensure we only rotate on the horizontal plane
        direction.y = 0;
        if (direction.sqrMagnitude < 0.001f)
        {
            yield break; // Avoids LookRotation error if direction is zero
        }

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        float timer = 0f;
        while (timer < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Snap to final rotation to ensure accuracy
        transform.rotation = targetRotation;
        _turningCoroutine = null;
    }

    /// <summary>
    /// Translates the NavMeshAgent's current velocity into a smoothed "Speed" parameter for the Animator.
    /// This logic was moved directly from NpcController.
    /// </summary>
    private void UpdateLocomotionAnimation()
    {
        if (_animator == null || _agent == null || !_agent.isOnNavMesh || string.IsNullOrEmpty(_config.speedParameterName))
        {
            // If we are stopped, ensure the speed parameter is 0.
            if (_animator != null && !string.IsNullOrEmpty(_config.speedParameterName))
            {
                _animator.SetFloat(_config.speedParameterName, 0f);
            }
            return;
        }

        // 1. Get the agent's current speed and normalize it to a 0-1 range.
        float normalizedSpeed = _agent.velocity.magnitude / _agent.speed;

        // 2. Smooth the value over time to prevent jerky animation transitions.
        float currentSpeed = _animator.GetFloat(_config.speedParameterName);
        float smoothedSpeed = Mathf.Lerp(currentSpeed, normalizedSpeed, Time.deltaTime * 10f);

        // 3. Set the "Speed" float parameter in the animator.
        _animator.SetFloat(_config.speedParameterName, smoothedSpeed);
    }

    #endregion
}