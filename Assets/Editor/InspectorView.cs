using UnityEngine.UIElements;
using UnityEditor;

public class InspectorView : VisualElement
{
    public InspectorView()
    {
        // Optionally add styling or layout here
    }

    public void Bind(SerializedObject serializedObject)
    {
        this.Clear();
        if (serializedObject != null)
        {
            var inspector = new UnityEditor.UIElements.InspectorElement(serializedObject);
            this.Add(inspector);
        }
    }
}