using UnityEngine;
using UnityEngine.AI;

public class PointAndClickMovement : MonoBehaviour
{
    public Camera mainCamera; // Assign the main camera in the Inspector
    public LayerMask groundLayer; // Layer for the ground
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Perform a single raycast
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
                    else
                    {
                        Debug.Log("IInteractable not found on: " + hit.collider.gameObject.name);
                    }
                }
            }
        }
    }
}
