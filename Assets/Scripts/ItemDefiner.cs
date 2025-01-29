using UnityEngine;

public class ItemDefiner : MonoBehaviour
{
    public CreateInventoryItem item; // Assign in Inspector

    public void Pickup() // Change this if you use another input method
    {
        Inventory.Instance.AddItem(item);
        Destroy(gameObject); // Remove the item from the scene
    }

}
