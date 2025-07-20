// Action_StartDialogue.cs (Refactored)

using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Action_StartDialogue : MonoBehaviour, IInteractableAction
{
    private NpcInteraction npcInteraction; // <--- Changed from NpcController

    private void Awake()
    {
        // Find the NpcInteraction component instead of the controller.
        npcInteraction = GetComponentInParent<NpcInteraction>(); // <--- Changed from NpcController
        if (npcInteraction == null)
        {
            Debug.LogError($"Action_StartDialogue on {gameObject.name} could not find an NpcInteraction component in its parent hierarchy!", this);
            this.enabled = false;
        }
    }

    /// <summary>
    /// Called by Interactable.cs when the player is in range.
    /// This method now talks to the dedicated interaction component.
    /// </summary>
    public void ExecuteAction()
    {
        npcInteraction?.InitiateDialogue(); // <--- Changed from npcController
    }

    /// <summary>
    /// Called by Interactable.cs if the interaction is cancelled.
    /// We no longer need to do anything here. The NpcController's state machine
    /// will handle returning to normal after dialogue ends. This simplifies things.
    /// </summary>
    public void ResetAction()
    {
        // No action needed. The NPC's state is managed by the NpcController now.
    }
}