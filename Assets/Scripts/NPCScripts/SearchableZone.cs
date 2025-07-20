// --- START OF FILE SearchableZone.cs ---

using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class SearchableZone : MonoBehaviour
{
    [Header("Zone Definition")]
    [Tooltip("A unique, descriptive name for this area (e.g., 'Library', 'Cafeteria').")]
    public string zoneName = "Unnamed Zone";

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