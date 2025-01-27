using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public Camera mainCamera; // Assign the main camera in the Inspector
    public LayerMask groundLayer; // Layer for the ground
    private NavMeshAgent agent;
    private IInteractable lastHovered; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
    
     Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0)) // Left-click
        {            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                Debug.Log("It hit: " + hit.collider.gameObject.name); // Log what was hit

                // Check if the object is interactable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.OnClick(); // Call OnClick for interactables
                }
                else
                {
                    // Move the player if the object is in the groundLayer
                    if (((1 << hit.collider.gameObject.layer) & groundLayer) != 0)
                    {
                        agent.SetDestination(hit.point);
                        Debug.Log("Player is moving to: " + hit.point);
                    }
                }
            }
        }
        if (Physics.Raycast(ray, out RaycastHit hover, Mathf.Infinity))
        {
            IInteractable interactable = hover.collider.GetComponent<IInteractable>();
            if (interactable !=null)
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
