using UnityEngine;
using UnityEngine.AI;

public class BaseInteract : MonoBehaviour, IInteractable
{
    public virtual void OnClick()
    {
        Debug.Log("On click fired");

    GameObject player = GameObject.FindGameObjectWithTag("Player");
    if (player != null)
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(transform.position);
            Debug.Log("Player is moving to interactable object");
        }
        else
        {
            Debug.LogWarning("player does not have a NavMeshAgent component");
        }
    }
    else
    {
        Debug.LogWarning("Player not found in scene");
    }


    }

    public virtual void Interact()
    {
        Debug.Log("Interacting");  
    }

    public virtual void WhenHovered()
    {
        Debug.Log("Hovering Object");
    }

    public virtual bool CanInteract()
    {  return true; }
}
