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
    [Tooltip("The floor that should be visible for a NEW game.")]
    public FloorLevel startingFloor = FloorLevel.Lower;

    public enum FloorLevel
    {
        Lower = 0,
        First = 1,
        Second = 2
    }

    private FloorLevel _currentVisibleFloor;
    public FloorLevel CurrentVisibleFloor => _currentVisibleFloor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple FloorVisibilityManager instances found. Destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // We only need to subscribe to saving. Loading state is handled in Start().
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave += SaveFloorState;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks.
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave -= SaveFloorState;
        }
    }

    void Start()
    {
        // If the WorldDataManager doesn't exist, we can't do anything smart.
        if (WorldDataManager.Instance == null)
        {
            Debug.LogWarning("[FloorVisibilityManager] WorldDataManager not found. Using default starting floor.");
            UpdateFloorVisibility(startingFloor, true);
            return;
        }

        FloorLevel floorToSet;

        // The WorldDataManager's 'isNewGame' property is now the reliable source of truth.
        if (!WorldDataManager.Instance.isNewGame)
        {
            // A game has been loaded. Use the saved data.
            int savedIndex = WorldDataManager.Instance.saveData.playerState.currentFloorIndex;

            // Add a safety check to ensure the saved data is valid.
            if (System.Enum.IsDefined(typeof(FloorLevel), savedIndex))
            {
                floorToSet = (FloorLevel)savedIndex;
                Debug.Log($"[FloorVisibilityManager] Continuing a saved game. Setting floor to loaded value: {floorToSet}");
            }
            else
            {
                Debug.LogError($"[FloorVisibilityManager] Invalid floor index '{savedIndex}' found in save data. Falling back to default.");
                floorToSet = startingFloor;
            }
        }
        else
        {
            // This is a new game. Use the inspector default.
            floorToSet = startingFloor;
            Debug.Log($"[FloorVisibilityManager] Starting a new game. Setting floor to default: {floorToSet}");
        }

        // Finally, apply the determined floor visibility.
        UpdateFloorVisibility(floorToSet, true);
    }

    private void SaveFloorState()
    {
        if (WorldDataManager.Instance == null) return;

        WorldDataManager.Instance.saveData.playerState.currentFloorIndex = (int)_currentVisibleFloor;
        Debug.Log($"[FloorVisibilityManager] Saved current floor state: {_currentVisibleFloor} (Index: {(int)_currentVisibleFloor})");
    }

    public void UpdateFloorVisibility(FloorLevel targetFloor, bool isInitialSetup = false)
    {
        if (!isInitialSetup && _currentVisibleFloor == targetFloor)
        {
            // We still notify NPCs even if the floor isn't changing, just in case they need an update.
            NotifyAllNpcsOfFloorChange(targetFloor);
            return;
        }

        Debug.Log($"Switching visible floor to: {targetFloor}");
        _currentVisibleFloor = targetFloor;

        SetFloorActiveState(lowerFloorParent, targetFloor == FloorLevel.Lower);
        SetFloorActiveState(firstFloorParent, targetFloor == FloorLevel.First);
        SetFloorActiveState(secondFloorParent, targetFloor == FloorLevel.Second);

        NotifyAllNpcsOfFloorChange(targetFloor);
    }

    private void SetFloorActiveState(GameObject floorParent, bool isEnabled)
    {
        if (floorParent == null)
        {
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
    }

    private void NotifyAllNpcsOfFloorChange(FloorLevel playerFloor)
    {
        NpcController[] allNpcs = FindObjectsByType<NpcController>(FindObjectsSortMode.None);
        foreach (NpcController npc in allNpcs)
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
}