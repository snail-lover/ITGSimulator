// In an "Editor" folder
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements; // For ObjectField, Foldout, etc.
using UnityEditor; // For ScriptableObject.CreateInstance

public class DialogueNodeView : Node
{
    public DialogueNodeSaveData NodeData { get; private set; } // Make setter private if only set in constructor
    private DialogueNodeEditorDataWrapper _editableSOInstance; // Renamed for clarity

    public DialogueNodeView(DialogueNodeSaveData nodeData) : base()
    {
        this.NodeData = nodeData;
        this.title = string.IsNullOrEmpty(nodeData.nodeID) ? "Dialogue Node" : nodeData.nodeID;
        this.viewDataKey = nodeData.nodeGUID; // Important for persistence

        style.left = nodeData.position.x;
        style.top = nodeData.position.y;
    }

    public ScriptableObject GetEditableScriptableObject()
    {
        // Always create/update the wrapper to ensure it reflects current NodeData
        // especially if NodeData could be swapped or significantly altered externally (less common here)
        // For inspector purposes, it's usually created once and then its values are synced.
        if (_editableSOInstance == null)
        {
            _editableSOInstance = ScriptableObject.CreateInstance<DialogueNodeEditorDataWrapper>();
        }
        _editableSOInstance.Initialize(NodeData); // Refresh with current data
        _editableSOInstance.NodeViewTitleUpdateAction = newTitle => this.title = newTitle; // Pass action to update node title
        return _editableSOInstance;
    }
}

public class DialogueNodeEditorDataWrapper : ScriptableObject
{
    [Tooltip("The unique ID for this node in the final JSON data.")]
    public string nodeID;
    [TextArea(3, 10)]
    public string dialogueText;
    public ItemGate itemGate; // Ensure ItemGate is [Serializable]

    private DialogueNodeSaveData _originalData;
    public System.Action<string> NodeViewTitleUpdateAction;

    private void OnValidate()
    {
        if (_originalData != null)
        {
            bool titleChanged = _originalData.nodeID != nodeID;
            _originalData.nodeID = nodeID;
            _originalData.dialogueText = dialogueText;
            // Deep copy back if ItemGate is a class
            if (itemGate != null)
            {
                _originalData.itemGate = new ItemGate
                {
                    itemName = itemGate.itemName,
                    requiredItemID = itemGate.requiredItemID,
                    removeItemOnSelect = itemGate.removeItemOnSelect
                };
            }
            else
            {
                _originalData.itemGate = null;
            }

            if (titleChanged && NodeViewTitleUpdateAction != null)
            {
                NodeViewTitleUpdateAction.Invoke(nodeID);
            }
        }
    }

    public void Initialize(DialogueNodeSaveData originalData)
    {
        _originalData = originalData;
        nodeID = originalData.nodeID;
        dialogueText = originalData.dialogueText;
        if (originalData.itemGate != null)
        {
            itemGate = new ItemGate
            {
                itemName = originalData.itemGate.itemName,
                requiredItemID = originalData.itemGate.requiredItemID,
                removeItemOnSelect = originalData.itemGate.removeItemOnSelect
            };
        }
        else
        {
            itemGate = new ItemGate();
        }
        NodeViewTitleUpdateAction = null;
    }

    public void ApplyChangesToOriginal()
    {
        if (_originalData != null)
        {
            bool titleChanged = _originalData.nodeID != nodeID;
            _originalData.nodeID = nodeID;
            _originalData.dialogueText = dialogueText;
            if (itemGate != null)
            {
                _originalData.itemGate = new ItemGate
                {
                    itemName = itemGate.itemName,
                    requiredItemID = itemGate.requiredItemID,
                    removeItemOnSelect = itemGate.removeItemOnSelect
                };
            }
            else
            {
                _originalData.itemGate = null;
            }
            if (titleChanged && NodeViewTitleUpdateAction != null)
            {
                NodeViewTitleUpdateAction.Invoke(nodeID);
            }
        }
    }
}