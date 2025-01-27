using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BaseInteract : MonoBehaviour, IInteractable
{
    private GameObject currentHoverText;
    public GameObject hoverTextPrefab; // Assign the prefab for the hover text in the Inspector
   public float interactionRange = 2f;
   
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
         if (player != null)
          {
              float distance = Vector3.Distance(player.transform.position, transform.position);
               if (distance <= interactionRange)
        {
            Debug.Log($"{gameObject.name} interacted with!");
        }
    }

       NavMeshAgent playerAgent = GameObject.FindGameObjectWithTag("Player").GetComponent<NavMeshAgent>();
        if (playerAgent != null)
        {
            playerAgent.isStopped = true;
        }


    }

    public virtual void WhenHovered()
    {
        //If there is no Hover text, Instantiate the hover thext
        if (currentHoverText == null)
        {
            currentHoverText = Instantiate(hoverTextPrefab, Input.mousePosition, Quaternion.identity);
            currentHoverText.transform.SetParent(GameObject.Find("HoverTextCanva").transform, false); // Add to Canvas
        }

        currentHoverText.GetComponent<UnityEngine.UI.Text>().text = gameObject.name; //get the name of the thing hovered
        currentHoverText.transform.position = Input.mousePosition; //put the hover text where the mouse is
    }
    
     public virtual void HideHover()
    {
        if (currentHoverText != null) //if there is a hover text showing, destroy it
        {
            Destroy(currentHoverText);
            currentHoverText = null;
        }
    }
    
    public virtual bool CanInteract()
    {  return true; }
}
