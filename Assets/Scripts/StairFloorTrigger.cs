using UnityEngine;

public class StairFloorTrigger : MonoBehaviour
{
    // Set this in the Inspector for each stair trigger
    // 0 = Lower, 1 = First, 2 = Second
    public int destinationFloorIndex;

    // Reference the single FloorVisibilityManager instance
    public FloorVisibilityManager visibilityManager;

    // Find the manager if not assigned (alternative)
    void Start()
    {
        if (visibilityManager == null)
        {
            visibilityManager = FindObjectOfType<FloorVisibilityManager>();
            if (visibilityManager == null)
            {
                Debug.LogError("StairTrigger could not find FloorVisibilityManager!");
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        // Make sure it's the player entering the trigger
        // Adjust "Player" tag if you use a different one
        if (other.CompareTag("Player"))
        {
            Debug.Log($"Player entered stair trigger for floor {destinationFloorIndex}");
            if (visibilityManager != null)
            {
                // Tell the manager to update visibility
                visibilityManager.PlayerChangedFloor(destinationFloorIndex);

                // --- IMPORTANT FOR NEXT STEP ---
                // Here you would ALSO tell the Player and potentially nearby NPCs
                // about the floor change so their own visibility logic can run.
                // We'll handle that in the "Dynamic Entities" part.
                // Example: other.GetComponent<PlayerVisibility>()?.SetCurrentFloor(destinationFloorIndex);
            }
        }
    }
}