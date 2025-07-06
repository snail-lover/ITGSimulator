using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class CreateInventoryItem : ScriptableObject
{
    [Header("Core Data")]
    public string id; // Unique ID like "viktors_camera"
    public string itemName; // Display name like "Viktor's Camera"
    public Sprite icon;

    [Header("Properties & Categories")]
    [Tooltip("What kind of item is this? Can be multiple things.")]
    public ItemTag tags; // Our new tagging field!
}