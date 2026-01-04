// In an "Editor" folder
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class DialogueGraphView : GraphView
{
    public readonly Vector2 DefaultNodeSize = new Vector2(300, 400);
    private readonly DialogueGraphEditorWindow _editorWindow;
    private DialogueSearchWindow _searchWindow;
   
    private List<ISelectable> _lastSelection = new List<ISelectable>();


    public DialogueGraphView(DialogueGraphEditorWindow editorWindow)
    {
        _editorWindow = editorWindow;

        // Ensure the ItemDatabase is ready for use by the editor.
        var styleSheet = Resources.Load<StyleSheet>("DialogueGraphStyle");
        if (styleSheet != null)
        {
            styleSheets.Add(styleSheet);
        }
        else
        {
            Debug.LogWarning("[DialogueGraphView] DialogueGraphStyle.uss not found in a 'Resources' folder. The graph will use default styling.");
        }
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraphStyle"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddSearchWindow();


        graphViewChanged += OnGraphViewChanged;
        RegisterCallback<MouseUpEvent>(OnGraphMouseUp);
        RegisterCallback<KeyDownEvent>(OnGraphViewKeyDown);
    }

    // --- FIX: Restore the OnGraphViewChanged callback ---
    // This is needed to detect when a selection is deleted.
    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // When elements are removed (e.g., via the Delete key), the selection might change.
        if (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Any())
        {
            // We use schedule.Execute to run this check after the GraphView has finished processing the removal.
            schedule.Execute(CheckAndUpdateSelection).StartingIn(0);
        }
        return graphViewChange;
    }

    // --- FIX: Restore the OnGraphMouseUp callback ---
    // This is needed to detect when a user clicks to change selection.
    private void OnGraphMouseUp(MouseUpEvent evt)
    {
        CheckAndUpdateSelection();
    }

    // --- FIX: Restore the selection checking logic ---
    private void CheckAndUpdateSelection()
    {
        // If the content of the selection list has changed, update the inspector.
        if (!selection.SequenceEqual(_lastSelection))
        {
            _lastSelection = new List<ISelectable>(selection); // Update our tracker

            var selectedNodeView = selection.OfType<DialogueNodeView>().FirstOrDefault();
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


    private void OnGraphViewKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        {
            // Use the GraphView's built-in method to delete selected elements.
            DeleteSelection();
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
        return ports.Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node &&
            endPort.portType == startPort.portType
        ).ToList();
    }

    // This helper class is used for the ListView to correctly handle callbacks.
    private class ItemData
    {
        public Action removeAction;
        public EventCallback<ChangeEvent<string>> keyChangeCallback;
        public EventCallback<ChangeEvent<bool>> valueChangeCallback;
    }

    private VisualElement CreateConditionsUI(List<WorldStateCondition> conditions)
    {
        var container = new Foldout { text = "World State Conditions" };
        var conditionList = new ListView(conditions, 20,
            makeItem: () =>
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var keyField = new TextField { style = { flexGrow = 1 } };
                var valueToggle = new Toggle("Must be TRUE:");
                var removeButton = new Button { text = "X" };
                row.Add(keyField);
                row.Add(valueToggle);
                row.Add(removeButton);
                row.userData = new ItemData();
                return row;
            },
            bindItem: (element, i) =>
            {
                var keyField = element.Q<TextField>();
                var valueToggle = element.Q<Toggle>();
                var removeButton = element.Q<Button>();
                var itemData = element.userData as ItemData;

                if (itemData.removeAction != null)
                {
                    removeButton.clicked -= itemData.removeAction;
                    keyField.UnregisterValueChangedCallback(itemData.keyChangeCallback);
                    valueToggle.UnregisterValueChangedCallback(itemData.valueChangeCallback);
                }

                keyField.value = conditions[i].conditionKey;
                valueToggle.value = conditions[i].requiredValue;

                itemData.removeAction = () => { conditions.RemoveAt(i); ((ListView)element.parent).RefreshItems(); };
                itemData.keyChangeCallback = evt => conditions[i].conditionKey = evt.newValue;
                itemData.valueChangeCallback = evt => conditions[i].requiredValue = evt.newValue;

                removeButton.clicked += itemData.removeAction;
                keyField.RegisterValueChangedCallback(itemData.keyChangeCallback);
                valueToggle.RegisterValueChangedCallback(itemData.valueChangeCallback);
            });

        conditionList.headerTitle = "Conditions";
        conditionList.showAddRemoveFooter = true;
        conditionList.reorderable = true;
        var addButton = conditionList.Q<Button>("add-button");
        if (addButton != null)
        {
            addButton.clicked += () =>
            {
                conditions.Add(new WorldStateCondition());
                conditionList.RefreshItems();
            };
        }

        container.Add(conditionList);
        return container;
    }

    private VisualElement CreateStateChangesUI(List<WorldStateChange> stateChanges)
    {
        var container = new Foldout() { text = "State Changes on Select" };
        var stateChangeList = new ListView(stateChanges, 20,
            makeItem: () =>
            {
                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                var keyField = new TextField("State Key:") { style = { flexGrow = 1 } };
                var valueToggle = new Toggle("Set to TRUE:");
                var removeButton = new Button { text = "X" };
                row.Add(keyField);
                row.Add(valueToggle);
                row.Add(removeButton);
                row.userData = new ItemData();
                return row;
            },
            bindItem: (element, i) =>
            {
                var keyField = element.Q<TextField>();
                var valueToggle = element.Q<Toggle>();
                var removeButton = element.Q<Button>();
                var itemData = element.userData as ItemData;

                if (itemData.removeAction != null)
                {
                    removeButton.clicked -= itemData.removeAction;
                    keyField.UnregisterValueChangedCallback(itemData.keyChangeCallback);
                    valueToggle.UnregisterValueChangedCallback(itemData.valueChangeCallback);
                }

                keyField.value = stateChanges[i].stateKey;
                valueToggle.value = stateChanges[i].stateValue;

                itemData.removeAction = () => { stateChanges.RemoveAt(i); ((ListView)element.parent).RefreshItems(); };
                itemData.keyChangeCallback = evt => stateChanges[i].stateKey = evt.newValue;
                itemData.valueChangeCallback = evt => stateChanges[i].stateValue = evt.newValue;

                removeButton.clicked += itemData.removeAction;
                keyField.RegisterValueChangedCallback(itemData.keyChangeCallback);
                valueToggle.RegisterValueChangedCallback(itemData.valueChangeCallback);
            });

        stateChangeList.headerTitle = "State Changes";
        stateChangeList.showAddRemoveFooter = true;
        stateChangeList.reorderable = true;
        var addButton = stateChangeList.Q<Button>("add-button");
        if (addButton != null)
        {
            addButton.clicked += () => {
                stateChanges.Add(new WorldStateChange());
                stateChangeList.RefreshItems();
            };
        }

        container.Add(stateChangeList);
        return container;
    }


    public DialogueNodeView CreateDialogueNode(DialogueNodeSaveData nodeData, Vector2 position)
    {
        // This logic handles both creating a new node and loading an existing one.
        if (nodeData == null)
        {
            nodeData = new DialogueNodeSaveData
            {
                nodeGUID = Guid.NewGuid().ToString(),
                position = position,
                nodeID = $"Node_{Guid.NewGuid().ToString().Substring(0, 4)}",
            };
        }
        // Ensure lists are initialized to avoid null reference errors
        nodeData.choices ??= new List<ChoiceSaveData>();
        nodeData.itemGate ??= new ItemGate();
        nodeData.worldStateConditions ??= new List<WorldStateCondition>();

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

        // --- NODE ITEM GATE REFACTOR ---
        var nodeItemGateFoldout = new Foldout() { text = "Node Item Gate" };
        var nodeItemNameField = new TextField("Item Name (Display):") { value = nodeData.itemGate.itemName };
        nodeItemNameField.RegisterValueChangedCallback(evt => nodeData.itemGate.itemName = evt.newValue);

        // Find the full item object from its saved ID to display in the UI.



        nodeItemGateFoldout.Add(nodeItemNameField);
        nodeView.mainContainer.Add(nodeItemGateFoldout);

        nodeView.mainContainer.Add(CreateConditionsUI(nodeData.worldStateConditions));

        var addChoiceButton = new Button(() => AddChoicePort(nodeView, null, true)) { text = "Add Choice" };
        nodeView.titleButtonContainer.Add(addChoiceButton);

        foreach (var choiceSaveData in nodeData.choices)
        {
            AddChoicePort(nodeView, choiceSaveData, false);
        }

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
            };
            nodeView.NodeData.choices.Add(choiceData);
        }

        // Initialize sub-objects if they are null (important for loading old data)
        choiceData.itemGate ??= new ItemGate();
        choiceData.worldStateConditions ??= new List<WorldStateCondition>();
        choiceData.stateChangesOnSelect ??= new List<WorldStateChange>();

        var outputPort = nodeView.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        outputPort.portName = "";
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

        // --- CHOICE ITEM GATE REFACTOR ---
        var choiceItemGateFoldout = new Foldout { text = "Choice Item Gate" };
        var choiceItemNameField = new TextField("Item Name:") { value = choiceData.itemGate.itemName };
        choiceItemNameField.RegisterValueChangedCallback(evt => choiceData.itemGate.itemName = evt.newValue);


        var removeItemToggle = new Toggle("Remove on Select:") { value = choiceData.itemGate.removeItemOnSelect };
        removeItemToggle.RegisterValueChangedCallback(evt => choiceData.itemGate.removeItemOnSelect = evt.newValue);

        choiceItemGateFoldout.Add(choiceItemNameField);
        choiceItemGateFoldout.Add(removeItemToggle);
        detailsContainer.Add(choiceItemGateFoldout);

        var overrideNextNodeIdField = new TextField("Override Next Node ID:") { value = choiceData.overrideNextNodeID };
        overrideNextNodeIdField.RegisterValueChangedCallback(evt => choiceData.overrideNextNodeID = evt.newValue);
        detailsContainer.Add(overrideNextNodeIdField);

        detailsContainer.Add(CreateConditionsUI(choiceData.worldStateConditions));
        detailsContainer.Add(CreateStateChangesUI(choiceData.stateChangesOnSelect));

        // Add the "Starts Hangout" checkbox
        var startsHangoutToggle = new Toggle("Starts Hangout:") { value = choiceData.startsHangout };
        startsHangoutToggle.RegisterValueChangedCallback(evt => choiceData.startsHangout = evt.newValue);
        detailsContainer.Add(startsHangoutToggle);


        nodeView.outputContainer.Add(choiceRowContainer);
        nodeView.outputContainer.Add(detailsContainer);

        nodeView.RefreshPorts();
        nodeView.RefreshExpandedState();
        return outputPort;
    }

    private void RemoveChoicePort(DialogueNodeView nodeView, ChoiceSaveData choiceDataToRemove, VisualElement choiceRowContainer, Port portToRemove)
    {
        var edgesToDelete = edges.ToList().Where(x => x.output == portToRemove).ToList();
        DeleteElements(edgesToDelete);

        nodeView.NodeData.choices.Remove(choiceDataToRemove);
        nodeView.outputContainer.Remove(choiceRowContainer);

        VisualElement detailsToRemove = nodeView.outputContainer.Q("ChoiceDetails_" + choiceDataToRemove.outputPortGUID);
        if (detailsToRemove != null)
        {
            nodeView.outputContainer.Remove(detailsToRemove);
        }

        nodeView.RefreshPorts();
        nodeView.RefreshExpandedState();
    }

    public void SaveGraphToSO(DialogueGraphSO graphSO)
    {
        if (graphSO == null) return;

        // Clear old data
        graphSO.nodes.Clear();
        graphSO.edges.Clear();

        // Save nodes
        foreach (var nodeView in nodes.OfType<DialogueNodeView>())
        {
            nodeView.NodeData.position = nodeView.GetPosition().position;
            graphSO.nodes.Add(nodeView.NodeData);
        }

        // Save edges
        foreach (var edge in edges)
        {
            var outputNodeView = edge.output?.node as DialogueNodeView;
            var inputNodeView = edge.input?.node as DialogueNodeView;

            if (outputNodeView == null || inputNodeView == null) continue;

            graphSO.edges.Add(new EdgeSaveData
            {
                outputNodeGUID = outputNodeView.NodeData.nodeGUID,
                outputNodePortGUID = edge.output.userData as string,
                inputNodeGUID = inputNodeView.NodeData.nodeGUID
            });
        }

        EditorUtility.SetDirty(graphSO);
        AssetDatabase.SaveAssets();
    }

    public void LoadGraphFromSO(DialogueGraphSO graphSO)
    {
        if (graphSO == null) return;

        // Clear existing graph elements
        DeleteElements(graphElements);

        var nodeViewCache = new Dictionary<string, DialogueNodeView>();

        // First pass: create all node views from saved data
        foreach (DialogueNodeSaveData nodeData in graphSO.nodes)
        {
            var nodeView = CreateDialogueNode(nodeData, nodeData.position);
            nodeViewCache[nodeData.nodeGUID] = nodeView;
        }

        // Second pass: connect the nodes with edges
        foreach (EdgeSaveData edgeData in graphSO.edges)
        {
            if (!nodeViewCache.TryGetValue(edgeData.outputNodeGUID, out var outputNodeView) ||
                !nodeViewCache.TryGetValue(edgeData.inputNodeGUID, out var inputNodeView))
            {
                continue;
            }

            Port outputPort = outputNodeView.outputContainer.Query<Port>().ToList()
                .FirstOrDefault(p => p.userData as string == edgeData.outputNodePortGUID);
            Port inputPort = inputNodeView.inputContainer.Q<Port>();

            if (outputPort != null && inputPort != null)
            {
                var edge = outputPort.ConnectTo(inputPort);
                AddElement(edge);
            }
        }
    }

    #region Helper Methods

    /// <summary>
    /// Ensures the ItemDatabase is loaded and available for the editor.
    /// </summary>

    /// <summary>
    /// A safe helper method to get an item from the database using its ID.
    /// </summary>

    #endregion
}