using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;
public class BaseItem : MonoBehaviour, IClickable
{
    public CreateInventoryItem item;
    public float pickupRange = 2f;
    private Transform player;
    private NavMeshAgent playerAgent;
    private static BaseItem currentTarget = null; // Track current item target
     private Coroutine pickupCoroutine;
     private GameObject currentHoverText;
    public GameObject hoverTextPrefab; 
    private RectTransform hoverTextRect;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject.transform;
        playerAgent = playerObject.GetComponent<NavMeshAgent>();
    }

     public void OnClick()
    {
        if (PointAndClickMovement.currentTarget != null && (object)PointAndClickMovement.currentTarget != this)
        {
            PointAndClickMovement.currentTarget.ResetInteractionState();
        }

        PointAndClickMovement.currentTarget = this;
        OnItemClicked();
    }

    private void MovePlayerToItem()
    {
        if (pickupCoroutine != null)
            StopCoroutine(pickupCoroutine);
            
        playerAgent.SetDestination(transform.position);
        pickupCoroutine = StartCoroutine(CheckIfArrived());
    }   

    public void ResetInteractionState()
    {
        if (this == null) return;

        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }
        currentTarget = null;
    }

    public void OnItemClicked()
    {
        if (currentTarget != null && currentTarget != this)
        {
            currentTarget.CancelPickup(); // Cancel previous target
        }

        currentTarget = this;
        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= pickupRange)
        {
            Pickup();
        }
        else
        {
            MovePlayerToItem();
        }
    }

    public void Pickup()
    {
        if (currentTarget == this)
        {
            Inventory.Instance.AddItem(item);
            currentTarget = null;
            Destroy(gameObject);
            
        }
    }

   private IEnumerator CheckIfArrived()
    {
        while (playerAgent.pathPending || 
              (playerAgent.remainingDistance > pickupRange && (object)PointAndClickMovement.currentTarget == this))
        {
            yield return null;
        }

        // Only pickup if still the current target
        if ((object)PointAndClickMovement.currentTarget == this) 
            Pickup();
    }

    public void CancelPickup()
    {
        if (currentTarget == this)
        {
            currentTarget = null;
        }
    }

    public void WhenHovered()
    {
        if (currentHoverText == null && hoverTextPrefab != null)
        {
            currentHoverText = Instantiate(hoverTextPrefab, UnityEngine.Object.FindFirstObjectByType<Canvas>().transform);
            hoverTextRect = currentHoverText.GetComponent<RectTransform>();

            Text textComponent = currentHoverText.GetComponent<Text>();
            if (textComponent == null)
            {
                Debug.LogError("Hover text prefab is missing a Text component!");
                return;
            }
            textComponent.text = gameObject.name;
        }
    }

    public void HideHover()
    {
        if (currentHoverText != null)
        {
            Destroy(currentHoverText);
            currentHoverText = null;
        }
    }
}
