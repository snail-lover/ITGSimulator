using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections; 
using System.Collections.Generic;
using Game.Core;      
using Game.Gameplay;
using System.Linq; 

namespace Game.UI
{
    public class InventoryDetailsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject contentParent; 
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Transform tagsContainer;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject tagPrefab;

        [Header("3D Preview")]
        [SerializeField] private InventoryItemPreview itemPreviewer; 

        [Header("State Settings")]
        public string fluidStateID = "Item_FluidID"; 
        public string fillLevelStateID = "Item_IsFull";

        // --- NEW: Track what we are currently showing ---
        private string _lastItemID;
        private string _lastUniqueID;

        private void Start()
        {
            SelectedItemState.OnChanged += TryUpdateDisplay;
            InventoryHoverState.OnChanged += TryUpdateDisplay;
            
            // Optional: Listen to Inventory changes if you want tags to update live while looking at them
            if (Inventory.Instance != null)
                Inventory.Instance.OnInventoryChanged += TryUpdateDisplay;

            TryUpdateDisplay();
        }

        private void OnDestroy()
        {
            SelectedItemState.OnChanged -= TryUpdateDisplay;
            InventoryHoverState.OnChanged -= TryUpdateDisplay;
            
            if (Inventory.Instance != null)
                Inventory.Instance.OnInventoryChanged -= TryUpdateDisplay;
        }

        private void TryUpdateDisplay()
        {
            string idToShow = null;
            string uniqueIdToShow = null;

            // Selection takes priority
            if (!string.IsNullOrEmpty(SelectedItemState.SelectedItemID))
            {
                idToShow = SelectedItemState.SelectedItemID;
                uniqueIdToShow = SelectedItemState.SelectedUniqueID;
            }
            else 
            {
                idToShow = InventoryHoverState.HoveredItemID;
                uniqueIdToShow = InventoryHoverState.HoveredUniqueID;
            }

            UpdateDetails(idToShow, uniqueIdToShow);
        }

        private void UpdateDetails(string itemID, string uniqueID)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                contentParent.SetActive(false);
                if(itemPreviewer != null) itemPreviewer.ClearPreview();
                
                // Clear cache so re-selecting works later
                _lastItemID = null;
                _lastUniqueID = null;
                return;
            }

            ItemDefinition item = ItemDatabase.Instance.GetItemByID(itemID);
            if (item == null) return;

            contentParent.SetActive(true);

            // 1. ALWAYS Update Data (Text & Tags)
            // This ensures that if the state changes (e.g. Empty -> Full), 
            // the tags update immediately without deselecting.
            if(nameText) nameText.text = item.itemName;
            if(descriptionText) descriptionText.text = item.description;
            
            RefreshTags(item, uniqueID); 

            // 2. CONDITIONALLY Update Preview (3D Model)
            // We only ask the previewer to spawn a new model if the selection changed.
            // This prevents the rotation from resetting (the jitter) when just the tags update.
            if (itemID != _lastItemID || uniqueID != _lastUniqueID)
            {
                if (itemPreviewer != null)
                    itemPreviewer.ShowItem(item.worldPrefab, item.previewRotation, item.previewScale);
            }

            // 3. Update Cache
            _lastItemID = itemID;
            _lastUniqueID = uniqueID;
        }

        private void RefreshTags(ItemDefinition itemDef, string specificUniqueID)
        {
            foreach(Transform child in tagsContainer) Destroy(child.gameObject);

            // 1. Static Tags
            Color staticColor = new Color(0.3f, 0.3f, 0.3f, 1f); 
            foreach (var tagDef in itemDef.tags)
            {
                if(tagDef != null) SpawnTag(tagDef.name, staticColor);
            }

            // 2. Dynamic Tags
            InventoryItemInstance instance = null;

            // Try find by Unique ID (Best)
            if (!string.IsNullOrEmpty(specificUniqueID))
            {
                instance = WorldDataManager.Instance.saveData.playerInventory.items
                    .FirstOrDefault(x => x.uniqueID == specificUniqueID);
            }
            
            // Fallback find by Item ID (Okay for hovering unselected items if logic allows)
            if (instance == null)
            {
                instance = WorldDataManager.Instance.saveData.playerInventory.items
                    .FirstOrDefault(x => x.itemID == itemDef.id);
            }

            if (instance != null)
            {
                CheckAndSpawnFluidTag(instance);
                CheckAndSpawnGenericStates(instance);
            }

            if (gameObject.activeInHierarchy) StartCoroutine(ForceLayoutRebuild());
        }

        // ... (Keep CheckAndSpawnFluidTag, CheckAndSpawnGenericStates, SpawnTag, etc.) ...
        
        private void CheckAndSpawnFluidTag(InventoryItemInstance instance)
        {
            var fillState = instance.savedStates.FirstOrDefault(s => s.stateID == fillLevelStateID);
            if (fillState.value == 0) return; 

            var fluidState = instance.savedStates.FirstOrDefault(s => s.stateID == fluidStateID);
            if (fluidState.value > 0)
            {
                FluidDefinition fluid = ItemDatabase.Instance.GetFluidByID(fluidState.value);
                if (fluid != null) SpawnTag($"Contains {fluid.fluidName}", fluid.visualColor);
            }
        }

        private void CheckAndSpawnGenericStates(InventoryItemInstance instance)
        {
            foreach (var savedState in instance.savedStates)
            {
                if (savedState.stateID == fillLevelStateID || savedState.stateID == fluidStateID) continue;

                StateDefinition stateDef = ItemDatabase.Instance.GetStateByName(savedState.stateID);
                if (stateDef != null && stateDef.showInUI && savedState.value == 1)
                {
                    string label = string.IsNullOrEmpty(stateDef.uiLabel) ? stateDef.name : stateDef.uiLabel;
                    SpawnTag(label, stateDef.uiColor);
                }
            }
        }

        private void SpawnTag(string text, Color color)
        {
            GameObject newTagObj = Instantiate(tagPrefab, tagsContainer);
            UI_Tag tagScript = newTagObj.GetComponent<UI_Tag>();
            if (color.a == 0) color.a = 1f;
            if (tagScript != null) tagScript.Setup(text, color);
        }

        private IEnumerator ForceLayoutRebuild()
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tagsContainer.GetComponent<RectTransform>());
        }
    }
}