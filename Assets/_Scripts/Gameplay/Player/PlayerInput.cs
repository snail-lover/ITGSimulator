using Game.Core;
using Game.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInput : MonoBehaviour
{
    [Header("Setup")]
    public Camera mainCamera;
    public LayerMask clickableLayers;
    
    // References to the "Body Parts"
    public PlayerMotor motor;
    public PlayerHoldController holder;
    public PlayerDragController dragger; // <--- NEW REFERENCE

    public static event System.Action<IInteractable> OnInteractCommand;
    public static event System.Action<IInteractable, string> OnUseItemCommand;

    private bool isInputLocked = false;

    void Awake()
    {
        if (motor == null) motor = GetComponent<PlayerMotor>();
        if (holder == null) holder = GetComponent<PlayerHoldController>();
        if (dragger == null) dragger = GetComponent<PlayerDragController>(); // <--- Auto Get
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
        HandleKeyboardAction(); 
    }
    
    private void HandleKeyboardAction()
    {
        // "Holster" (F)
        if (Input.GetKeyDown(KeyCode.F))
        {
            // If dragging, F could also stop dragging? 
            // For now, let's keep F for inventory holstering only, or mirror Q.
            if (dragger != null && dragger.IsDragging)
            {
                dragger.StopDrag();
                return;
            }

            if (holder != null)
            {
                holder.DropItem(); 
                SelectedItemState.Clear();
            }
        }

        // "Drop / Stop Interaction" (Q)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // --- NEW LOGIC START ---
            
            // 1. Priority: If dragging, STOP dragging.
            if (dragger != null && dragger.IsDragging)
            {
                dragger.StopDrag();
                return; // Do NOT throw an item if we just stopped dragging
            }

            // 2. Secondary: If not dragging, Throw held item.
            if (holder != null)
            {
                holder.DropHeldItemToWorld();
                SelectedItemState.Clear();
            }
            
            // --- NEW LOGIC END ---
        }
    }

    // ... (Keep HandleMovement, HandleMouseClick, etc. exactly as they were) ...
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
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    InteractionController.Instance.OnRightClickInteract(interactable);
                    return; 
                }
            }

            if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID))
            {
                SelectedItemState.Clear();
            }
            return;
        }

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
                if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID)) SelectedItemState.Clear();
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID)) SelectedItemState.Clear();
        }
    }

    public void LockInput() { isInputLocked = true; }
    public void UnlockInput() { isInputLocked = false; }
}