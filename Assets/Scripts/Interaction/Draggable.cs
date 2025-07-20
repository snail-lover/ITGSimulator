// Draggable.cs (Version 6 - Standalone IClickable)
using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))] // -- CHANGED -- No longer requires BaseItem
public class Draggable : MonoBehaviour, IClickable // -- CHANGED -- Implements IClickable directly
{
    // -- STATIC EVENTS FOR UI (Unchanged) ---
    public static event Action<Draggable> OnDragStarted;
    public static event Action<Draggable> OnDragEnded;
    public static Draggable CurrentlyDraggedItem { get; private set; }


    // --- Item Data (Previously in BaseItem) ---
    [Header("Item Data")]
    public CreateInventoryItem item;
    [Tooltip("A UNIQUE ID for this specific item instance in the world. This is crucial for the save system.")]
    public string worldItemID;

    // --- States (Unchanged) ---
    private enum DragState { None, PlayerApproaching, PotentialDrag, IsDragging }
    private DragState currentState = DragState.None;

    // --- Component References ---
    private Rigidbody rb;
    private Camera mainCamera;
    private Transform playerTransform;
    private Collider objCollider;

    // --- Drag/Click Parameters (Unchanged section) ---
    [Header("Interaction")]
    public float dragThreshold = 10f;
    private Vector2 mouseDownPosition;

    [Header("Dragging Physics")]
    public float maxGrabDistance = 5f;
    public float slideSpeed = 25f;
    public float recenterForce = 15f;
    public bool allowDepthControl = true;
    public float depthControlSpeed = 10f;

    [Header("Flinging")]
    [Range(0f, 5f)] public float flingMultiplier = 1.2f;
    public float maxFlingSpeed = 20f;
    [Range(1, 10)] public int velocityAverageFrames = 3;

    [Header("Visual Feedback")]
    public GameObject rangeVisualizerPrefab;
    private GameObject currentVisualizer;

    // --- Drag State Variables (Unchanged) ---
    private Vector3 dragOffset;
    private float dragPlaneDistance;
    private List<Vector3> velocityHistory = new List<Vector3>();
    private bool dragIntentRegistered = false;

    void Awake()
    {
        // --- ADDED: Critical Save System Check (from BaseItem) ---
        if (string.IsNullOrEmpty(worldItemID))
        {
            Debug.LogError($"[{gameObject.name}] This Draggable item has no 'World Item ID' set! It will not save correctly.", this);
        }
        if (WorldDataManager.Instance != null && WorldDataManager.Instance.IsWorldItemPickedUp(worldItemID))
        {
            Debug.Log($"[{worldItemID}] has already been picked up. Destroying this instance.");
            Destroy(gameObject);
            return;
        }

        rb = GetComponent<Rigidbody>();
        // baseItem = GetComponent<BaseItem>(); // -- REMOVED --
        mainCamera = Camera.main;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
        objCollider = GetComponent<Collider>();
    }

    public void OnClick()
    {
        if (currentState != DragState.None)
        {
            // If we are approaching, a re-click registers drag intent.
            if (currentState == DragState.PlayerApproaching)
            {
                RegisterDragIntent();
            }
            return;
        }

        // Is the player in range for an instant interaction?
        if (Vector3.Distance(transform.position, playerTransform.position) <= maxGrabDistance)
        {
            Debug.Log($"[{name}] Player in range. Entering PotentialDrag state.");
            currentState = DragState.PotentialDrag;
            mouseDownPosition = Input.mousePosition;
        }
        else
        {
            // Player is OUT of range. We must handle the approach ourselves now.
            Debug.Log($"[{name}] Player out of range. Initiating approach.");
            InitiateApproach();
        }
    }

    /// <summary>
    /// Called by PointAndClickMovement if the player clicks away, cancelling this interaction.
    /// </summary>
    public void ResetInteractionState()
    {
        // This is effectively the old "ResetFromExternal"
        if (PointAndClickMovement.currentTarget == this)
        {
            PointAndClickMovement.Instance?.UnlockPlayerApproach();
        }
        currentState = DragState.None;
        dragIntentRegistered = false;
        Debug.Log($"[{name}] Interaction state reset.");
    }


