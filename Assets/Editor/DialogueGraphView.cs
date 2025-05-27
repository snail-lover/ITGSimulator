// In an "Editor" folder
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(275, 350);
    private DialogueGraphEditorWindow _editorWindow;
    private DialogueSearchWindow _searchWindow;
    private List<ISelectable> _lastSelection = new List<ISelectable>(); // To track changes

    public DialogueGraphView(DialogueGraphEditorWindow editorWindow)
    {
        _editorWindow = editorWindow;

        var styleSheet = Resources.Load<StyleSheet>("DialogueGraphStyle");
        if (styleSheet != null) styleSheets.Add(styleSheet);
        else Debug.LogWarning("[DialogueGraphView] DialogueGraphStyle.uss not found in Resources.");

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger()); // This manipulator handles selection logic
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new FreehandSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddSearchWindow();

        // graphViewChanged is a good place to react to many things, including elements being removed
        // which might affect selection.
        this.graphViewChanged += OnGraphViewChanged;

        // Register a callback for MouseUpEvent. This is a common point to check selection.
        this.RegisterCallback<MouseUpEvent>(OnGraphMouseUp);

        this.RegisterCallback<KeyDownEvent>(OnGraphViewKeyDown);
    }

    private void OnGraphMouseUp(MouseUpEvent evt)
    {
        // After a mouse up, the selection might have changed due to clicks.
        // Check if the actual selection content has changed.
        CheckAndUpdateSelection();
    }

    private void CheckAndUpdateSelection()
    {
        // Compare current selection with the last known selection
        // This is a simple check; more complex scenarios might need deeper comparison.
        bool selectionHasChanged = false;
        if (this.selection.Count != _lastSelection.Count)
        {
            selectionHasChanged = true;
        }
        else
        {
            for (int i = 0; i < this.selection.Count; i++)
            {
                if (this.selection[i] != _lastSelection[i])
                {
                    selectionHasChanged = true;
                    break;
                }
            }
        }

        if (selectionHasChanged)
        {
            _lastSelection = new List<ISelectable>(this.selection); // Update last selection

            var selectedNodeView = this.selection.OfType<DialogueNodeView>().FirstOrDefault();
            if (_editorWindow != null)
            {
                if (selectedNodeView != null)
                {
                    _editorWindow.UpdateInspector(selectedNodeView.GetEditableScriptableObject());
                }
                else if (_editorWindow._currentGraphSO != null)
                {
                    _editorWindow.UpdateGraphInspector(_editorWindow._currentGraphSO);
                }
                else
                {
                    _editorWindow.UpdateInspector(null);
                }
            }
        }
    }


    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // Elements being removed can affect selection
        if (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Any())
        {
            // After elements are removed, the selection list (this.selection) will be updated by the GraphView.
            // We can then check it.
            // Using schedule.ExecuteNextFrame ensures this runs after GraphView has fully processed the removal.
            schedule.Execute(() => CheckAndUpdateSelection()).StartingIn(0);
        }

        // Other changes (like edges created) don't typically alter node selection directly,
        // but it's not harmful to check.
        // However, to avoid too frequent checks, you might be more specific.

        return graphViewChange;
    }

    private void OnGraphViewKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        {
            if (this.selection.Any())
            {
                DeleteSelection(); // This will trigger OnGraphViewChanged
                evt.StopPropagation();
            }
        }
    }

    private void AddSearchWindow()
    {
        _searchWindow = ScriptableObject.CreateInstance<DialogueSearchWindow>();
        _searchWindow.Init(this, _editorWindow);
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node &&
            endPort.portType == startPort.portType
        ).ToList();
    }

    public DialogueNodeView CreateDialogueNode(DialogueNodeSaveData nodeData, Vector2 position)
    {
        bool isNewNode = nodeData == null;
        if (isNewNode)
        {
            nodeData = new DialogueNodeSaveData
            {
                nodeGUID = Guid.NewGuid().ToString(),
                position = position,
                nodeID = $"Node_{Guid.NewGuid().ToString().Substring(0, 4)}",
                choices = new List<ChoiceSaveData>(),
                itemGate = new ItemGate()
            };
        }
        else
        {
            if (nodeData.choices == null) nodeData.choices = new List<ChoiceSaveData>();
            if (nodeData.itemGate == null) nodeData.itemGate = new ItemGate();
        }

        var nodeView = new DialogueNodeView(nodeData);
        nodeView.SetPosition(new Rect(position, DefaultNodeSize));

        var inputPort = nodeView.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        inputPort.portName = "In";
        nodeView.inputContainer.Add(inputPort);

        var nodeIdField = new TextField("Node ID:") { value = nodeData.nodeID };
        nodeIdField.RegisterValueChangedCallback(evt => {
            nodeData.nodeID = evt.newValue;
            nodeView.title = evt.newValue;
        });
        nodeView.mainContainer.Add(nodeIdField);

        var dialogueTextField = new TextField("Dialogue Text:") { multiline = true, value = nodeData.dialogueText };
        dialogueTextField.style.minHeight = 80;
        dialogueTextField.RegisterValueChangedCallback(evt => nodeData.dialogueText = evt.newValue);
        nodeView.mainContainer.Add(dialogueTextField);

        var nodeItemGateFoldout = new Foldout() { text = "Node Item Gate" };
        var nodeItemNameField = new TextField("Item Name:") { value = nodeData.itemGate.itemName };
        nodeItemNameField.RegisterValueChangedCallback(evt => nodeData.itemGate.itemName = evt.newValue);
        var nodeItemObjectField = new ObjectField("Required Item (Prefab):") { objectType = typeof(GameObject), value = nodeData.itemGate.requiredItem };
        nodeItemObjectField.RegisterValueChangedCallback(evt => nodeData.itemGate.requiredItem = evt.newValue as GameObject);
        nodeItemGateFoldout.Add(nodeItemNameField);
        nodeItemGateFoldout.Add(nodeItemObjectField);
        nodeView.mainContainer.Add(nodeItemGateFoldout);

        var addChoiceButton = new Button(() => AddChoicePort(nodeView, null, true)) { text = "Add Choice" };
        nodeView.titleButtonContainer.Add(addChoiceButton);

        foreach (var choiceSaveData in nodeData.choices)
        {
            AddChoicePort(nodeView, choiceSaveData, false);
        }

        var entryPointToggle = new Toggle("Is Main Entry Point") { value = nodeData.isEntryPoint };
        entryPointToggle.RegisterValueChangedCallback(evt => {
            nodeData.isEntryPoint = evt.newValue;
            if (evt.newValue) MarkAsEntryPoint(nodeView);
        });
        entryPointToggle.tooltip = "Is this the starting node for the entire dialogue tree?";
        nodeView.mainContainer.Add(entryPointToggle);

        nodeView.RefreshExpandedState();
        nodeView.RefreshPorts();
        AddElement(nodeView);
        return nodeView;
    }

    public Port AddChoicePort(DialogueNodeView nodeView, ChoiceSaveData choiceData = null, bool createNewData = false)
    {
        if (createNewData)
        {
            choiceData = new ChoiceSaveData
            {
                outputPortGUID = Guid.NewGuid().ToString(),
                choiceText = "New Choice",
                itemGate = new ItemGate()
            };
            nodeView.NodeData.choices.Add(choiceData);
        }
        else if (choiceData == null)
        {
            Debug.LogError("[DialogueGraphView] AddChoicePort error: choiceData is null and not creating new.");
            return null;
        }
        else
        {
            if (choiceData.itemGate == null) choiceData.itemGate = new ItemGate();
        }

        var outputPort = nodeView.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        outputPort.portName = "";
        if (string.IsNullOrEmpty(choiceData.outputPortGUID)) choiceData.outputPortGUID = Guid.NewGuid().ToString();
        outputPort.userData = choiceData.outputPortGUID;

        var choiceRowContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };

        var choiceTextField = new TextField { value = choiceData.choiceText, style = { flexGrow = 1, marginRight = 5 } };
        choiceTextField.RegisterValueChangedCallback(evt => choiceData.choiceText = evt.newValue);

        var deleteChoiceButton = new Button(() => RemoveChoicePort(nodeView, choiceData, choiceRowContainer, outputPort)) { text = "X" };

        choiceRowContainer.Add(choiceTextField);
        choiceRowContainer.Add(deleteChoiceButton);
        choiceRowContainer.Add(outputPort);

        var detailsContainer = new VisualElement { style = { marginLeft = 20, marginTop = 2, marginBottom = 5 } };
        detailsContainer.name = "ChoiceDetails_" + choiceData.outputPortGUID;

        var lovePointsField = new IntegerField("Love +/-:") { value = choiceData.lovePointChange };
        lovePointsField.RegisterValueChangedCallback(evt => choiceData.lovePointChange = evt.newValue);
        detailsContainer.Add(lovePointsField);

        var choiceItemGateFoldout = new Foldout { text = "Choice Item Gate" };
        var choiceItemNameField = new TextField("Item Name:") { value = choiceData.itemGate.itemName };
        choiceItemNameField.RegisterValueChangedCallback(evt => choiceData.itemGate.itemName = evt.newValue);
        var choiceItemObjectField = new ObjectField("Required Item:") { objectType = typeof(GameObject), value = choiceData.itemGate.requiredItem };
        choiceItemObjectField.RegisterValueChangedCallback(evt => choiceData.itemGate.requiredItem = evt.newValue as GameObject);
        choiceItemGateFoldout.Add(choiceItemNameField);
        choiceItemGateFoldout.Add(choiceItemObjectField);
        detailsContainer.Add(choiceItemGateFoldout);

        var choiceOverrideNextNodeIdField = new TextField("Override Next Node ID:") { value = choiceData.overrideNextNodeID };
        choiceOverrideNextNodeIdField.tooltip = "Use for 'END' or special targets not connectable in graph.";
        choiceOverrideNextNodeIdField.RegisterValueChangedCallback(evt => choiceData.overrideNextNodeID = evt.newValue);
        detailsContainer.Add(choiceOverrideNextNodeIdField);

        var cutsceneObjectField = new ObjectField("Trigger Cutscene:")
        {
            objectType = typeof(Cutscene), // Specify the type of asset
            value = choiceData.triggerCutscene,
            allowSceneObjects = false // We only want Project assets (ScriptableObjects)
        };
        cutsceneObjectField.RegisterValueChangedCallback(evt =>
        {
            choiceData.triggerCutscene = evt.newValue as Cutscene;
        });
        cutsceneObjectField.tooltip = "If a Cutscene is assigned here, it will play when this choice is selected. 'Next Node ID' will be ignored.";
        detailsContainer.Add(cutsceneObjectField);

        nodeView.outputContainer.Add(choiceRowContainer);
        nodeView.outputContainer.Add(detailsContainer);

        nodeView.RefreshPorts();
        nodeView.RefreshExpandedState();
        return outputPort;
    }

    private void MarkAsEntryPoint(DialogueNodeView newEntryPointView)
    {
        foreach (var node in this.nodes.OfType<DialogueNodeView>())
        {
            if (node.NodeData.isEntryPoint && node != newEntryPointView)
            {
                node.NodeData.isEntryPoint = false;
                var oldEntryPointToggle = node.mainContainer.Query<Toggle>().ToList()
                    .FirstOrDefault(t => t.label == "Is Main Entry Point");
                if (oldEntryPointToggle != null)
                {
                    oldEntryPointToggle.SetValueWithoutNotify(false);
                }
            }
        }
    }

    private void RemoveChoicePort(DialogueNodeView nodeView, ChoiceSaveData choiceDataToRemove, VisualElement choiceRowContainer, Port portToRemove)
    {
        if (portToRemove != null)
        {
            var edgesToDelete = this.edges.ToList().Where(x => x.output == portToRemove).ToList();
            foreach (var edge in edgesToDelete)
            {
                edge.input?.Disconnect(edge);
                RemoveElement(edge);
            }
        }

        nodeView.NodeData.choices.Remove(choiceDataToRemove);
        nodeView.outputContainer.Remove(choiceRowContainer);

        VisualElement detailsToRemove = nodeView.outputContainer.Q("ChoiceDetails_" + choiceDataToRemove.outputPortGUID);
        if (detailsToRemove != null)
        {
            nodeView.outputContainer.Remove(detailsToRemove);
        }
        else
        {
            Debug.LogWarning($"[DialogueGraphView] Could not find details container 'ChoiceDetails_{choiceDataToRemove.outputPortGUID}'.");
        }

        nodeView.RefreshPorts();
        nodeView.RefreshExpandedState();
    }

    public void SaveGraphToSO(DialogueGraphSO graphSO)
    {
        if (graphSO == null)
        {
            Debug.LogError("[DialogueGraphView] SaveGraphToSO: graphSO is null.");
            return;
        }

        graphSO.nodes.Clear();
        graphSO.edges.Clear();
        graphSO.entryPointNodeGUID = null;

        foreach (DialogueNodeView nodeView in this.nodes.OfType<DialogueNodeView>())
        {
            (nodeView.GetEditableScriptableObject() as DialogueNodeEditorDataWrapper)?.ApplyChangesToOriginal();
            nodeView.NodeData.position = nodeView.GetPosition().position;
            graphSO.nodes.Add(nodeView.NodeData);

            if (nodeView.NodeData.isEntryPoint)
            {
                if (graphSO.entryPointNodeGUID != null)
                {
                    Debug.LogWarning($"[DialogueGraphView] Multiple entry points designated. Using last one: {nodeView.NodeData.nodeID}");
                }
                graphSO.entryPointNodeGUID = nodeView.NodeData.nodeGUID;
            }
        }

        foreach (Edge edge in this.edges.ToList())
        {
            var outputNodeView = edge.output?.node as DialogueNodeView;
            var inputNodeView = edge.input?.node as DialogueNodeView;

            if (outputNodeView != null && inputNodeView != null && edge.output != null)
            {
                graphSO.edges.Add(new EdgeSaveData
                {
                    outputNodeGUID = outputNodeView.NodeData.nodeGUID,
                    outputNodePortGUID = edge.output.userData as string,
                    inputNodeGUID = inputNodeView.NodeData.nodeGUID
                });
            }
        }
        EditorUtility.SetDirty(graphSO);
    }

    public void LoadGraphFromSO(DialogueGraphSO graphSO)
    {
        if (graphSO == null)
        {
            Debug.LogError("[DialogueGraphView] LoadGraphFromSO: graphSO is null.");
            return;
        }

        DeleteElements(this.graphElements.ToList());
        _lastSelection.Clear(); // Clear last selection when loading new graph

        var nodeViewCache = new Dictionary<string, DialogueNodeView>();

        if (graphSO.nodes == null) graphSO.nodes = new List<DialogueNodeSaveData>();
        foreach (DialogueNodeSaveData nodeData in graphSO.nodes)
        {
            if (nodeData == null) continue;
            var nodeView = CreateDialogueNode(nodeData, nodeData.position);
            if (!string.IsNullOrEmpty(nodeData.nodeGUID))
            {
                nodeViewCache[nodeData.nodeGUID] = nodeView;
            }
        }

        if (graphSO.edges == null) graphSO.edges = new List<EdgeSaveData>();
        foreach (EdgeSaveData edgeData in graphSO.edges)
        {
            if (edgeData == null) continue;
            if (nodeViewCache.TryGetValue(edgeData.outputNodeGUID, out DialogueNodeView outputNodeView) &&
                nodeViewCache.TryGetValue(edgeData.inputNodeGUID, out DialogueNodeView inputNodeView))
            {
                Port outputPort = outputNodeView.outputContainer.Query<Port>().ToList()
                                    .FirstOrDefault(p => p.userData as string == edgeData.outputNodePortGUID);
                Port inputPort = inputNodeView.inputContainer.Q<Port>();

                if (outputPort != null && inputPort != null)
                {
                    Edge edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
                else
                {
                    Debug.LogWarning($"[Load] Edge connect fail: Port not found. OutputPortGUID: {edgeData.outputNodePortGUID} on Node {edgeData.outputNodeGUID}, InputNode: {edgeData.inputNodeGUID}");
                }
            }
            else
            {
                Debug.LogWarning($"[Load] Edge fail: Node GUID not found. Output: '{edgeData.outputNodeGUID}', Input: '{edgeData.inputNodeGUID}'.");
            }
        }
        // After loading, update the inspector to show graph properties or nothing if no nodes are auto-selected.
        CheckAndUpdateSelection();
    }
}