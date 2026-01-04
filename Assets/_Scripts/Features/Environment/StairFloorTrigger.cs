using UnityEngine;
using Game.Core; // <- FloorLevel enum now lives in Core

[RequireComponent(typeof(Collider))]
public class StairFloorTrigger : MonoBehaviour
{
    [Tooltip("The floor this trigger leads TO. 0=Lower, 1=First, 2=Second")]
    [Range(0, 2)]
    public int destinationFloorIndex = 0;

    private FloorLevel DestinationFloor => (FloorLevel)Mathf.Clamp(destinationFloorIndex, 0, 2);

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Player enters: tell the manager to switch visible floor.
        if (other.CompareTag("Player"))
        {
            var mgr = FloorVisibilityManager.Instance; // Gameplay singleton
            if (mgr != null)
            {
                // Your manager’s existing API is int-based:
                mgr.PlayerChangedFloor((int)DestinationFloor);
            }
            else
            {
                Debug.LogWarning("[StairFloorTrigger] FloorVisibilityManager.Instance not found.", this);
            }
            return;
        }

        // NPC enters: update NPC’s own floor + refresh based on current visible floor.
        var npc = other.GetComponentInParent<NpcController>(); // Features type; ok from here
        if (npc != null)
        {
            npc.NotifyNpcChangedFloor(DestinationFloor);

            var mgr = FloorVisibilityManager.Instance;
            if (mgr != null)
            {
                npc.UpdateVisibilityBasedOnPlayerFloor(mgr.CurrentVisibleFloor); // CurrentVisibleFloor should be Game.Core.FloorLevel
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        destinationFloorIndex = Mathf.Clamp(destinationFloorIndex, 0, 2);
    }
#endif
}
