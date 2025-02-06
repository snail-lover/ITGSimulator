using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class CreateInventoryItem : ScriptableObject
{
    public string id;
    public string itemName;
    public Sprite icon;
}
