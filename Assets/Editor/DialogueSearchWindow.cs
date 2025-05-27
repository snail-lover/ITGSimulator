// In an "Editor" folder
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView _graphView;
    private DialogueGraphEditorWindow _editorWindow;

    public void Init(DialogueGraphView graphView, DialogueGraphEditorWindow editorWindow)
    {
        _graphView = graphView;
        _editorWindow = editorWindow;
    }

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeEntry(new GUIContent("Dialogue Node"))
            {
                userData = typeof(DialogueNodeView), // Or a specific enum/type
                level = 1
            },
            // Add other node types here if you have them (e.g., "Event Node", "Condition Node")
        };
        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        var worldMousePosition = _editorWindow.rootVisualElement.ChangeCoordinatesTo(_editorWindow.rootVisualElement.parent, context.screenMousePosition - _editorWindow.position.position);
        var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition);

        if (searchTreeEntry.userData is System.Type type)
        {
            if (type == typeof(DialogueNodeView))
            {
                _graphView.CreateDialogueNode(null, localMousePosition); // Pass null for new node data
                return true;
            }
            // Handle other types
        }
        return false;
    }
}