using UnityEngine;

/// <summary>
/// Draws customizable gizmos in the Unity Editor for scene visualization.
/// Attach this component to any GameObject to display a gizmo at its position.
/// </summary>
public class EditorMarker : MonoBehaviour
{
    /// <summary>
    /// The color of the gizmo.
    /// </summary>
    public Color gizmoColor = Color.yellow;

    /// <summary>
    /// The size (radius or edge length) of the gizmo.
    /// </summary>
    public float gizmoSize = 0.5f;

    /// <summary>
    /// Types of gizmos that can be drawn.
    /// </summary>
    public enum GizmoType
    {
        Sphere,
        Cube,
        WireSphere,
        WireCube
    }

    /// <summary>
    /// The selected gizmo type to draw.
    /// </summary>
    public GizmoType type = GizmoType.Sphere;

    /// <summary>
    /// Draws the selected gizmo in the Scene view.
    /// Called automatically by the Unity Editor.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

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
    }

    /*
    // Uncomment to draw gizmo only when the object is selected in the editor.
    // void OnDrawGizmosSelected()
    // {
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawSphere(transform.position, gizmoSize * 1.2f);
    // }
    */
}