using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    public class PlayerHoldController : MonoBehaviour
    {
        [Header("References")]
        public Transform holdSocket;
        public Animator animator;

        // The "Nose" collider that stops us from walking into walls
        private BoxCollider _bumperCollider;
        
        private GameObject _currentHeldObject;
        
        // --- CHANGED: Track the Instance, not just the ID ---
        private InventoryItemInstance _currentHeldInstance;
        private string _currentHeldItemID; // Kept for quick access
        private ItemDefinition _currentHeldItemData; 

        private PlayerDragController _dragController;

        void Awake()
        {
            _bumperCollider = gameObject.AddComponent<BoxCollider>();
            _bumperCollider.isTrigger = false; 
            _bumperCollider.enabled = false;
            _dragController = GetComponent<PlayerDragController>();
        }

        // --- NEW: The main entry point from InventorySlotContext ---
        public void HoldItemInstance(InventoryItemInstance instance)
        {
            if (instance == null) { DropItem(); return; }

            // 1. Stop Dragging if active
            if (_dragController != null && _dragController.IsDragging)
                _dragController.StopDrag();

            // 2. Get Data
            ItemDefinition itemData = ItemDatabase.Instance.GetItemByID(instance.itemID);
            if (itemData == null || itemData.worldPrefab == null) return;

            // 3. Clear previous
            DropItem();

            // 4. Save References
            _currentHeldInstance = instance;
            _currentHeldItemID = instance.itemID;
            _currentHeldItemData = itemData;

            // 5. Spawn Visuals
            _currentHeldObject = Instantiate(itemData.worldPrefab, holdSocket);
            
            // 6. Strip Physics (so the held item doesn't fall out of hand)
            Rigidbody rb = _currentHeldObject.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            Collider col = _currentHeldObject.GetComponent<Collider>();
            if (col != null) col.isTrigger = true; 

            // 7. Layer setup
            SetLayerRecursively(_currentHeldObject, LayerMask.NameToLayer("Ignore Raycast"));

            // 8. Offsets
            _currentHeldObject.transform.localPosition = itemData.holdPositionOffset;
            _currentHeldObject.transform.localRotation = Quaternion.Euler(itemData.holdRotationOffset);

            // --- CRITICAL: Apply Saved State to Visuals ---
            // This turns on the Water mesh if the item is full
            SmartObject smartObj = _currentHeldObject.GetComponent<SmartObject>();
            if (smartObj != null)
            {
                smartObj.ImportStates(instance.savedStates);
                // Note: SmartObjectReactor usually updates in Start(). 
                // Since we just Instantiated, Start() hasn't run yet, so this data will be ready for it.
            }
            // ----------------------------------------------

            // 9. Animation & Physics Bumper
            if (animator != null) animator.SetBool("IsHolding", true);

            if (itemData.useBumperCollider)
            {
                Bounds visualBounds = GetCombinedBounds(_currentHeldObject);
                _bumperCollider.center = transform.InverseTransformPoint(visualBounds.center);
                _bumperCollider.size = visualBounds.size;
                _bumperCollider.enabled = true;
            }
        }

        // Compatibility wrapper for old calls
        public void HoldItem(string itemID)
        {
             // If we call this, we are creating a "Phantom" item that isn't in inventory logic.
             // Ideally, find the item in inventory first.
             // For now, we create a fake instance to keep logic consistent.
             var fakeInstance = new InventoryItemInstance { itemID = itemID };
             HoldItemInstance(fakeInstance);
        }

        public void DropItem()
        {
            if (_currentHeldObject != null)
            {
                Destroy(_currentHeldObject);
                _currentHeldObject = null;
            }
            if (animator != null) animator.SetBool("IsHolding", false);
            
            _currentHeldItemID = null;
            _currentHeldItemData = null;
            _currentHeldInstance = null; // Clear instance

            if (_bumperCollider != null) _bumperCollider.enabled = false;
        }
        
        // Called when you press "Q" (or Drop Button) while holding something
        public void DropHeldItemToWorld()
        {
             // Redirect to the robust logic
             if (_currentHeldInstance != null)
             {
                 DropSpecificItemInstance(_currentHeldInstance);
                 // DropItem() is called inside DropSpecificItemInstance via the removal check, 
                 // but let's call it here to be safe and clear the hand immediately.
                 DropItem(); 
             }
             else if (!string.IsNullOrEmpty(_currentHeldItemID))
             {
                 // Fallback if we are holding a "Phantom" item
                 DropSpecificItem(_currentHeldItemID);
             }
        }

        // Logic to spawn the item into the world and remove from inventory
        public void DropSpecificItemInstance(InventoryItemInstance instance)
        {
            ItemDefinition def = ItemDatabase.Instance.GetItemByID(instance.itemID);
            if (def == null) return;

            // 1. Spawn in front of player
            Vector3 dropPos = transform.position + transform.forward * 1.5f + Vector3.up;
            GameObject spawnedObj = Instantiate(def.worldPrefab, dropPos, Quaternion.identity);

            // 2. Setup Physics
            Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();
            if (rb == null) rb = spawnedObj.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse);

            SetLayerRecursively(spawnedObj, LayerMask.NameToLayer("Interactable"));

            // 3. Restore State (Full/Empty/Hot)
            SmartObject smartObj = spawnedObj.GetComponent<SmartObject>();
            if (smartObj != null)
            {
                smartObj.ImportStates(instance.savedStates);
            }
        
            // 4. Link ItemInstance for future Pickup
            ItemInstance itemComp = spawnedObj.GetComponent<ItemInstance>();
            if (itemComp == null) itemComp = spawnedObj.AddComponent<ItemInstance>();
            itemComp.itemData = def;

            // 5. Remove from Inventory Data
            if (Inventory.Instance != null) 
                Inventory.Instance.RemoveItemInstance(instance);

            // 6. Clear Hand if this was the held item
            if (_currentHeldInstance == instance)
            {
                DropItem();
            }
        }
        
        public void DropSpecificItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return;

            ItemDefinition itemData = ItemDatabase.Instance.GetItemByID(itemID);
            if (itemData == null || itemData.worldPrefab == null) return;

            // 1. Calculate Spawn Position
            Vector3 spawnPos = transform.position + (transform.forward * 0.5f) + (Vector3.up * 1.2f);
            Quaternion spawnRot = Quaternion.identity;

            // 2. Instantiate World Object
            GameObject worldObj = Instantiate(itemData.worldPrefab, spawnPos, spawnRot);

            // 3. Setup Physics
            Rigidbody rb = worldObj.GetComponent<Rigidbody>();
            if (rb == null) rb = worldObj.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(transform.forward * 2f, ForceMode.Impulse);

            SetLayerRecursively(worldObj, LayerMask.NameToLayer("Interactable"));

            // 4. Ensure ItemInstance exists (Logic)
            ItemInstance instance = worldObj.GetComponent<ItemInstance>();
            if (instance == null) instance = worldObj.AddComponent<ItemInstance>();
            instance.itemData = itemData;

            // 5. Remove from Inventory
            // (Note: This removes the FIRST matching string ID it finds, since we don't have a unique instance here)
            if (Inventory.Instance != null) 
                Inventory.Instance.RemoveItemByID(itemID);

            // 6. Clear Hand if this was the held item
            if (_currentHeldItemID == itemID)
            {
                DropItem();
            }
        }
        
        // ... (Keep GetCombinedBounds, SetLayerRecursively, GetHeldItemID) ...
        
        private Bounds GetCombinedBounds(GameObject obj)
        {
            Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
        }

        public string GetHeldItemID() => _currentHeldItemID;
        public ItemDefinition GetHeldItemData() => _currentHeldItemData;
        
        public InventoryItemInstance GetHeldItemInstance()
        {
            return _currentHeldInstance;
        }
    }
}