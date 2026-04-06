// Place at: Assets/_Scripts/UI/Debug/DebugMenuUI.cs
// Attach to your persistent managers GameObject (same one as WorldDataManager etc.)

using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using Game.Gameplay;

public class DebugMenuUI : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Config
    // -------------------------------------------------------------------------

    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F7;

    [Header("Window")]
    [SerializeField] private float windowWidth  = 420f;
    [SerializeField] private float windowHeight = 520f;
    [SerializeField] private float minWidth  = 300f;
    [SerializeField] private float minHeight = 200f;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private bool   _visible;
    private int    _activeTab;
    private Rect   _windowRect;

    private Vector2 _personalityScroll;
    private Vector2 _relationshipScroll;
    private Vector2 _flagsScroll;

    // Resize
    private bool    _resizing;
    private Vector2 _resizeMouseStart;
    private Vector2 _resizeSizeStart;
    private const float ResizeGripSize = 16f;

    private static readonly string[] TabNames = { "Personality", "Relationships", "Flags" };

    // -------------------------------------------------------------------------
    // Styles
    // -------------------------------------------------------------------------

    private GUIStyle _windowStyle;
    private GUIStyle _tabActiveStyle;
    private GUIStyle _tabInactiveStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _subtleStyle;
    private GUIStyle _barBgStyle;
    private GUIStyle _barFillStyle;
    private GUIStyle _gripStyle;
    private bool     _stylesBuilt;

    // -------------------------------------------------------------------------
    // Colors
    // -------------------------------------------------------------------------

    private static readonly Color BackgroundColor  = new Color(0.08f, 0.08f, 0.10f, 0.96f);
    private static readonly Color PanelColor       = new Color(0.13f, 0.13f, 0.16f, 1f);
    private static readonly Color AccentColor      = new Color(0.40f, 0.75f, 0.90f, 1f);
    private static readonly Color PositiveColor    = new Color(0.40f, 0.90f, 0.55f, 1f);
    private static readonly Color NegativeColor    = new Color(0.90f, 0.45f, 0.40f, 1f);
    private static readonly Color NeutralColor     = new Color(0.70f, 0.70f, 0.75f, 1f);
    private static readonly Color BarBgColor       = new Color(0.20f, 0.20f, 0.24f, 1f);

    // -------------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------------

    private void Awake()
    {
        float x = (Screen.width  - windowWidth)  * 0.5f;
        float y = (Screen.height - windowHeight) * 0.5f;
        _windowRect = new Rect(x, y, windowWidth, windowHeight);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible) return;

        BuildStyles();

        HandleResize();

        _windowRect = GUI.Window(9901, _windowRect, DrawWindow, "", _windowStyle);

        // Clamp to screen
        _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width  - _windowRect.width);
        _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
    }

    // -------------------------------------------------------------------------
    // Resize
    // -------------------------------------------------------------------------

    private void HandleResize()
    {
        var gripScreen = new Rect(
            _windowRect.x + _windowRect.width  - ResizeGripSize,
            _windowRect.y + _windowRect.height - ResizeGripSize,
            ResizeGripSize, ResizeGripSize);

        var e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && gripScreen.Contains(e.mousePosition))
        {
            _resizing         = true;
            _resizeMouseStart = e.mousePosition;
            _resizeSizeStart  = new Vector2(_windowRect.width, _windowRect.height);
            e.Use();
        }

        if (_resizing)
        {
            if (e.type == EventType.MouseDrag || e.type == EventType.MouseMove)
            {
                Vector2 delta      = e.mousePosition - _resizeMouseStart;
                _windowRect.width  = Mathf.Max(minWidth,  _resizeSizeStart.x + delta.x);
                _windowRect.height = Mathf.Max(minHeight, _resizeSizeStart.y + delta.y);
                e.Use();
            }

            if (e.type == EventType.MouseUp)
            {
                _resizing = false;
                e.Use();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Window
    // -------------------------------------------------------------------------

    private void DrawWindow(int id)
    {
        float w = _windowRect.width;
        float h = _windowRect.height;

        // Title bar
        GUI.color = AccentColor;
        GUI.Label(new Rect(12, 8, w - 60, 22), "DEBUG MENU", _headerStyle);
        GUI.color = Color.white;

        // Close button
        GUI.color = NegativeColor;
        if (GUI.Button(new Rect(w - 32, 6, 24, 20), "✕", _subtleStyle))
            _visible = false;
        GUI.color = Color.white;

        GUI.DragWindow(new Rect(0, 0, w - 40, 30));

        // Tabs
        float tabW = w / TabNames.Length;
        for (int i = 0; i < TabNames.Length; i++)
        {
            var style = i == _activeTab ? _tabActiveStyle : _tabInactiveStyle;
            if (GUI.Button(new Rect(i * tabW, 32f, tabW, 28), TabNames[i], style))
                _activeTab = i;
        }

        // Content background
        DrawTabBackground(new Rect(0, 64, w, h - 64));

        // Scroll area — leave bottom strip for resize grip
        GUILayout.BeginArea(new Rect(8, 68, w - 16, h - 76 - ResizeGripSize));

        switch (_activeTab)
        {
            case 0: DrawPersonalityTab();   break;
            case 1: DrawRelationshipsTab(); break;
            case 2: DrawFlagsTab();         break;
        }

        GUILayout.EndArea();

        // Resize grip indicator
        GUI.color = new Color(1f, 1f, 1f, 0.20f);
        GUI.Label(new Rect(w - ResizeGripSize, h - ResizeGripSize, ResizeGripSize, ResizeGripSize), "◢", _gripStyle);
        GUI.color = Color.white;
    }

    // -------------------------------------------------------------------------
    // Tab: Personality
    // -------------------------------------------------------------------------

    private void DrawPersonalityTab()
    {
        _personalityScroll = GUILayout.BeginScrollView(_personalityScroll);

        if (PersonalityManager.Instance == null)
        {
            DrawMissingManager("PersonalityManager");
        }
        else
        {
            DrawSectionHeader("PERSONALITY AXES");
            GUILayout.Space(6);

            DrawAxis("Order  (Chaotic / Lawful)",       PersonalityManager.Instance.orderAxis,      -100f, 100f);
            GUILayout.Space(4);
            DrawAxis("Diligence  (Slacker / Academic)", PersonalityManager.Instance.diligenceAxis,  -100f, 100f);
            GUILayout.Space(4);
            DrawAxis("Social  (Sigma / Romantic)",      PersonalityManager.Instance.socialAxis,     -100f, 100f);
        }

        GUILayout.EndScrollView();
    }

    // -------------------------------------------------------------------------
    // Tab: Relationships
    // -------------------------------------------------------------------------

    private void DrawRelationshipsTab()
    {
        _relationshipScroll = GUILayout.BeginScrollView(_relationshipScroll);

        if (RelationshipManager.Instance == null)
        {
            DrawMissingManager("RelationshipManager");
        }
        else
        {
            var all = RelationshipManager.Instance.GetAllRelationships();

            if (all.Count == 0)
            {
                GUI.color = NeutralColor;
                GUILayout.Label("No relationships tracked yet.", _labelStyle);
                GUI.color = Color.white;
            }
            else
            {
                DrawSectionHeader("NPC RELATIONSHIPS");
                GUILayout.Space(6);

                foreach (var kvp in all)
                {
                    string npcId = kvp.Key;
                    float  value = kvp.Value;
                    var    stage = RelationshipManager.ValueToStage(value);

                    GUILayout.BeginHorizontal();
                    GUI.color = AccentColor;
                    GUILayout.Label(npcId, _labelStyle, GUILayout.Width(110));
                    GUI.color = StageColor(stage);
                    GUILayout.Label(stage.ToString(), _subtleStyle, GUILayout.Width(120));
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();

                    DrawAxis("", value, 0f, 100f, showNumbers: true);
                    GUILayout.Space(6);
                }
            }
        }

        GUILayout.EndScrollView();
    }

    // -------------------------------------------------------------------------
    // Tab: Flags
    // -------------------------------------------------------------------------

    private void DrawFlagsTab()
    {
        _flagsScroll = GUILayout.BeginScrollView(_flagsScroll);

        if (WorldDataManager.Instance == null)
        {
            DrawMissingManager("WorldDataManager");
        }
        else
        {
            var flags = WorldDataManager.Instance.GetAllFlags();

            DrawSectionHeader($"WORLD FLAGS ({flags.Count})");
            GUILayout.Space(4);

            if (flags.Count == 0)
            {
                GUI.color = NeutralColor;
                GUILayout.Label("No flags set.", _labelStyle);
                GUI.color = Color.white;
            }
            else
            {
                foreach (var kvp in flags)
                {
                    bool isTrue = kvp.Value;

                    GUILayout.BeginHorizontal();
                    GUI.color = isTrue ? PositiveColor : NegativeColor;
                    GUILayout.Label(isTrue ? "●" : "○", _labelStyle, GUILayout.Width(20));
                    GUI.color = isTrue ? Color.white : NeutralColor;
                    GUILayout.Label(kvp.Key, _labelStyle);
                    GUI.color = isTrue ? PositiveColor : NegativeColor;
                    GUILayout.Label(isTrue ? "TRUE" : "FALSE", _subtleStyle, GUILayout.Width(50));
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }

            var completed = WorldDataManager.Instance.GetCompletedDialogues();
            if (completed.Count > 0)
            {
                GUILayout.Space(10);
                DrawSectionHeader($"COMPLETED DIALOGUES ({completed.Count})");
                GUILayout.Space(4);

                foreach (var entryId in completed)
                {
                    GUILayout.BeginHorizontal();
                    GUI.color = PositiveColor;
                    GUILayout.Label("✓", _labelStyle, GUILayout.Width(20));
                    GUI.color = NeutralColor;
                    GUILayout.Label(entryId, _labelStyle);
                    GUI.color = Color.white;
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }
        }

        GUILayout.EndScrollView();
    }

    // -------------------------------------------------------------------------
    // Drawing helpers
    // -------------------------------------------------------------------------

    private void DrawAxis(string label, float value, float min, float max, bool showNumbers = false)
    {
        float t    = Mathf.InverseLerp(min, max, value);
        float midT = Mathf.InverseLerp(min, max, 0f);
        bool  hasLabel = !string.IsNullOrEmpty(label);

        if (hasLabel)
        {
            GUILayout.BeginHorizontal();
            GUI.color = NeutralColor;
            GUILayout.Label(label, _labelStyle, GUILayout.Width(200));
        }

        GUI.color = BarValueColor(value, min, max);
        GUILayout.Label(value.ToString("F0"), _subtleStyle, GUILayout.Width(36));
        GUI.color = Color.white;

        if (hasLabel) GUILayout.EndHorizontal();

        Rect barArea = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));
        GUI.color = BarBgColor;
        GUI.Box(barArea, "", _barBgStyle);

        float barLeft  = barArea.x + barArea.width * Mathf.Min(midT, t);
        float barWidth = barArea.width * Mathf.Abs(t - midT);

        GUI.color = BarValueColor(value, min, max);
        GUI.Box(new Rect(barLeft, barArea.y + 1, barWidth, barArea.height - 2), "", _barFillStyle);

        float tickX = barArea.x + barArea.width * midT;
        GUI.color = new Color(1, 1, 1, 0.3f);
        GUI.DrawTexture(new Rect(tickX - 0.5f, barArea.y, 1, barArea.height), Texture2D.whiteTexture);

        GUI.color = Color.white;
    }

    private void DrawSectionHeader(string text)
    {
        GUI.color = AccentColor;
        GUILayout.Label(text, _headerStyle);
        GUI.color = new Color(1, 1, 1, 0.15f);
        GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
        GUI.color = Color.white;
    }

    private void DrawMissingManager(string name)
    {
        GUI.color = NegativeColor;
        GUILayout.Label($"{name} not found in scene.", _labelStyle);
        GUI.color = Color.white;
    }

    private void DrawTabBackground(Rect rect)
    {
        var prev = GUI.color;
        GUI.color = PanelColor;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    // -------------------------------------------------------------------------
    // Color helpers
    // -------------------------------------------------------------------------

    private static Color BarValueColor(float value, float min, float max)
    {
        float mid = (min + max) * 0.5f;
        if (Mathf.Abs(value - mid) < 0.01f) return NeutralColor;
        if (min >= 0f) return value > mid ? PositiveColor : NeutralColor;
        return value > 0f ? PositiveColor : NegativeColor;
    }

    private static Color StageColor(RelationshipStage stage) => stage switch
    {
        RelationshipStage.Stranger         => NeutralColor,
        RelationshipStage.Acquaintance     => new Color(0.80f, 0.80f, 0.50f, 1f),
        RelationshipStage.Friend           => new Color(0.50f, 0.85f, 0.65f, 1f),
        RelationshipStage.RomanticInterest => new Color(0.90f, 0.65f, 0.75f, 1f),
        RelationshipStage.Dating           => new Color(0.95f, 0.45f, 0.65f, 1f),
        _                                  => NeutralColor
    };

    // -------------------------------------------------------------------------
    // Style builder
    // -------------------------------------------------------------------------

    private void BuildStyles()
    {
        if (_stylesBuilt) return;
        _stylesBuilt = true;

        _windowStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(0, 0, 0, 0),
            margin  = new RectOffset(0, 0, 0, 0),
            normal  = { background = MakeTex(2, 2, BackgroundColor) }
        };

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = AccentColor }
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal   = { textColor = Color.white }
        };

        _subtleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal   = { textColor = NeutralColor }
        };

        _tabActiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            normal    = { background = MakeTex(2, 2, AccentColor * 0.3f), textColor = AccentColor },
            hover     = { background = MakeTex(2, 2, AccentColor * 0.35f), textColor = AccentColor }
        };

        _tabInactiveStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            normal   = { background = MakeTex(2, 2, PanelColor), textColor = NeutralColor },
            hover    = { background = MakeTex(2, 2, new Color(0.20f, 0.20f, 0.24f, 1f)), textColor = Color.white }
        };

        _barBgStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = MakeTex(2, 2, BarBgColor) },
            padding = new RectOffset(0, 0, 0, 0),
            margin  = new RectOffset(0, 0, 0, 0)
        };

        _barFillStyle = new GUIStyle(GUI.skin.box)
        {
            normal  = { background = Texture2D.whiteTexture },
            padding = new RectOffset(0, 0, 0, 0),
            margin  = new RectOffset(0, 0, 0, 0)
        };

        _gripStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 14,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = NeutralColor }
        };
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var tex    = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}