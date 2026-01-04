using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Game.Core;
using Game.Gameplay;

namespace Game.Gameplay
{
    public class InteractionController : MonoBehaviour
    {
        public static InteractionController Instance { get; private set; }

        [Header("Settings")]
        public float interactionRange = 3f;

        [Header("Line of Sight")]
        [Tooltip("Layers that block interactions (Walls, Doors, etc). Usually 'Default'.")]
        public LayerMask occlusionLayers;
        [Tooltip("How high up to shoot the ray from (to avoid hitting the floor).")]
        public float lineOfSightHeight = 1.0f;

        [Header("Debug")]
        public bool showDebugGizmos = true;

        // References
        private Transform _playerTransform;
        private IInteractable _activeContextTarget;

        public static event Action<List<InteractionOption>, Vector2> OnOpenContextMenu;
        public static event Action OnClearContextMenu;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            var playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                _playerTransform = playerInput.transform;
            }
            else
            {
                Debug.LogError("[InteractionController] Could not find PlayerInput!");
            }
        }

        void OnEnable()
        {
            PlayerInput.OnInteractCommand += OnLeftClickInteract;
            PlayerInput.OnUseItemCommand += OnItemUsed;
        }

        void OnDisable()
        {
            PlayerInput.OnInteractCommand -= OnLeftClickInteract;
            PlayerInput.OnUseItemCommand -= OnItemUsed;
        }

        void Update()
        {
            // Monitor the active target (if any)
            if (_activeContextTarget != null)
            {
                // Re-use your existing validation logic!
                if (!IsValidInteraction(_activeContextTarget))
                {
                    // It is no longer valid (walked away, wall in between, etc.)
                    _activeContextTarget = null;

                    // Tell the UI to close
                    OnClearContextMenu?.Invoke();
                }
            }
        }


        private void OnLeftClickInteract(IInteractable target) => HandleInteraction(target, null, false);
        private void OnItemUsed(IInteractable target, string itemID) => HandleInteraction(target, itemID, false);
        public void OnRightClickInteract(IInteractable target) => HandleInteraction(target, null, true);

        private void HandleInteraction(IInteractable target, string itemID, bool isRightClick)
        {
            if (target == null || _playerTransform == null) return;

            // 1. Check if Valid (Distance + Wall Check)
            if (!IsValidInteraction(target))
            {
                // IsValidInteraction handles the Debug.Logs for us
                return;
            }

            // 2. Build Context
            InteractionContext context = new InteractionContext
            {
                Source = _playerTransform.gameObject,
                HeldItemID = itemID ?? SelectedItemState.SelectedItemID
            };

            // 3. Get Options
            List<InteractionOption> options = target.GetInteractions(context);

            if (options == null || options.Count == 0)
            {
                Debug.Log($"<color=gray>[Interaction]</color> No interactions available.");
                return;
            }

            // 4. Execute
            if (isRightClick)
            {
                if (options.Count > 0)
                {
                    options = options.OrderByDescending(x => x.Priority).ToList();

                    _activeContextTarget = target;
                    OnOpenContextMenu?.Invoke(options, Input.mousePosition);
                }
            }
            else
            {
                var bestOption = options.OrderByDescending(x => x.Priority).FirstOrDefault();
                if (bestOption != null)
                {
                    Debug.Log($"<color=green>[Action]</color> Executing: {bestOption.Label}");
                    bestOption.ActionToRun?.Invoke();
                    _activeContextTarget = null; OnClearContextMenu?.Invoke();
                }
            }
        }
        public void ClearTracking()
        {
            _activeContextTarget = null;
        }

        // --- VALIDATION LOGIC ---

        public bool IsValidInteraction(IInteractable target)
        {
            if (_playerTransform == null) return false;

            MonoBehaviour targetMb = target as MonoBehaviour;
            if (targetMb == null) return true; // Non-physical targets are always valid

            // A. Distance Check
            float dist = Vector3.Distance(_playerTransform.position, targetMb.transform.position);
            if (dist > interactionRange)
            {
                Debug.Log($"<color=yellow>[Interaction]</color> Too far! ({dist:F1}m)");
                return false;
            }

            // B. Line of Sight (Wall) Check
            if (!HasLineOfSight(targetMb))
            {
                Debug.Log($"<color=red>[Interaction]</color> Something is blocking the view!");
                return false;
            }

            return true;
        }

        private bool HasLineOfSight(MonoBehaviour targetMb)
        {
            // Calculate start and end points raised by the height offset
            // This prevents the ray from skimming the ground and failing
            Vector3 start = _playerTransform.position + Vector3.up * lineOfSightHeight;
            Vector3 end = targetMb.transform.position + Vector3.up * lineOfSightHeight;

            // If the target has a collider, try to aim for its center instead of its pivot
            // This helps if the pivot is at the feet but the object is tall (like a fridge)
            Collider targetCol = targetMb.GetComponent<Collider>();
            if (targetCol != null) end = targetCol.bounds.center;

            Vector3 direction = end - start;
            float distance = direction.magnitude;

            // Cast a Ray
            if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance, occlusionLayers))
            {
                // We hit something!

                // Check if we hit the target itself (or a child of it). If so, that's fine.
                if (hit.transform == targetMb.transform || hit.transform.IsChildOf(targetMb.transform))
                {
                    return true;
                }

                // We hit something else (a wall)
                return false;
            }

            // Raycast hit nothing, meaning clear path
            return true;
        }

        // --- VISUAL DEBUGGING ---
        void OnDrawGizmos()
        {
            if (!showDebugGizmos || _playerTransform == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_playerTransform.position, interactionRange);
        }
    }
}