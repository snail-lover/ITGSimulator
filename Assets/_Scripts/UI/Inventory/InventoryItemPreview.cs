using UnityEngine;

namespace Game.UI
{
    public class InventoryItemPreview : MonoBehaviour
    {
        [Tooltip("How fast the turntable spins. Recommended: 20 to 50.")]
        public float rotateSpeed = 30f;

        private GameObject _spinnerParent; // The invisible object that spins
        private GameObject _currentModel;  // The actual item model

        private void Update()
        {
            // Debug Logic: Check if this script actually controls the spinner
            if (_spinnerParent == null)
            {
                // If this prints, you are looking at a script that didn't create the item.
                // Or the reference was lost.
                // Debug.Log("Script running, but _spinnerParent is NULL."); 
                return; 
            }

            if (rotateSpeed == 0) 
            {
                Debug.LogWarning("Script has Spinner, but Speed is 0!");
                return;
            }

            // Force rotation
            _spinnerParent.transform.Rotate(Vector3.up, rotateSpeed * Time.unscaledDeltaTime, Space.World);
    
            // Prove it is working
            // Debug.Log($"Rotated! Current Y: {_spinnerParent.transform.eulerAngles.y}");
        }

        public void ShowItem(GameObject itemPrefab, Vector3 customRotation, float customScale)
        {
            ClearPreview();

            if (itemPrefab == null) return;
            
            // Safety Check
            if (ItemPreviewStage.Instance == null)
            {
                Debug.LogError("[InventoryItemPreview] Preview Stage missing! Is the 'PreviewStage' object active in the scene?");
                return;
            }

            Transform spawnPoint = ItemPreviewStage.Instance.spawnPoint;

            // 1. Create a NEW "Spinner" parent automatically
            _spinnerParent = new GameObject("Preview_Spinner");
            
            // 2. Align it with the spawn point
            _spinnerParent.transform.SetParent(spawnPoint);
            _spinnerParent.transform.localPosition = Vector3.zero;
            _spinnerParent.transform.localRotation = Quaternion.identity; // Reset rotation so it's flat
            _spinnerParent.transform.localScale = Vector3.one;
            _spinnerParent.layer = spawnPoint.gameObject.layer;

            // 3. Instantiate the actual Item Model inside the Spinner
            _currentModel = Instantiate(itemPrefab, _spinnerParent.transform);

            // 4. Apply YOUR custom adjustments (from ItemDefinition)
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.Euler(customRotation); 
            _currentModel.transform.localScale = Vector3.one * customScale; 

            // 5. Cleanup Physics (Stop falling)
            Rigidbody rb = _currentModel.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true; 
            }

            // 6. Cleanup Colliders
            foreach (var col in _currentModel.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // 7. Ensure layers are correct
            SetLayerRecursively(_currentModel, spawnPoint.gameObject.layer);
        }

        public void ClearPreview()
        {
            if (_spinnerParent != null)
            {
                Destroy(_spinnerParent);
                _spinnerParent = null;
                _currentModel = null;
            }
        }

        private void OnDestroy()
        {
            ClearPreview();
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}