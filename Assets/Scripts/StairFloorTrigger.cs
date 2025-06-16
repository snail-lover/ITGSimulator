using UnityEngine;

public class StairFloorTrigger : MonoBehaviour
{
    [Tooltip("The floor this trigger leads TO. 0=Lower, 1=First, 2=Second")]
    public int destinationFloorIndex; // This is the floor the entity ARRIVES ON.

    private FloorVisibilityManager visibilityManager;

    void Start()
    {
        visibilityManager = FloorVisibilityManager.Instance;
        if (visibilityManager == null)
        {
            Debug.LogError($"[{gameObject.name}] StairTrigger could not find FloorVisibilityManager instance!");
        }

        if (!System.Enum.IsDefined(typeof(FloorVisibilityManager.FloorLevel), destinationFloorIndex))
        {
            Debug.LogError($"[{gameObject.name}] StairTrigger has an invalid destinationFloorIndex: {destinationFloorIndex}. Make sure it's 0, 1, or 2.", this);
        }

        // Ensure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[{gameObject.name}] StairFloorTrigger is missing a Collider component!", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[{gameObject.name}] StairFloorTrigger's Collider was not set to 'Is Trigger'. Forcing it now. Please check setup.", this);
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- Handle Player ---
        if (other.CompareTag("Player"))
        {
            if (visibilityManager != null)
            {
                FloorVisibilityManager.FloorLevel targetPlayerFloor = (FloorVisibilityManager.FloorLevel)destinationFloorIndex;
                Debug.Log($"Player (Tag: {other.tag}) entered stair trigger '{gameObject.name}' for destination floor {targetPlayerFloor} (Index: {destinationFloorIndex}). Player's current FVM floor: {visibilityManager.CurrentVisibleFloor}");

                if (visibilityManager.CurrentVisibleFloor != targetPlayerFloor)
                {
                    visibilityManager.PlayerChangedFloor(destinationFloorIndex);
                }
                else
                {
                    visibilityManager.PlayerChangedFloor(destinationFloorIndex);
                    Debug.Log($"Player re-entered stair trigger for their current floor {targetPlayerFloor}. FVM will re-notify NPCs.");
                }
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Player entered trigger, but FloorVisibilityManager is null!");
            }
        }
        // --- Handle NPC ---
        else
        {
            // KEY CHANGE: Use GetComponentInParent in case 'other' is the child StairSensor
            BaseNPC npc = other.GetComponentInParent<BaseNPC>();
            if (npc != null)
            {
                FloorVisibilityManager.FloorLevel npcNewFloor = (FloorVisibilityManager.FloorLevel)destinationFloorIndex;
                Rigidbody npcRb = other.attachedRigidbody; // Get the Rigidbody associated with the collider that entered

                //Debug.LogWarning($"NPC_TRIGGER_ENTER: NPC '{npc.npcName}' (Collider: {other.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}, HasRigidbodyOnCollidingObject: {npcRb != null}) entered stair trigger '{gameObject.name}'. Attempting to move to floor {npcNewFloor} (Index: {destinationFloorIndex}). NPC's current reported floor: {npc.currentNpcFloorLevel}");

                if (npc.currentNpcFloorLevel != npcNewFloor)
                {
                    npc.NotifyNpcChangedFloor(npcNewFloor);
                }
                else
                {
                    //Debug.Log($"NPC_TRIGGER_INFO: NPC '{npc.npcName}' re-entered stair trigger for their current floor {npcNewFloor}. No change to NPC's internal floor level needed.");
                    if (FloorVisibilityManager.Instance != null)
                    {
                        npc.UpdateVisibilityBasedOnPlayerFloor(FloorVisibilityManager.Instance.CurrentVisibleFloor);
                    }
                }
            }
        }
    }
}