// WorldDataDebugger.cs (Updated)

using UnityEngine;
using System.Collections.Generic; // Required for IReadOnlyDictionary

/// <summary>
/// An OnGUI-based debugger that displays the COMPLETE state from the WorldDataManager.
/// Press a key (F1 by default) to toggle the display.
/// The window can be dragged around the screen.
/// </summary>
public class WorldDataDebugger : MonoBehaviour
{
    [Header("Display Controls")]
    [Tooltip("Toggles the visibility of the debug window.")]
    public bool showDebugger = true;

    [Tooltip("The key to press to toggle the debugger window's visibility.")]
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Window Properties")]
    [Tooltip("The screen-space rectangle that defines the debugger window's position and size.")]
    public Rect windowRect = new Rect(20, 20, 400, 550); // x, y, width, height (Increased height slightly)

    private Vector2 scrollPosition;

    private GUIStyle trueStyle;
    private GUIStyle falseStyle;
    private GUIStyle boldStyle;
    private bool stylesInitialized = false;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showDebugger = !showDebugger;
        }
    }

    private void InitializeStyles()
    {
        trueStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = Color.green },
            fontStyle = FontStyle.Bold
        };

        falseStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = new Color(1.0f, 0.5f, 0.5f) } // A light red
        };

        boldStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!showDebugger) return;
        if (!stylesInitialized) InitializeStyles();

        windowRect = GUI.Window(0, windowRect, DrawWindow, "World Data Debugger");
    }

    void DrawWindow(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        if (WorldDataManager.Instance == null)
        {
            GUILayout.Label("WorldDataManager not found.", falseStyle);
            return;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // --- NEW/UPDATED SECTION ---
        GUILayout.Label("--- Player State ---", boldStyle);
        var playerState = WorldDataManager.Instance.saveData.playerState;
        if (playerState != null)
        {
            // Display Floor State
            var floorEnum = (FloorVisibilityManager.FloorLevel)playerState.currentFloorIndex;
            GUILayout.Label($"  - Current Floor: {floorEnum} (Index: {playerState.currentFloorIndex})");

            // Display Position
            GUILayout.Label($"  - Last Position: {playerState.lastKnownPosition}");
        }
        else
        {
            GUILayout.Label("PlayerState not initialized.");
        }
        GUILayout.Space(10);
        // --- END NEW/UPDATED SECTION ---

        GUILayout.Label("--- Global Flags ---", boldStyle);
        var globalFlags = WorldDataManager.Instance.saveData.globalFlags;
        if (globalFlags.Count == 0)
        {
            GUILayout.Label("No global flags set.");
        }
        else
        {
            foreach (KeyValuePair<string, bool> flag in globalFlags)
            {
                GUIStyle currentStyle = flag.Value ? trueStyle : falseStyle;
                GUILayout.Label($"{flag.Key}: {flag.Value}", currentStyle);
            }
        }
        GUILayout.Space(10);

        GUILayout.Label("--- NPC Runtime Data ---", boldStyle);
        var npcStates = WorldDataManager.Instance.saveData.allNpcRuntimeData;
        if (npcStates.Count == 0)
        {
            GUILayout.Label("No NPC data initialized.");
        }
        else
        {
            foreach (KeyValuePair<string, NpcRuntimeData> npcData in npcStates)
            {
                GUILayout.Label(npcData.Key + ":", boldStyle);
                GUILayout.Label($"  - currentLove: {npcData.Value.currentLove}");
            }
        }
        GUILayout.Space(10);

        GUILayout.Label("--- Player Inventory ---", boldStyle);
        if (WorldDataManager.Instance.saveData.playerInventory != null)
        {
            var inventoryIDs = WorldDataManager.Instance.saveData.playerInventory.itemIDs;
            if (inventoryIDs.Count == 0)
            {
                GUILayout.Label("Inventory is empty.");
            }
            else
            {
                foreach (string itemID in inventoryIDs)
                {
                    if (ItemDatabase.Instance != null)
                    {
                        var itemData = ItemDatabase.Instance.GetItemByID(itemID);
                        string displayName = itemData != null ? itemData.itemName : "!!! UNKNOWN ITEM !!!";
                        GUILayout.Label($"  - {displayName} (ID: {itemID})");
                    }
                    else
                    {
                        GUILayout.Label($"  - ID: {itemID}");
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("Inventory system not added to GameSaveData.");
        }

        GUILayout.EndScrollView();
    }
}