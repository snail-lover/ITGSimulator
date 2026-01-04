// --- START OF FILE SearchableZone.cs ---

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class SearchableZone : MonoBehaviour
{
    [Header("Zone Definition")]
    [Tooltip("A unique, descriptive name for this area (e.g., 'Library', 'Cafeteria').")]
    public string zoneName = "Unnamed Zone";

    [Header("Gizmo Settings")]
    [Tooltip("Toggle the visibility of the zone's gizmo in the Scene view.")]
    public bool showGizmo = true; // <-- ADDED THIS LINE

    // --- Static Registry for Easy Access ---
    // This part remains the same.
    public static List<SearchableZone> AllZones { get; private set; } = new List<SearchableZone>();

    private void OnEnable()
    {
        if (!AllZones.Contains(this))
        {
            AllZones.Add(this);
        }
    }

    private void OnDisable()
    {
        AllZones.Remove(this);
    }

    // --- Gizmos for easy level design ---
    // The Gizmo now draws the zone's boundaries, which is more useful.
    private void OnDrawGizmos()
    {
        // Only draw the Gizmo if the 'showGizmo' checkbox is ticked
        if (!showGizmo) // <-- MODIFIED THIS SECTION
        {
            return;
        }

        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f); // Cyan, semi-transparent
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(collider.center, collider.size);
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 1f); // Opaque for the wireframe
            Gizmos.DrawWireCube(collider.center, collider.size);
        }
    }
}