    void Update()
    {
        if (currentState == DragState.PlayerApproaching)
        {
            bool heldAndMoved = Input.GetMouseButton(0) && (Vector2.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold);
            bool reclickIntentActive = dragIntentRegistered && Input.GetMouseButton(0);

            if (heldAndMoved || reclickIntentActive)
            {
                if (Vector3.Distance(transform.position, playerTransform.position) <= maxGrabDistance)
                {
                    Debug.Log("Drag initiated DURING player approach!");
                    PointAndClickMovement.Instance?.StopPlayerMovementAndNotify();
                    ResetInteractionState(); // Abort the approach.
                    StartDrag(true);
                }
            }
        }
        else if (currentState == DragState.PotentialDrag)
        {
            if (Vector2.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold)
            {
                StartDrag(true);
            }
        }
        else if (currentState == DragState.IsDragging)
        {
            // If the mouse button is no longer held down, end the drag.
            // This is more reliable than OnMouseUp() when UI elements might interfere.
            if (!Input.GetMouseButton(0))
            {
                Debug.Log($"[{name}] Detected mouse button UP in Update while dragging. Ending drag.");
                // Ensure the state is correctly reset before calling EndDrag.
                // Although OnMouseUp would also do this, this ensures it happens.
                currentState = DragState.None;
                EndDrag();
            }
        }
    }


    public void InitiateApproach()
    {
        Debug.Log($"[{name}] Player is approaching. Entering PlayerApproaching state.");
        currentState = DragState.PlayerApproaching;
        mouseDownPosition = Input.mousePosition;
        dragIntentRegistered = false;

        // --- CHANGED: We now command the player directly ---
        PointAndClickMovement.Instance.LockPlayerApproach(this);
        PointAndClickMovement.Instance.SetPlayerDestination(transform.position, true);
    }

    public void RegisterDragIntent()
    {
        // This is called when the player re-clicks during an approach.
        if (currentState == DragState.PlayerApproaching)
        {
            Debug.Log("Drag intent registered by player re-click!");
            dragIntentRegistered = true;
            // We also need to update the mouse down position, because the player
            // might have released the first click and this is a new one.
            mouseDownPosition = Input.mousePosition;
        }
    }





    void OnMouseUp()
    {
        // Debug.Log($"[{name}] OnMouseUp called. CurrentState: {currentState}"); // Optional debug
        if (currentState == DragState.PotentialDrag)
        {
            currentState = DragState.None;
            // Debug.Log($"[{name}] In-range click detected. Picking up instantly. New State: {currentState}"); // Optional debug
            AttemptPickup(); // Directly call our own pickup logic.
        }
        else if (currentState == DragState.IsDragging)
        {
            currentState = DragState.None;
            // Debug.Log($"[{name}] Drag ended. New State: {currentState}. Calling EndDrag()."); // Optional debug
            EndDrag();
        }
        // else { Debug.Log($"[{name}] OnMouseUp called, but not in PotentialDrag or IsDragging state. CurrentState: {currentState}"); } // Optional debug
    }

