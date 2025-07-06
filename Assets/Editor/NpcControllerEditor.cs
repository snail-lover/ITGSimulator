using UnityEngine;
using UnityEditor; // We still need this for things like [CustomEditor] and EditorGUILayout.

// This attribute tells Unity that this script defines the custom Inspector for the NpcController class.
[CustomEditor(typeof(NpcController))]
// THE CRITICAL FIX: We explicitly state we are inheriting from the 'Editor' class
// that lives inside the 'UnityEditor' namespace. This resolves all ambiguity.
public class NpcControllerEditor : UnityEditor.Editor
{
    // We override the base OnInspectorGUI method to draw our custom UI.
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector fields first (e.g., the Npc Config slot).
        base.OnInspectorGUI();

        // 'target' is a property of the base UnityEditor.Editor class.
        // This will now work correctly because the inheritance is unambiguous.
        NpcController controller = (NpcController)target;

        // We only want to display this live data when the game is in Play Mode.
        if (Application.isPlaying)
        {
            // A null check to prevent errors before Awake() has run.
            if (controller.runtimeData != null)
            {
                // Add some visual separation in the Inspector.
                EditorGUILayout.Space(10);

                // Draw a bold label.
                EditorGUILayout.LabelField("Live Runtime Data", EditorStyles.boldLabel);

                // Use a "disabled group" to make the fields appear greyed out and read-only.
                EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.IntField("Current Love", controller.runtimeData.currentLove);

                EditorGUI.EndDisabledGroup();

                // 'Repaint()' is a method of the base UnityEditor.Editor class.
                // This will now work correctly. It tells Unity to redraw this Inspector
                // window, which is how we get live updates.
                Repaint();
            }
        }
    }
}