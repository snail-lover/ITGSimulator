// --- START OF FILE NpcDebugMenu.cs ---

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// An IMGUI-based debug menu for monitoring NPC needs and other systems.
/// Press the Backquote (`) key to toggle the menu.
/// Add this script to a single persistent GameObject in your scene.
/// </summary>
public class NpcDebugMenu : MonoBehaviour
{
    // Menu state
    private bool isMenuOpen = false;
    private int currentTab = 0;
    private string[] tabNames = { "NPC Status", "System Info" }; // Renamed tab for clarity

    // Window properties
    private Rect windowRect = new Rect(20, 20, 450, 500);
    private Vector2 scrollPosition;

    // Styling
    private GUIStyle boldLabelStyle;
    private GUIStyle subLabelStyle;

    private void Update()
    {
        // Toggle the menu with the backquote key
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isMenuOpen = !isMenuOpen;
        }
    }

    private void OnGUI()
    {
        if (!isMenuOpen)
        {
            return;
        }

        // Initialize styles on first run
        if (boldLabelStyle == null)
        {
            boldLabelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            subLabelStyle = new GUIStyle(GUI.skin.label) { padding = new RectOffset(15, 0, 0, 0) }; // Indented style
        }

        windowRect = GUILayout.Window(0, windowRect, DrawWindowContent, "NPC Debug Menu");
    }

    private void DrawWindowContent(int windowID)
    {
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        switch (currentTab)
        {
            case 0:
                DrawNpcStatusTab(); // Renamed method
                break;
            case 1:
                DrawSystemInfoTab();
                break;
        }
    }

    /// <summary>
    /// Draws the content for the "NPC Status" tab, including needs and known activities.
    /// </summary>
    private void DrawNpcStatusTab()
    {
        GUILayout.Label("Live Status of all Active NPCs", boldLabelStyle);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        if (NpcController.AllActiveNpcs == null || NpcController.AllActiveNpcs.Count == 0)
        {
            GUILayout.Label("No active NPCs in the scene.");
        }
        else
        {
            foreach (var npc in NpcController.AllActiveNpcs)
            {
                if (npc == null || npc.runtimeData == null) continue;

                // --- Draw NPC Header ---
                string npcStatus = $"--- {npc.npcConfig.npcName} (State: {npc.CurrentSuperState}) ---";
                GUILayout.Space(10);
                GUILayout.Label(npcStatus, boldLabelStyle);

                // --- Draw Needs Section ---
                if (npc.runtimeData.needs == null || npc.runtimeData.needs.Count == 0)
                {
                    GUILayout.Label("This NPC has no needs defined.", subLabelStyle);
                }
                else
                {
                    foreach (var need in npc.runtimeData.needs.Values)
                    {
                        DrawNeedBar(need);
                    }
                }

                // --- NEW: Draw Remembered Locations Section ---
                GUILayout.Space(5);
                GUILayout.Label("Remembered Locations:", subLabelStyle);
                if (npc.runtimeData.rememberedActivityLocations.Count == 0)
                {
                    GUILayout.Label("  - No memories yet.", subLabelStyle);
                }
                else
                {
                    foreach (var memory in npc.runtimeData.rememberedActivityLocations)
                    {
                        string id = memory.Key;
                        Vector3 pos = memory.Value.position;
                        float timeAgo = Time.time - memory.Value.lastUpdatedTime;
                        string memoryText = $"  - {id} at {pos.ToString("F1")} ({timeAgo:F1}s ago)";
                        GUILayout.Label(memoryText, subLabelStyle);
                    }
                }

                // --- NEW: Draw Learned Zone Contents Section ---
                GUILayout.Space(5);
                GUILayout.Label("Learned Zone Contents:", subLabelStyle);
                if (npc.runtimeData.learnedZoneContents.Count == 0)
                {
                    GUILayout.Label("  - No zone knowledge yet.", subLabelStyle);
                }
                else
                {
                    foreach (var zoneMemory in npc.runtimeData.learnedZoneContents)
                    {
                        string zoneName = zoneMemory.Key;
                        string contents = string.Join(", ", zoneMemory.Value);
                        string zoneText = $"  - {zoneName}: [{contents}]";
                        GUILayout.Label(zoneText, subLabelStyle);
                    }
                }
            }
        }

        GUILayout.EndScrollView();
    }

    /// <summary>
    /// Draws a single need with a label, a progress bar, and a numeric value.
    /// </summary>
    private void DrawNeedBar(NpcNeed need)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(15); // Indent the need bars

        GUILayout.Label(need.name, GUILayout.Width(70));
        GUILayout.Label($"{need.currentValue:F1} / 100", GUILayout.Width(80));

        Rect barRect = GUILayoutUtility.GetRect(100, 20, GUILayout.ExpandWidth(true));
        GUI.color = Color.grey;
        GUI.DrawTexture(barRect, Texture2D.whiteTexture);

        float fillPercentage = need.currentValue / 100f;
        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * fillPercentage, barRect.height);

        GUI.color = Color.Lerp(Color.green, Color.red, fillPercentage);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the content for the placeholder "System Info" tab.
    /// </summary>
    private void DrawSystemInfoTab()
    {
        GUILayout.Label("General System Information", boldLabelStyle);
        GUILayout.Space(10);

        if (ActivityManager.Instance != null)
        {
            int activityCount = 0;
            if (ActivityManager.Instance.AllRegisteredActivities != null)
            {
                foreach (var activity in ActivityManager.Instance.AllRegisteredActivities)
                {
                    activityCount++;
                }
            }
            GUILayout.Label($"Registered Activities: {activityCount}");
        }
        else
        {
            GUILayout.Label("ActivityManager not found.");
        }

        if (NpcController.AllActiveNpcs != null)
        {
            GUILayout.Label($"Active NPCs: {NpcController.AllActiveNpcs.Count}");
        }
    }
}