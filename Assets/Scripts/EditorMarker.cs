using UnityEngine;

public class EditorMarker : MonoBehaviour
{
    public Color gizmoColor = Color.yellow;
    public float gizmoSize = 0.5f;
    public enum GizmoType { Sphere, Cube, WireSphere, WireCube }
    public GizmoType type = GizmoType.Sphere;

    // This function is called by the editor when Gizmos are being drawn
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor; // Set the color of the Gizmo

        switch (type)
        {
            case GizmoType.Sphere:
                Gizmos.DrawSphere(transform.position, gizmoSize);
                break;
            case GizmoType.Cube:
                Gizmos.DrawCube(transform.position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                break;
            case GizmoType.WireSphere:
                Gizmos.DrawWireSphere(transform.position, gizmoSize);
                break;
            case GizmoType.WireCube:
                Gizmos.DrawWireCube(transform.position, new Vector3(gizmoSize, gizmoSize, gizmoSize));
                break;
        }

        // You can also draw lines, text, etc.
        // Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
        // UnityEditor.Handles.Label(transform.position + Vector3.up, gameObject.name); // For text (requires using UnityEditor;)
    }

    // If you only want it to draw when selected, use OnDrawGizmosSelected()
    // void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawSphere(transform.position, gizmoSize * 1.2f); // Make it slightly larger when selected
    // }
}