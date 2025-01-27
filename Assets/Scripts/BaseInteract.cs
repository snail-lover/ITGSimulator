using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BaseInteract : MonoBehaviour, IInteractable
{
    private GameObject currentHoverText;
    public GameObject hoverTextPrefab; // Assign the prefab for the hover text in the Inspector
    public float interactionRange = 2f;
    private bool isPlayerMovingToInteract = false;
    public AudioClip soundEffect;
    private AudioSource audioSource;

    protected virtual void Start()
    {

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)  
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.5f;
       
    }

    void Update()
    {
        if (isPlayerMovingToInteract)
        {
            // Check if the player has arrived within interaction range
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(player.transform.position, transform.position);
                if (distance <= interactionRange)
                {
                    Interact(); // Trigger interaction
                    isPlayerMovingToInteract = false; // Stop checking
                }
            }
        }
    }
    public virtual void OnClick()
    {
        // Get the player and their NavMeshAgent
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            NavMeshAgent playerAgent = player.GetComponent<NavMeshAgent>();
            if (playerAgent != null)
            {
                // Move the player toward this object
                playerAgent.SetDestination(transform.position);
                isPlayerMovingToInteract = true; // Mark as moving to interact
            }
        }
    }

    public virtual void Interact()
    {
        Debug.Log($"{gameObject.name} interacted with!");
        if (soundEffect != null)
            audioSource.PlayOneShot(soundEffect);
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
