using UnityEngine;

public class StairFloorTrigger : MonoBehaviour
{
    [Tooltip("The floor this trigger leads TO. 0=Lower, 1=First, 2=Second")]
    public int destinationFloorIndex; // This is the floor the entity ARRIVES ON.

    // Reference to FloorVisibilityManager (still needed for player)
    private FloorVisibilityManager visibilityManager; // Made private, will find in Start

    void Start()
    {
        visibilityManager = FloorVisibilityManager.Instance; // Use the singleton
        if (visibilityManager == null)
        {
            Debug.LogError($"[{gameObject.name}] StairTrigger could not find FloorVisibilityManager instance!");
        }

        // Basic validation for destinationFloorIndex
        if (!System.Enum.IsDefined(typeof(FloorVisibilityManager.FloorLevel), destinationFloorIndex))
        {
            Debug.LogError($"[{gameObject.name}] StairTrigger has an invalid destinationFloorIndex: {destinationFloorIndex}. Make sure it's 0, 1, or 2.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- Handle Player ---
        if (other.CompareTag("Player"))
        {
            if (visibilityManager != null)
            {
                // Player has entered, so this trigger's destinationFloorIndex is where the player is NOW.
                // Only change floor if the player is actually moving to a new floor.
                FloorVisibilityManager.FloorLevel targetPlayerFloor = (FloorVisibilityManager.FloorLevel)destinationFloorIndex;
                if (visibilityManager.CurrentVisibleFloor != targetPlayerFloor)
                {
                    Debug.Log($"Player entered stair trigger. Changing player's visible floor to {targetPlayerFloor} (Index: {destinationFloorIndex})");
                    visibilityManager.PlayerChangedFloor(destinationFloorIndex);
                }
                else
                {
                    // Debug.Log($"Player re-entered stair trigger for their current floor {targetPlayerFloor}. No FVM change.");
                }
            }
        }
        // --- Handle NPC ---
        else
        {
            BaseNPC npc = other.GetComponent<BaseNPC>();
            if (npc != null)
            {
                // NPC has entered, so this trigger's destinationFloorIndex is where the NPC is NOW.
                FloorVisibilityManager.FloorLevel npcNewFloor = (FloorVisibilityManager.FloorLevel)destinationFloorIndex;

                // Only notify if the NPC is actually changing to a different floor.
                if (npc.currentNpcFloorLevel != npcNewFloor)
                {
                    Debug.Log($"NPC '{npc.npcName}' entered stair trigger. Updating NPC's current floor to {npcNewFloor} (Index: {destinationFloorIndex})");
                    npc.NotifyNpcChangedFloor(npcNewFloor);
                }
                else
                {
                    // Debug.Log($"NPC '{npc.npcName}' re-entered stair trigger for their current floor {npcNewFloor}. No NPC floor change notification.");
                }
            }
        }
    }
}