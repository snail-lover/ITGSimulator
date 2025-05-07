using UnityEngine;
using System.Collections.Generic; // Required for Lists

public class FloorVisibilityManager : MonoBehaviour
{
    // Assign these floor parent GameObjects in the Inspector
    public GameObject lowerFloorParent;
    public GameObject firstFloorParent;
    public GameObject secondFloorParent;

    // Optional: Store references to renderers and colliders for performance
    private List<Renderer> lowerFloorRenderers;
    private List<Renderer> firstFloorRenderers;
    private List<Renderer> secondFloorRenderers;

    private List<Collider> lowerFloorColliders;
    private List<Collider> firstFloorColliders;
    private List<Collider> secondFloorColliders;


    // Enum to make floor identification clearer
    public enum FloorLevel
    {
        Lower = 0,
        First = 1,
        Second = 2
    }

    private FloorLevel currentVisibleFloor = FloorLevel.Lower; // Example starting floor

    void Start()
    {
        // Optional: Pre-cache components if needed
        CacheFloorComponents();

        // Set the initial visibility based on the starting floor
        UpdateFloorVisibility(currentVisibleFloor);
    }

    // --- Optional: Caching for potential performance improvement ---
    void CacheFloorComponents()
    {
        lowerFloorRenderers = GetAllComponentsInChildren<Renderer>(lowerFloorParent);
        firstFloorRenderers = GetAllComponentsInChildren<Renderer>(firstFloorParent);
        secondFloorRenderers = GetAllComponentsInChildren<Renderer>(secondFloorParent);
        Debug.Log($"Cached {lowerFloorRenderers.Count} renderers for Lower Floor.");
        Debug.Log($"Cached {firstFloorRenderers.Count} renderers for First Floor.");
        Debug.Log($"Cached {secondFloorRenderers.Count} renderers for Second Floor.");

        lowerFloorColliders = GetAllComponentsInChildren<Collider>(lowerFloorParent);
        firstFloorColliders = GetAllComponentsInChildren<Collider>(firstFloorParent);
        secondFloorColliders = GetAllComponentsInChildren<Collider>(secondFloorParent);
        Debug.Log($"Cached {lowerFloorColliders.Count} colliders for Lower Floor.");
        Debug.Log($"Cached {firstFloorColliders.Count} colliders for First Floor.");
        Debug.Log($"Cached {secondFloorColliders.Count} colliders for Second Floor.");
    }

    List<T> GetAllComponentsInChildren<T>(GameObject parent) where T : Component
    {
        if (parent == null) return new List<T>();
        // Use 'true' to include inactive Components as well,
        // in case they start disabled for some reason.
        return new List<T>(parent.GetComponentsInChildren<T>(true));
    }
    // --- End Optional Caching ---


    /// <summary>
    /// Makes the target floor visible and interactive, and hides/disables others.
    /// </summary>
    /// <param name="targetFloor">The floor level to make visible.</param>
    public void UpdateFloorVisibility(FloorLevel targetFloor)
    {
        Debug.Log($"Switching visible floor to: {targetFloor}");
        currentVisibleFloor = targetFloor;

        // Set visibility and interactivity based on the target floor
        // Using cached lists (if enabled):
        if (lowerFloorRenderers != null && lowerFloorColliders != null) // Check if caching was done
        {
            SetFloorComponentsEnabled(lowerFloorRenderers, lowerFloorColliders, targetFloor == FloorLevel.Lower);
            SetFloorComponentsEnabled(firstFloorRenderers, firstFloorColliders, targetFloor == FloorLevel.First);
            SetFloorComponentsEnabled(secondFloorRenderers, secondFloorColliders, targetFloor == FloorLevel.Second);
        }
        else // Fallback to finding components on demand
        {
            SetFloorActiveState(lowerFloorParent, targetFloor == FloorLevel.Lower);
            SetFloorActiveState(firstFloorParent, targetFloor == FloorLevel.First);
            SetFloorActiveState(secondFloorParent, targetFloor == FloorLevel.Second);
        }
    }

    /// <summary>
    /// Enables or disables all Renderer and Collider components for a floor using cached lists.
    /// </summary>
    private void SetFloorComponentsEnabled(List<Renderer> renderers, List<Collider> colliders, bool isEnabled)
    {
        foreach (Renderer rend in renderers)
        {
            rend.enabled = isEnabled;
        }
        foreach (Collider col in colliders)
        {
            col.enabled = isEnabled;
        }
        // Optional: Log
        // if (renderers.Count > 0) Debug.Log($"{renderers[0].transform.root.name}: Set {renderers.Count} renderers and {colliders.Count} colliders to enabled={isEnabled}");
    }


    /// <summary>
    /// Enables or disables all Renderer and Collider components within a parent GameObject (on-demand version).
    /// </summary>
    /// <param name="floorParent">The parent GameObject of the floor.</param>
    /// <param name="isEnabled">True to enable components, false to disable.</param>
    private void SetFloorActiveState(GameObject floorParent, bool isEnabled)
    {
        if (floorParent == null)
        {
            Debug.LogWarning("Floor parent GameObject is not assigned.");
            return;
        }

        // Find all renderers each time
        Renderer[] renderers = floorParent.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            rend.enabled = isEnabled;
        }

        // Find all colliders each time
        Collider[] colliders = floorParent.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = isEnabled;
        }

        // Optional: Log how many components were affected
        // Debug.Log($"{floorParent.name}: Set {renderers.Length} renderers and {colliders.Length} colliders to enabled={isEnabled}");
    }


    // --- Example Trigger Method (called from staircase script) ---
    public void PlayerChangedFloor(int floorIndex) // 0=Lower, 1=First, 2=Second
    {
         if (System.Enum.IsDefined(typeof(FloorLevel), floorIndex))
         {
             UpdateFloorVisibility((FloorLevel)floorIndex);
         }
         else
         {
             Debug.LogError($"Invalid floor index received: {floorIndex}");
         }
    }
}