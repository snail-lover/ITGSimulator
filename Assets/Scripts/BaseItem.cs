using UnityEngine;
using UnityEngine.AI;
using System.Collections;
// using UnityEngine.UI; // No longer needed for hover text directly

public class BaseItem : MonoBehaviour, IClickable // No longer needs IHoverable here
{
    public CreateInventoryItem item;
    public float pickupRange = 2f;
    private Transform player;
    private NavMeshAgent playerAgent;
    private static BaseItem currentTarget = null; // Track current item target
    private Coroutine pickupCoroutine;
    // Removed: private GameObject currentHoverText;
    // Removed: public GameObject hoverTextPrefab; 
    // Removed: private RectTransform hoverTextRect;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerAgent = playerObject.GetComponent<NavMeshAgent>();
        }
        else
        {
            Debug.LogError("Player object not found! BaseItem cannot initialize.", gameObject);
        }
    }

    public void OnClick()
    {
        // Check if another IClickable (which might be a BaseItem or something else)
        // is currently being interacted with by PointAndClickMovement.
        if (PointAndClickMovement.currentTarget != null && PointAndClickMovement.currentTarget != this)
        {
            // Attempt to reset the other target's interaction state if it's an IClickable
            // This part assumes PointAndClickMovement.currentTarget is always IClickable
            IClickable previousPncTarget = PointAndClickMovement.currentTarget as IClickable;
            if (previousPncTarget != null)
            {
                previousPncTarget.ResetInteractionState();
            }
        }

        PointAndClickMovement.currentTarget = this; // Set this item as the PointAndClickMovement target
        OnItemClicked();
    }

    private void MovePlayerToItem()
    {
        if (playerAgent == null) return;

        if (pickupCoroutine != null)
            StopCoroutine(pickupCoroutine);

        playerAgent.SetDestination(transform.position);
        pickupCoroutine = StartCoroutine(CheckIfArrived());
    }

    public void ResetInteractionState()
    {
        // 'this == null' check is usually for safety in editor or after destruction, 
        // but if the object is truly null, this method wouldn't be called.
        // However, it doesn't hurt.
        if (this == null || !gameObject.activeInHierarchy) return;

        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }

        // Only reset currentTarget if *this* item was the one being targeted for pickup.
        // This prevents one item's ResetInteractionState call from nullifying another item's active pickup.
        if (currentTarget == this)
        {
            currentTarget = null;
        }
    }

    public void OnItemClicked()
    {
        // If another BaseItem is already being targeted for pickup, cancel its pickup.
        if (currentTarget != null && currentTarget != this)
        {
            currentTarget.CancelPickup();
        }

        currentTarget = this; // This item is now the one actively being pursued for pickup.
        if (player == null) return;

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
        if (currentTarget == this && Inventory.Instance != null) // Check if still the current target for pickup
        {
            Inventory.Instance.AddItem(item);
            currentTarget = null; // Clear the pickup target

            // If this item was also the PointAndClickMovement's general target, clear that too.
            if (PointAndClickMovement.currentTarget == this)
            {
                PointAndClickMovement.currentTarget = null;
            }

            Destroy(gameObject);
        }
    }

    private IEnumerator CheckIfArrived()
    {
        if (playerAgent == null) yield break;

        // Wait while path is being calculated OR (distance is too far AND this item is still the PointAndClickMovement's target)
        // The PointAndClickMovement.currentTarget check ensures that if the player clicks elsewhere, this coroutine stops trying to pickup.
        while (playerAgent.pathPending ||
              (playerAgent.remainingDistance > playerAgent.stoppingDistance + 0.1f && // Use stoppingDistance for more accuracy
               playerAgent.remainingDistance > pickupRange && // Also ensure it's within custom pickupRange
               (PointAndClickMovement.currentTarget == this || currentTarget == this) // Check both possible target states
              ))
        {
            // If the player has no path or has arrived but is not close enough
            if (!playerAgent.pathPending && playerAgent.remainingDistance <= playerAgent.stoppingDistance + 0.1f &&
                playerAgent.remainingDistance > pickupRange &&
                (PointAndClickMovement.currentTarget == this || currentTarget == this))
            {
                // Player is at NavMesh destination but not close enough to pick up.
                // This might happen if item is slightly off-mesh or pickupRange is very small.
                // Optionally, add logic here (e.g., try to move closer, or just fail)
                // For now, we'll let it break and attempt pickup if PointAndClickMovement.currentTarget is still this.
                break;
            }
            yield return null;
        }

        // Only pickup if still the current PointAndClickMovement target OR the specific BaseItem pickup target
        if ((PointAndClickMovement.currentTarget == this || currentTarget == this) &&
            Vector3.Distance(player.position, transform.position) <= pickupRange)
        {
            Pickup();
        }
        else if (currentTarget == this) // If it was the specific pickup target but player moved away or clicked something else
        {
            // Optionally, reset currentTarget if the player moved away/didn't reach
            // This prevents an item from remaining "targeted" if the player gives up.
            // However, PointAndClickMovement.currentTarget will handle the broader interaction cancellation.
            // For now, let Pickup() handle its own `currentTarget == this` check.
        }
        pickupCoroutine = null;
    }

    public void CancelPickup()
    {
        // This method is called by another BaseItem to tell *this* item to stop its pickup process.
        if (pickupCoroutine != null)
        {
            StopCoroutine(pickupCoroutine);
            pickupCoroutine = null;
        }
        if (currentTarget == this) // If this item was indeed the one actively being picked up
        {
            currentTarget = null;
        }
    }

    // --- REMOVED HOVER METHODS ---
    // public void WhenHovered() { ... }
    // public void HideHover() { ... }
}