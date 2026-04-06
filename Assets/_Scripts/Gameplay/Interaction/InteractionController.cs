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
        
        [Header("Global System")]
        public InteractionRegistry globalRegistry; 

        [Header("Settings")]
        public float interactionRange = 3f;

        [Header("Line of Sight")]
        public LayerMask occlusionLayers;
        public float lineOfSightHeight = 1.0f;

        [Header("Debug")]
        public bool showDebugGizmos = true;

        private Transform _playerTransform;
        private PlayerHoldController _playerHolder;
        private IInteractable _activeContextTarget;

        // Track the warning state so we don't spam the event
        private bool _isOutOfRange = false;

        // Events
        public static event Action OnClearContextMenu;
        
        // NEW: Event sends TRUE if too far, FALSE if close enough
        public static event Action<bool> OnRangeWarningChanged; 

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (globalRegistry != null) globalRegistry.Initialize();
        }

        void Start()
        {
            var playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                _playerTransform = playerInput.transform;
                _playerHolder = playerInput.GetComponent<PlayerHoldController>();
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
            // Only run logic if we have an open context menu target
            if (_activeContextTarget != null)
            {
                MonoBehaviour targetMb = _activeContextTarget as MonoBehaviour;

                // 1. Target Destroyed check
                if (targetMb == null)
                {
                    ClearContext();
                    return;
                }

                // 2. Line of Sight Check (Hard Constraint - Close menu if lost)
                if (!HasLineOfSight(targetMb))
                {
                    ClearContext();
                    return;
                }

                // 3. Distance Check (Soft Constraint - Toggle Warning Text)
                float dist = Vector3.Distance(_playerTransform.position, targetMb.transform.position);
                
                bool currentlyOutOfRange = dist > interactionRange;

                // If the state changed (e.g., walked from Far -> Close), fire event
                if (currentlyOutOfRange != _isOutOfRange)
                {
                    _isOutOfRange = currentlyOutOfRange;
                    OnRangeWarningChanged?.Invoke(_isOutOfRange);
                }
            }
        }

        private void OnLeftClickInteract(IInteractable target) => HandleInteraction(target, null, false);
        private void OnItemUsed(IInteractable target, string itemID) => HandleInteraction(target, itemID, false);
        public void OnRightClickInteract(IInteractable target) => HandleInteraction(target, null, true);

        private void HandleInteraction(IInteractable target, string itemID, bool isRightClick)
        {
            if (target == null || _playerTransform == null) return;

            // --- CRITICAL CHANGE ---
            // For Right Click, we ONLY check Line of Sight, we ignore Distance.
            // For Left Click, we still enforce distance immediately.
            if (isRightClick)
            {
                if (!HasLineOfSight(target as MonoBehaviour)) return;
            }
            else
            {
                if (!IsValidInteraction(target)) return;
            }
            // -----------------------

            // Setup Context Data
            string contextItemID = itemID; 
            if (string.IsNullOrEmpty(contextItemID)) contextItemID = SelectedItemState.SelectedItemID;
            if (string.IsNullOrEmpty(contextItemID) && _playerHolder != null) contextItemID = _playerHolder.GetHeldItemID();

            ItemDefinition contextItemData = null;
            if (!string.IsNullOrEmpty(contextItemID)) contextItemData = ItemDatabase.Instance.GetItemByID(contextItemID);

            InteractionContext context = new InteractionContext
            {
                Source = _playerTransform.gameObject,
                HeldItem = contextItemData
            };

            List<InteractionOption> options = target.GetInteractions(context);
            if (options == null || options.Count == 0) return;

            if (isRightClick)
            {
                options = options.OrderByDescending(x => x.Priority).ToList();
                _activeContextTarget = target;
                
                // Force an initial check so the UI is correct the moment it opens
                float dist = Vector3.Distance(_playerTransform.position, (target as MonoBehaviour).transform.position);
                _isOutOfRange = dist > interactionRange;
                
                // Open Menu
                UIEvents.RequestContextMenu(options, Input.mousePosition, target.GetName());
                
                // Update Warning immediately
                OnRangeWarningChanged?.Invoke(_isOutOfRange);
            }
            else
            {
                // Left click logic remains the same
                var bestOption = options.OrderByDescending(x => x.Priority).FirstOrDefault();
                if (bestOption != null)
                {
                    bestOption.ActionToRun?.Invoke();
                    ClearContext();
                }
            }
        }

        private void ClearContext()
        {
            _activeContextTarget = null;
            OnClearContextMenu?.Invoke();
        }

        public void ClearTracking()
        {
            _activeContextTarget = null;
        }

        // Used for Left Clicks (Enforces Distance)
        public bool IsValidInteraction(IInteractable target)
        {
            if (_playerTransform == null) return false;
            MonoBehaviour targetMb = target as MonoBehaviour;
            if (targetMb == null) return true; 

            float dist = Vector3.Distance(_playerTransform.position, targetMb.transform.position);
            if (dist > interactionRange) return false;

            if (!HasLineOfSight(targetMb)) return false;

            return true;
        }

        private bool HasLineOfSight(MonoBehaviour targetMb)
        {
            // ... (Keep your existing Line of Sight logic exactly as is) ...
            Vector3 start = _playerTransform.position + Vector3.up * lineOfSightHeight;
            Vector3 end;
            Collider targetCol = targetMb.GetComponentInChildren<Collider>();

            if (targetCol != null)
            {
                end = targetCol.ClosestPoint(start);
                if (end == start) end = targetCol.bounds.center;
            }
            else
            {
                end = targetMb.transform.position + Vector3.up * lineOfSightHeight;
            }

            Vector3 direction = end - start;
            float targetDistance = direction.magnitude;

            if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, targetDistance, occlusionLayers))
            {
                if (hit.transform == targetMb.transform || hit.transform.IsChildOf(targetMb.transform)) return true;
                if (Mathf.Abs(hit.distance - targetDistance) < 0.05f) return true; 
                return false;
            }
            return true;
        }

        void OnDrawGizmos()
        {
            if (!showDebugGizmos || _playerTransform == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_playerTransform.position, interactionRange);
        }
    }
}