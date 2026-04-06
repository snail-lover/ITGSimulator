using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Game.Core;     
using Game.Gameplay; 
using System.Linq;

namespace Game.UI
{
    public class InventorySlotContext : MonoBehaviour, IPointerClickHandler
    {
        [Header("Data")]
        // CHANGED: We now store the full instance data, not just the ID string.
        public InventoryItemInstance itemInstance; 
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OpenContextForSlot();
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                SelectLogic();
            }
        }

        private void OpenContextForSlot()
        {
            if (itemInstance == null) return;
            string itemID = itemInstance.itemID;

            List<InteractionOption> options = new List<InteractionOption>();

            // --- OPTION 1: SELECT ---
            options.Add(new InteractionOption
            {
                Label = "Select",
                Priority = 10,
                ActionToRun = () => SelectLogic()
            });

            // --- OPTION 2: HOLD ---
            options.Add(new InteractionOption
            {
                Label = "Hold",
                Priority = 5,
                ActionToRun = () =>
                {
                    var holder = FindFirstObjectByType<PlayerHoldController>();
                    // FIX: Pass the Unique Instance, so the holder spawns the correct specific mug
                    if (holder != null) holder.HoldItemInstance(itemInstance);
                    SelectedItemState.Clear(); 
                }
            });

            // --- OPTION 3: DROP ---
            options.Add(new InteractionOption
            {
                Label = "Drop",
                Priority = -5,
                ActionToRun = () =>
                {
                    var holder = FindFirstObjectByType<PlayerHoldController>();
                    // FIX: Drop this SPECIFIC instance
                    if (holder != null) holder.DropSpecificItemInstance(itemInstance);
                    SelectedItemState.Clear();
                }
            });
            
            // ... (Inspect remains mostly the same, just reads from itemID) ...

            string displayName = ItemDatabase.Instance.GetItemName(itemID);
            UIEvents.RequestContextMenu(options, Input.mousePosition, displayName);
        }

        private void SelectLogic()
        {
            if (itemInstance == null) 
            {
                SelectedItemState.Clear();
                return;
            }

            Debug.Log($"[Inventory] Selected: {itemInstance.itemID} ({itemInstance.uniqueID})");
            
            // FIX: Pass both IDs
            SelectedItemState.Set(itemInstance.itemID, itemInstance.uniqueID);
        }
    }
}