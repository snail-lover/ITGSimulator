using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public Camera mainCamera;            // Assign the main camera in the Inspector
    public LayerMask groundLayer;        // Layer for the ground
    private NavMeshAgent agent;
    private IInteractable lastHovered;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing from Player.");
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found. Please assign a camera to 'mainCamera'.");
            }
        }
    }

    private void Update()
    {
        // Prevent player input if dialogue is active
        if (BaseDialogue.IsDialogueActive)
        {
            return;
        }

        HandleMovementInput();
        HandleHoverEffects();
    }

    /// <summary>
    /// Handles player movement based on mouse input.
    /// </summary>
    private void HandleMovementInput()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0)) // Left-click
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer | LayerMask.GetMask("Interactable")))
            {
                Debug.Log("Clicked on: " + hit.collider.gameObject.name);

                // Check if the object is interactable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.OnClick(); // Call OnClick for interactables
                }

                // Check if the hit object is a BaseNPC
                BaseNPC npc = hit.collider.GetComponent<BaseNPC>();
                if (npc != null)
                {
                    npc.Interact(); // Initiate interaction with NPC
                }

                // If the clicked object is on the ground layer, move the player
                if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
                {
                    agent.SetDestination(hit.point);
                    Debug.Log("Player is moving to: " + hit.point);
                    BaseNPC.ClearCurrentTarget(); // Clear any existing NPC interaction
                }
            }
        }
    }

    /// <summary>
    /// Handles hover effects for interactable objects.
    /// </summary>
    private void HandleHoverEffects()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hover, Mathf.Infinity, LayerMask.GetMask("Interactable")))
        {
            IInteractable interactable = hover.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.WhenHovered();

                if (interactable != lastHovered)
                {
                    lastHovered?.HideHover();
                    lastHovered = interactable;
                }
            }
            else
            {
                lastHovered?.HideHover();
                lastHovered = null;
            }
        }
        else
        {
            lastHovered?.HideHover();
            lastHovered = null;
        }
    }
}