    /// <summary>
    /// A unified method for picking up the item, whether by click or drag.
    /// </summary>
    private void AttemptPickup()
    {
        if (Inventory.Instance != null)
        {
            Debug.Log($"[{name}] Adding item '{item.itemName}' to inventory.");

            // 1. Add the item data to the player's inventory.
            Inventory.Instance.AddItem(item);

            // 2. Tell the WorldDataManager this item has been removed from the scene.
            WorldDataManager.Instance?.MarkWorldItemAsPickedUp(worldItemID);

            // 3. CRITICAL: If this item is still the global target in PointAndClickMovement,
            //    we must clear the reference before destroying the object to prevent a null pointer.
            if (PointAndClickMovement.currentTarget == this)
            {
                PointAndClickMovement.currentTarget = null;
            }

            // 4. The item is now in the inventory, so destroy its representation in the game world.
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"[{name}] Inventory.Instance is null. Cannot pick up item.");
        }
    }



    private void StartDrag(bool snapToCursor)
    {

        CurrentlyDraggedItem = this;

        currentState = DragState.IsDragging;

        // --- NEW: Fire the event to notify the UI ---
        OnDragStarted?.Invoke(this);

        PointAndClickMovement.Instance?.HardLockPlayerMovement();

        dragPlaneDistance = Vector3.Dot(transform.position - mainCamera.transform.position, mainCamera.transform.forward);

        if (snapToCursor)
        {
            dragOffset = Vector3.zero;
        }
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 pointOnPlane = ray.GetPoint(dragPlaneDistance);
            dragOffset = transform.position - pointOnPlane;
        }

        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 5f;
        velocityHistory.Clear();
        ShowRangeVisualizer();
    }

    /// <summary>
    /// Called from OnMouseUp when a drag operation is concluded.
    /// </summary>
    private void EndDrag()
    {

        if (CurrentlyDraggedItem == this)
        {
            CurrentlyDraggedItem = null;
        }


        // 1. Reset state and unlock player input immediately.
        currentState = DragState.None;
        PointAndClickMovement.Instance?.HardUnlockPlayerMovement();

        // 2. Fire the event for UI or other systems to know the drag has finished.
        OnDragEnded?.Invoke(this);

        // 3. Check if the mouse is over the inventory panel on release.
        bool wasDroppedOnInventory = (InventoryPanel.Instance != null && InventoryPanel.Instance.IsPointerOverPanel);

        if (wasDroppedOnInventory)
        {
            // --- ACTION: PICKUP ---
            // The item was successfully dropped on the inventory.
            Debug.Log($"[{name}] Drag ended over inventory panel. Attempting pickup.");

            // Hide the visualizer. We do this before pickup in case the visualizer
            // is a child of this object, which is about to be destroyed.
            HideRangeVisualizer();

            // Call our single, unified pickup method. It handles everything else.
            AttemptPickup();
        }
        else
        {
            // --- ACTION: FLING ---
            // The item was dropped into the game world.
            Debug.Log($"[{name}] Drag ended in world space. Applying fling physics.");

            // Re-enable standard physics properties for the object.
            rb.useGravity = true;
            rb.linearDamping = 0.5f; // Or your scene's default damping value

            // Calculate and apply the fling velocity based on the last few frames of movement.
            if (velocityHistory.Count > 0)
            {
                Vector3 averageVelocity = Vector3.zero;
                foreach (var v in velocityHistory)
                {
                    averageVelocity += v;
                }
                averageVelocity /= velocityHistory.Count;

                Vector3 flingVelocity = Vector3.ClampMagnitude(averageVelocity * flingMultiplier, maxFlingSpeed);
                rb.linearVelocity = flingVelocity;
            }

            // Hide the visualizer as the drag is complete.
            HideRangeVisualizer();
        }
    }

    void FixedUpdate()
    {
        if (currentState == DragState.IsDragging)
        {
            // 1. Determine the cursor's target position in the world
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 targetPosition = ray.GetPoint(dragPlaneDistance) + dragOffset;

            // 2. Clamp this position to the player's grab range
            Vector3 directionFromPlayer = targetPosition - playerTransform.position;
            if (directionFromPlayer.magnitude > maxGrabDistance)
            {
                targetPosition = playerTransform.position + directionFromPlayer.normalized * maxGrabDistance;
            }

            // 3. Calculate the vector from the object to the target
            Vector3 moveDirection = targetPosition - rb.position;
            float distanceToTarget = moveDirection.magnitude;

            // 4. Cast a ray to see if we would hit anything on the way to the target
            if (Physics.Raycast(rb.position, moveDirection, out RaycastHit hit, distanceToTarget, -1, QueryTriggerInteraction.Ignore))
            {
                // --- We hit something! This is where the magic happens. ---

                // A. We project the remainder of the movement onto the surface we hit.
                // This makes the object slide along the wall/floor.
                Vector3 projectedMove = Vector3.ProjectOnPlane(moveDirection, hit.normal);
                rb.linearVelocity = projectedMove.normalized * slideSpeed * Time.fixedDeltaTime;

                // B. We also apply a smaller, constant force towards the actual cursor target.
                // This helps the object "unstick" from sharp corners and feel more responsive.
                rb.AddForce(moveDirection * recenterForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
            }
            else
            {
                // --- Path is clear! Move directly towards the cursor. ---
                rb.linearVelocity = moveDirection * slideSpeed * Time.fixedDeltaTime;
            }

            // 5. Record velocity for flinging
            velocityHistory.Add(rb.linearVelocity);
            if (velocityHistory.Count > velocityAverageFrames)
            {
                velocityHistory.RemoveAt(0);
            }
        }
    }

    // --- HELPER METHODS FOR VISUAL FEEDBACK ---

    private void ShowRangeVisualizer()
    {
        if (rangeVisualizerPrefab != null)
        {
            if (currentVisualizer == null)
            {
                currentVisualizer = Instantiate(rangeVisualizerPrefab, playerTransform.position, Quaternion.identity, playerTransform);
            }
            // Scale the visualizer to match the grab distance. Assuming it's a sphere/ring of default size 1.
            currentVisualizer.transform.localScale = new Vector3(maxGrabDistance * 2, 0.01f, maxGrabDistance * 2); // For a flat ring/circle
            currentVisualizer.SetActive(true);
        }
    }

    private void HideRangeVisualizer()
    {
        if (currentVisualizer != null)
        {
            currentVisualizer.SetActive(false);
        }
    }

    // Optional: If the Draggable script is disabled or destroyed while dragging, make sure we unlock player movement.
    void OnDisable()
    {
        if (currentState == DragState.IsDragging)
        {
            // This will call EndDrag, which handles most cleanup
            OnMouseUp();
        }

        // --- NEW: Failsafe to clear the static reference ---
        if (CurrentlyDraggedItem == this)
        {
            CurrentlyDraggedItem = null;
        }
    }

}


