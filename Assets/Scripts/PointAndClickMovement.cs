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

            // Raycast only hits objects in the groundLayer
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}
