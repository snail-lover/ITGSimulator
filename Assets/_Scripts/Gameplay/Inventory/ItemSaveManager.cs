using UnityEngine;
using System.Collections.Generic;
using Game.Core;     // <-- Added for ItemDefinition
using Game.Gameplay; // <-- Added for ItemInstance and ItemDatabase

public class ItemSaveManager : MonoBehaviour
{
    void OnEnable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave += CaptureSceneItemState;
            WorldDataManager.Instance.OnAfterLoad += RestoreSceneItemState;
        }
    }

    void OnDisable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave -= CaptureSceneItemState;
            WorldDataManager.Instance.OnAfterLoad -= RestoreSceneItemState;
        }
    }

    private void CaptureSceneItemState()
    {
        var saveData = WorldDataManager.Instance.saveData;
        saveData.sceneItemInstances.Clear(); // Start with a fresh list

        // Find every single ItemInstance in the currently active scene.
        ItemInstance[] allItemInstances = FindObjectsByType<ItemInstance>(FindObjectsSortMode.None);

        Debug.Log($"[ItemSaveManager] Found {allItemInstances.Length} items to save.");

        foreach (var instance in allItemInstances)
        {
            // We assume itemData is assigned. If it's null, we shouldn't save it (or it will break loading).
            if (instance.itemData != null)
            {
                var dataToSave = new SavedItemInstance(
                    instance.itemData.id,
                    instance.transform.position,
                    instance.transform.rotation
                );
                saveData.sceneItemInstances.Add(dataToSave);
            }
        }
    }

    private void RestoreSceneItemState()
    {
        // First, destroy any items that might already be in the scene to prevent duplicates.
        ItemInstance[] existingInstances = FindObjectsByType<ItemInstance>(FindObjectsSortMode.None);
        foreach (var instance in existingInstances)
        {
            Destroy(instance.gameObject);
        }

        var itemInstancesToLoad = WorldDataManager.Instance.saveData.sceneItemInstances;
        Debug.Log($"[ItemSaveManager] Loading {itemInstancesToLoad.Count} items from save data.");

        foreach (var savedInstance in itemInstancesToLoad)
        {
            // CHANGED: CreateInventoryItem -> ItemDefinition
            ItemDefinition itemData = ItemDatabase.Instance.GetItemByID(savedInstance.itemID);

            if (itemData != null && itemData.worldPrefab != null)
            {
                // Instantiate the clean prefab and place it correctly
                GameObject newInstance = Instantiate(
                    itemData.worldPrefab,
                    savedInstance.position,
                    savedInstance.rotation
                );

                // Add and configure the ItemInstance component
                // Ensure the prefab doesn't already have one to avoid duplicates
                ItemInstance instanceComponent = newInstance.GetComponent<ItemInstance>();
                if (instanceComponent == null)
                {
                    instanceComponent = newInstance.AddComponent<ItemInstance>();
                }

                instanceComponent.itemData = itemData;
            }
        }
    }
}