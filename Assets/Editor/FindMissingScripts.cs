// FindMissingScripts.cs
using UnityEngine;
using UnityEditor;
using System.Linq;

public class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Project")]
    public static void FindAndLogMissingScripts()
    {
        Debug.Log("Starting search for prefabs with missing scripts...");
        int missingCount = 0;

        // Get the GUIDs of all prefabs in the project
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            // Get all components on the prefab's root and all its children
            Component[] components = prefab.GetComponentsInChildren<Component>(true);

            foreach (Component component in components)
            {
                // A "missing script" is actually a null component reference
                if (component == null)
                {
                    missingCount++;
                    // Log an error with a context object. Clicking this error in the console
                    // will highlight the problematic prefab in your Project window!
                    Debug.LogError($"Missing script found on prefab: '{prefab.name}'", prefab);
                    // We only need to report this prefab once, so we can break out.
                    break;
                }
            }
        }

        Debug.Log($"Search complete. Found {missingCount} prefabs with missing scripts.");
    }
}