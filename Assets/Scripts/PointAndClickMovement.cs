using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public Camera mainCamera; // Assign in the Inspector
    public LayerMask groundLayer; // Layer for the ground
    private NavMeshAgent agent;
    private IClickable lastHovered;
    public static IClickable currentTarget; // Tracks the current interaction target
    private bool isInteracting = false; // Add this flag

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
        HandleMovementInput();
        HandleHoverEffects();
    }

    private void HandleMovementInput()
    {
        // Prevent further clicks if currently interacting with an NPC
        if (isInteracting)
        {
            return; // Do nothing while interacting
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IClickable clickable = hit.collider.GetComponent<IClickable>();
                if (clickable != null)
                {
                    // Clear previous interaction state
                    if (currentTarget != null)
                    {
                        currentTarget.ResetInteractionState();
                    }

                    currentTarget = clickable;
                    clickable.OnClick();

                    // Check if the NPC is talking after clicking
                    if (BaseNPC.currentTarget != null && BaseNPC.currentTarget.isTalking)
                    {
                        isInteracting = true; // Set the flag to true
                        return; // Do nothing while talking
                    }
                }
                else
                {
                    // Handle ground click - clear any interaction
                    agent.SetDestination(hit.point);
                    if (currentTarget != null)
                    {
                        currentTarget.ResetInteractionState();
                        currentTarget = null;
                    }
                    // Clear NPC interaction state
                    if (BaseNPC.currentTarget != null)
                    {
                        BaseNPC.currentTarget.ResetInteractionState();
                    }
                }
            }
        }
    }

    public void EndInteraction()
    {
        isInteracting = false; // Reset the flag when interaction ends
    }

    private void HandleHoverEffects()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hover, Mathf.Infinity, LayerMask.GetMask("Interactable")))
        {
            IClickable interactable = hover.collider.GetComponent<IClickable>();
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