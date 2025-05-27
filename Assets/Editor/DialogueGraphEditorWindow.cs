// In an "Editor" folder
using System.IO; // For Path operations
using UnityEditor;
using UnityEditor.Callbacks; // For OnOpenAsset
using UnityEditor.UIElements; // <--- ADDED for InspectorView
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphEditorWindow : EditorWindow
{
    private DialogueGraphView _graphView;
    internal DialogueGraphSO _currentGraphSO; // <--- CHANGED from private to internal
    private Label _fileNameLabel;
    private InspectorView _inspectorView;

    [MenuItem("Dialogue/Dialogue Graph Editor")]
    public static void OpenWindow()
    {
        GetWindow<DialogueGraphEditorWindow>("Dialogue Graph Editor");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        Object item = EditorUtility.InstanceIDToObject(instanceID);
        if (item is DialogueGraphSO graphSO)
        {
            DialogueGraphEditorWindow window = GetWindow<DialogueGraphEditorWindow>("Dialogue Graph Editor");
            window.LoadGraph(graphSO);
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        ConstructGraphView();
        GenerateToolbar();
        // GenerateMiniMap(); // Optional - uncomment if you implement it
        GenerateInspectorView();
    }

    private void OnDisable()
    {
        if (_graphView != null) rootVisualElement.Remove(_graphView);
        if (_inspectorView != null && _inspectorView.parent != null) // Check parent before removing
        {
            rootVisualElement.Remove(_inspectorView);
        }
    }

    private void ConstructGraphView()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolbar()
    {
        var toolbar = new Toolbar();

        _fileNameLabel = new Label("No Graph Loaded");
        toolbar.Add(_fileNameLabel);

        var saveAsButton = new Button(() => RequestSaveData(true)) { text = "Save As..." };
        toolbar.Add(saveAsButton);

        var saveButton = new Button(() => RequestSaveData(false)) { text = "Save" };
        toolbar.Add(saveButton);

        var loadButton = new Button(() => RequestLoadData()) { text = "Load" };
        toolbar.Add(loadButton);

        var exportButton = new Button(ExportToJson) { text = "Export to JSON" };
        toolbar.Add(exportButton);

        rootVisualElement.Add(toolbar);
    }

    private void GenerateMiniMap()
    {
        // Implementation for MiniMap if you choose to add one
        // var miniMap = new MiniMap { anchored = true };
        // _graphView.Add(miniMap);
        // miniMap.SetPosition(new Rect(10, 30, 200, 140)); // Adjust position
    }

    private void GenerateInspectorView()
    {
        _inspectorView = new InspectorView();
        _inspectorView.style.width = 250; // Adjust as needed
        // Consider adding this to a TwoPaneSplitView for better layout with the graph
        // For now, ensure it's added to the rootVisualElement when it should be visible.
        // It will be added/removed in UpdateInspector/UpdateGraphInspector
    }

    public void UpdateInspector(ScriptableObject so)
    {
        if (_inspectorView == null) GenerateInspectorView();

        // Remove from parent if it's already there
        if (_inspectorView.parent != null)
        {
            _inspectorView.parent.Remove(_inspectorView);
        }

        if (so != null)
        {
            _inspectorView.SetEnabled(true);
            // For InspectorView to work well, 'so' should be a Unity Object (like a ScriptableObject asset).
            // DialogueNodeEditorDataWrapper is a ScriptableObject, so this should work.
            var serializedObject = new SerializedObject(so);
            _inspectorView.Bind(serializedObject); // Use Bind for ScriptableObjects

            // Add to the layout (e.g., to the side)
            // This simple add might put it on top of the graph view or below the toolbar.
            // A TwoPaneSplitView is recommended for a side-by-side inspector.
            if (!rootVisualElement.Contains(_inspectorView))
            {
                // A common layout is a Horizontal TwoPaneSplitView
                // For simplicity, just adding it here. You'll want to refine layout.
                rootVisualElement.Add(_inspectorView); // This might need adjustment for proper layout
            }
        }
        else
        {
            _inspectorView.SetEnabled(false);
            // If you were using _inspectorView.UpdateSelection(so), you might clear it.
            // With Bind, detaching and re-attaching or binding to null (if supported) handles it.
        }
    }

    public void UpdateGraphInspector(DialogueGraphSO graphSO)
    {
        if (_inspectorView == null) GenerateInspectorView();

        if (_inspectorView.parent != null)
        {
            _inspectorView.parent.Remove(_inspectorView);
        }

        if (graphSO != null)
        {
            _inspectorView.SetEnabled(true);
            var serializedObject = new SerializedObject(graphSO);
            _inspectorView.Bind(serializedObject);
            if (!rootVisualElement.Contains(_inspectorView))
            {
                rootVisualElement.Add(_inspectorView); // Adjust layout as needed
            }
        }
        else
        {
            _inspectorView.SetEnabled(false);
        }
    }

    private void RequestSaveData(bool saveAs)
    {
        string filePath = null;
        if (_currentGraphSO == null || saveAs)
        {
            filePath = EditorUtility.SaveFilePanelInProject("Save Dialogue Graph", "New Dialogue Graph", "asset", "Please enter a file name to save the dialogue graph to.");
            if (string.IsNullOrEmpty(filePath)) return;

            // If saving as new, or if current is null, create new or load/overwrite
            DialogueGraphSO existingAsset = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(filePath);
            if (existingAsset != null && saveAs) // Overwriting with "Save As"
            {
                _currentGraphSO = existingAsset;
            }
            else if (existingAsset == null) // Creating new
            {
                _currentGraphSO = CreateInstance<DialogueGraphSO>();
                AssetDatabase.CreateAsset(_currentGraphSO, filePath);
            }
            else // Saving to existing asset (not saveAs, but _currentGraphSO was null)
            {
                _currentGraphSO = existingAsset;
            }
            _fileNameLabel.text = Path.GetFileNameWithoutExtension(filePath);
        }
        else // Saving existing (_currentGraphSO is not null and not saveAs)
        {
            filePath = AssetDatabase.GetAssetPath(_currentGraphSO);
        }


        if (_currentGraphSO != null && _graphView != null)
        {
            _graphView.SaveGraphToSO(_currentGraphSO);
            EditorUtility.SetDirty(_currentGraphSO);
            AssetDatabase.SaveAssets(); // Save all changes to assets
            AssetDatabase.Refresh();    // Refresh asset database
            Debug.Log($"Graph saved to: {Path.GetFileName(filePath)}");
            UpdateGraphInspector(_currentGraphSO);
        }
    }

    private void RequestLoadData()
    {
        string filePath = EditorUtility.OpenFilePanel("Load Dialogue Graph", Application.dataPath, "asset"); // Start in Assets
        if (string.IsNullOrEmpty(filePath)) return;

        if (filePath.StartsWith(Application.dataPath))
        {
            filePath = "Assets" + filePath.Substring(Application.dataPath.Length);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Please select an asset from within the project.", "OK");
            return;
        }

        DialogueGraphSO graphSO = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(filePath);
        if (graphSO == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not load the selected file as DialogueGraphSO.", "OK");
            return;
        }
        LoadGraph(graphSO);
    }

    public void LoadGraph(DialogueGraphSO graphSO)
    {
        _currentGraphSO = graphSO;
        _fileNameLabel.text = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(graphSO));
        if (_graphView != null)
        {
            _graphView.LoadGraphFromSO(graphSO);
        }
        Debug.Log($"Graph loaded: {graphSO.name}");
        UpdateGraphInspector(graphSO);
    }

    private void ExportToJson()
    {
        if (_currentGraphSO == null)
        {
            EditorUtility.DisplayDialog("No Graph Loaded", "Please load or save a dialogue graph first.", "OK");
            return;
        }

        // Ensure the SO is up-to-date with the graph view's state
        if (_graphView != null)
        {
            _graphView.SaveGraphToSO(_currentGraphSO);
            EditorUtility.SetDirty(_currentGraphSO); // Mark as dirty before ToRuntimeData if it relies on serialized fields
        }

        DialogueData runtimeData = _currentGraphSO.ToRuntimeData();

        runtimeData.nodeDictionary = new System.Collections.Generic.Dictionary<string, DialogueNode>(); // Ensure System.Collections.Generic
        if (runtimeData.nodes != null)
        {
            foreach (var node in runtimeData.nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.nodeID))
                {
                    if (!runtimeData.nodeDictionary.ContainsKey(node.nodeID))
                        runtimeData.nodeDictionary.Add(node.nodeID, node);
                    else
                        Debug.LogWarning($"[ExportToJson] Duplicate nodeID '{node.nodeID}' found. Only first instance added to dictionary.");
                }
            }
        }

        string json = JsonUtility.ToJson(runtimeData, true);

        string exportPath = EditorUtility.SaveFilePanel("Export Dialogue to JSON", Application.dataPath, _currentGraphSO.name, "json");
        if (!string.IsNullOrEmpty(exportPath))
        {
            File.WriteAllText(exportPath, json);
            Debug.Log($"Dialogue exported to JSON: {exportPath}");
            AssetDatabase.Refresh();
        }
    }

    // This method was defined but not called by GraphView in your snippet.
    // GraphView's OnNodeSelectionChanged calls UpdateInspector directly.
    // public void OnNodeSelected(DialogueNodeView nodeView)
    // {
    //     if (nodeView != null && nodeView.GetEditableScriptableObject() != null) {
    //         UpdateInspector(nodeView.GetEditableScriptableObject());
    //     } else {
    //         UpdateGraphInspector(_currentGraphSO); // Show graph SO if no node selected or node has no SO
    //     }
    // }
}