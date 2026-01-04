using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInput : MonoBehaviour
{
    [Header("Setup")]
    public Camera mainCamera;
    public LayerMask clickableLayers;
    public PlayerMotor motor;

    // Core events other systems can subscribe to
    public static event System.Action<IInteractable> OnInteractCommand;
    public static event System.Action<IInteractable, string> OnUseItemCommand;

    private bool isInputLocked = false;

    void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
    }

    void Start()
    {
        if (CameraFollow.Instance != null)
            mainCamera = CameraFollow.Instance.GetComponent<Camera>();
        else
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (isInputLocked) return;

        HandleMovement();
        HandleMouseClick();
    }

    private void HandleMovement()
    {
        if (motor == null) return;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.sqrMagnitude >= 0.1f)
        {
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;
            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * input.z + camRight * input.x);
            motor.SetMoveDirection(moveDir);
        }
        else
        {
            motor.SetMoveDirection(Vector3.zero);
        }
    }

    private void HandleMouseClick()
    {
        // --- RIGHT CLICK (Context Menu) ---
        if (Input.GetMouseButtonDown(1))
        {
            // 1. First, check if we hit a valid interactable
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null)
                {
                    // We hit something! Open the menu.
                    // IMPORTANT: We do NOT clear the selected item here.
                    // The InteractionController will see the selected item and include "Use [Item]" options.
                    InteractionController.Instance.OnRightClickInteract(interactable);
                    return; // Stop here.
                }
            }

            // 2. If we missed everything (or hit a non-interactable wall), THEN clear selection.
            if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID))
            {
                SelectedItemState.Clear();
                Debug.Log("[PlayerInput] Right-click on empty space: Selection cleared.");
            }
            return;
        }

        // --- LEFT CLICK (Standard Interaction) ---
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            PerformRaycastInteraction();
        }
    }

    private void PerformRaycastInteraction()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                var selectedItemID = SelectedItemState.SelectedItemID;

                if (!string.IsNullOrEmpty(selectedItemID))
                {
                    OnUseItemCommand?.Invoke(interactable, selectedItemID);
                    SelectedItemState.Clear();
                }
                else
                {
                    OnInteractCommand?.Invoke(interactable);
                }
            }
            else
            {
                // Ground/Wall Click
                if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID))
                {
                    SelectedItemState.Clear();
                }
            }
        }
        else
        {
            // Sky Click
            if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID))
            {
                SelectedItemState.Clear();
            }
        }
    }

    public void LockInput() { isInputLocked = true; }
    public void UnlockInput() { isInputLocked = false; }
}