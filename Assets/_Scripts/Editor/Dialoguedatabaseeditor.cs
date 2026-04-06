// Place at: Assets/_Scripts/Editor/Dialogue/DialogueDatabaseEditor.cs
// This file must be inside an Editor folder or an Editor-only assembly.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Game.Core;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueDatabase))]
public class DialogueDatabaseEditor : Editor
{
    private bool _importing;
    private string _lastStatus;
    private bool _lastWasError;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Google Sheets Importer", EditorStyles.boldLabel);

        var db = (DialogueDatabase)target;

        EditorGUI.BeginDisabledGroup(_importing);
        if (GUILayout.Button(_importing ? "Importing…" : "Reimport from Google Sheets", GUILayout.Height(30)))
            _ = ImportAsync(db);
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(_lastStatus))
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true
            };
            GUI.color = _lastWasError ? new Color(1f, 0.5f, 0.5f) : new Color(0.6f, 1f, 0.6f);
            EditorGUILayout.LabelField(_lastStatus, style);
            GUI.color = Color.white;
        }
    }

    // -------------------------------------------------------------------------
    // Import
    // -------------------------------------------------------------------------

    private async Task ImportAsync(DialogueDatabase db)
    {
        _importing = true;
        _lastStatus = "Fetching…";
        _lastWasError = false;
        Repaint();

        try
        {
            string url = NormalizeCsvUrl(db.googleSheetCsvUrl);

            if (string.IsNullOrWhiteSpace(url))
            {
                SetStatus("No URL set on DialogueDatabase.", error: true);
                return;
            }

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            string csv = await client.GetStringAsync(url);

            var entries = ParseCsv(csv);
            db.entries = entries;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            SetStatus($"✓ Imported {entries.Count} entries.", error: false);
            Debug.Log($"[DialogueImporter] Imported {entries.Count} entries into {db.name}.");
        }
        catch (Exception e)
        {
            SetStatus($"✗ {e.Message}", error: true);
            Debug.LogError($"[DialogueImporter] {e}");
        }
        finally
        {
            _importing = false;
            Repaint();
        }
    }

    private void SetStatus(string msg, bool error)
    {
        _lastStatus = msg;
        _lastWasError = error;
    }

    // -------------------------------------------------------------------------
    // URL normalization
    // Convert pubhtml → pub?output=csv if the user pasted the wrong variant.
    // -------------------------------------------------------------------------

    private static string NormalizeCsvUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        url = url.Trim();

        if (url.EndsWith("/pubhtml"))
            url = url.Replace("/pubhtml", "/pub?output=csv");
        else if (url.Contains("/pubhtml?"))
            url = url.Substring(0, url.IndexOf("/pubhtml?")) + "/pub?output=csv";

        return url;
    }

    // -------------------------------------------------------------------------
    // CSV parsing
    // -------------------------------------------------------------------------

    private static List<DialogueEntry> ParseCsv(string csv)
    {
        var entries = new List<DialogueEntry>();

        // Normalize line endings
        csv = csv.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = SplitCsvLines(csv);

        if (lines.Count < 2) return entries;

        // Build column index map from header row
        var headers = ParseCsvRow(lines[0]);
        var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Count; i++)
            colMap[headers[i].Trim()] = i;

        for (int row = 1; row < lines.Count; row++)
        {
            string line = lines[row];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvRow(line);

            // Helper lambdas scoped to this row
            string Get(string col)
                => colMap.TryGetValue(col, out int idx) && idx < cols.Count
                    ? cols[idx].Trim()
                    : "";

            float GetFloat(string col, float defaultVal)
                => float.TryParse(Get(col), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : defaultVal;

            int GetInt(string col, int defaultVal)
                => int.TryParse(Get(col), out int v) ? v : defaultVal;

            bool GetBool(string col)
            {
                string s = Get(col).ToLowerInvariant();
                return s is "true" or "1" or "yes" or "y";
            }

            List<string> GetList(string col)
            {
                string s = Get(col);
                if (string.IsNullOrEmpty(s)) return new List<string>();
                var items = new List<string>();
                foreach (var item in s.Split('|'))
                {
                    string trimmed = item.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        items.Add(trimmed);
                }
                return items;
            }

            // Skip rows with no npc_id (blank spacer rows, section headers, etc.)
            string npcId = Get("npc_id");
            if (string.IsNullOrEmpty(npcId)) continue;

            var entry = new DialogueEntry
            {
                npcID                        = npcId,
                entryID                      = Get("entry_id"),
                nodeName                     = Get("node_name"),
                priority                     = GetInt("priority", 0),
                oneTimeOnly                  = GetBool("one_time_only"),

                minRelationship              = GetFloat("min_relationship", -1f),
                maxRelationship              = GetFloat("max_relationship", -1f),
                checkStage                   = GetBool("check_stage"),

                minOrder                     = GetFloat("min_order", -999f),
                maxOrder                     = GetFloat("max_order",  999f),
                minDiligence                 = GetFloat("min_diligence", -999f),
                maxDiligence                 = GetFloat("max_diligence",  999f),
                minSocial                    = GetFloat("min_social", -999f),
                maxSocial                    = GetFloat("max_social",  999f),

                requiredFlags                = GetList("required_flags"),
                blockedFlags                 = GetList("blocked_flags"),
                requiredItems                = GetList("required_items"),
                requiredCompletedDialogues   = GetList("required_completed_dialogues"),
                blockedCompletedDialogues    = GetList("blocked_completed_dialogues"),

                notes                        = Get("notes"),
            };

            // required_stage — written as "Friend", "Dating", etc.
            string stageStr = Get("required_stage");
            if (!string.IsNullOrEmpty(stageStr) &&
                Enum.TryParse<RelationshipStage>(stageStr, ignoreCase: true, out var stage))
            {
                entry.requiredStage = stage;
            }

            entries.Add(entry);
        }

        return entries;
    }

    // Split on newlines but respect quoted fields that contain newlines.
    private static List<string> SplitCsvLines(string csv)
    {
        var lines = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(c);
            }
            else if (c == '\n' && !inQuotes)
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        if (sb.Length > 0) lines.Add(sb.ToString());
        return lines;
    }

    // Parse a single CSV row into fields, handling quoted fields with escaped quotes.
    private static List<string> ParseCsvRow(string line)
    {
        var fields = new List<string>();
        int i = 0;

        while (i <= line.Length)
        {
            if (i == line.Length)
            {
                // Trailing comma edge case — empty final field already added; stop.
                break;
            }

            if (line[i] == '"')
            {
                // Quoted field
                i++;
                var sb = new StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                        }
                        else
                        {
                            i++;
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(line[i++]);
                    }
                }
                fields.Add(sb.ToString());
                // Consume trailing comma
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                // Unquoted field
                int start = i;
                while (i < line.Length && line[i] != ',') i++;
                fields.Add(line.Substring(start, i - start));
                if (i < line.Length) i++; // consume comma
            }
        }

        return fields;
    }
}