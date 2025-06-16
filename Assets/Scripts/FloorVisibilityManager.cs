using UnityEngine;
using System.Collections.Generic;

public class FloorVisibilityManager : MonoBehaviour
{
    public static FloorVisibilityManager Instance { get; private set; }

    [Header("Floor Parents")]
    public GameObject lowerFloorParent;
    public GameObject firstFloorParent;
    public GameObject secondFloorParent;

    [Header("Configuration")]
    [Tooltip("The floor that should be visible when the scene starts.")]
    public FloorLevel startingFloor = FloorLevel.Lower; // <<< CHANGE: Added public field for Inspector

    public enum FloorLevel
    {
        Lower = 0,
        First = 1,
        Second = 2
    }

    // <<< CHANGE: Removed inline initialization. It will now be set in Awake.
    private FloorLevel _currentVisibleFloor;
    public FloorLevel CurrentVisibleFloor => _currentVisibleFloor; // Public getter

    // Optional Caching (can be uncommented and used if preferred)
    /*
    private List<Renderer> lowerFloorRenderers;
    private List<Collider> lowerFloorColliders;
    // ... and for other floors
    */

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if this manager needs to persist across scenes
        }
        else
        {
            Debug.LogWarning("Multiple FloorVisibilityManager instances found. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        // <<< CHANGE: Set the initial floor based on the inspector value.
        _currentVisibleFloor = startingFloor;
    }

    void Start()
    {
        // CacheFloorComponents(); // If using caching
        // This will now use the value set in Awake from the inspector
        UpdateFloorVisibility(_currentVisibleFloor, true); // Initial setup
    }

    // Renamed to avoid conflict and added an initialSetup flag
    public void UpdateFloorVisibility(FloorLevel targetFloor, bool isInitialSetup = false)
    {
        if (!isInitialSetup && _currentVisibleFloor == targetFloor)
        {
            // Debug.Log($"Player already on floor: {targetFloor}. No change needed for floor objects.");
            // Still notify NPCs in case an NPC changed floor while player was on current floor
            NotifyAllNpcsOfFloorChange(targetFloor);
            return;
        }

        Debug.Log($"Switching visible floor to: {targetFloor}");
        _currentVisibleFloor = targetFloor;

        SetFloorActiveState(lowerFloorParent, targetFloor == FloorLevel.Lower);
        SetFloorActiveState(firstFloorParent, targetFloor == FloorLevel.First);
        SetFloorActiveState(secondFloorParent, targetFloor == FloorLevel.Second);

        // Notify all NPCs about the player's new floor
        NotifyAllNpcsOfFloorChange(targetFloor);
    }

    private void SetFloorActiveState(GameObject floorParent, bool isEnabled)
    {
        if (floorParent == null)
        {
            Debug.LogWarning("Floor parent GameObject is not assigned.");
            return;
        }

        Renderer[] renderers = floorParent.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer rend in renderers)
        {
            rend.enabled = isEnabled;
        }

        Collider[] colliders = floorParent.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = isEnabled;
        }
        // Debug.Log($"{floorParent.name}: Set renderers & colliders to enabled={isEnabled}");
    }


    private void NotifyAllNpcsOfFloorChange(FloorLevel playerFloor)
    {
        // FindObjectsOfType can be a bit slow if you have many NPCs and call this very frequently.
        // For a more optimized solution, consider a registration system where NPCs register/unregister.
        BaseNPC[] allNpcs = FindObjectsOfType<BaseNPC>();
        // Debug.Log($"Notifying {allNpcs.Length} NPCs of floor change to: {playerFloor}");
        foreach (BaseNPC npc in allNpcs)
        {
            npc.UpdateVisibilityBasedOnPlayerFloor(playerFloor);
        }
    }

    public void PlayerChangedFloor(int floorIndex)
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

    // --- Optional Caching (Example if you uncomment) ---
    /*
    void CacheFloorComponents()
    {
        lowerFloorRenderers = GetAllComponentsInChildren<Renderer>(lowerFloorParent);
        lowerFloorColliders = GetAllComponentsInChildren<Collider>(lowerFloorParent);
        // ... for other floors ...
        Debug.Log($"Cached components for floors.");
    }

    List<T> GetAllComponentsInChildren<T>(GameObject parent) where T : Component
    {
        if (parent == null) return new List<T>();
        return new List<T>(parent.GetComponentsInChildren<T>(true));
    }

    // If using cached lists, SetFloorActiveState would become:
    private void SetFloorComponentsEnabled(List<Renderer> renderers, List<Collider> colliders, bool isEnabled)
    {
        foreach (Renderer rend in renderers) rend.enabled = isEnabled;
        foreach (Collider col in colliders) col.enabled = isEnabled;
    }
    // And UpdateFloorVisibility would call SetFloorComponentsEnabled with the cached lists.
    */
}