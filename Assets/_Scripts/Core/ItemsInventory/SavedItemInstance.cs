// SavedItemInstance.cs
using UnityEngine;

[System.Serializable]
public class SavedItemInstance
{
    public string itemID; // The ID from the ScriptableObject (e.g., "house_key")
    public Vector3 position;
    public Quaternion rotation;

    public SavedItemInstance(string id, Vector3 pos, Quaternion rot)
    {
        itemID = id;
        position = pos;
        rotation = rot;
    }
}