using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))] // Ensure LineRenderer is present
public class ClickVisualizer : MonoBehaviour
{
    [Header("Marker Settings")]
    public GameObject clickMarkerPrefab; // Assign a prefab for the visual marker (e.g., a small cylinder, sphere, or a decal projector)
    public float markerYOffset = 0.05f; // Small offset to prevent Z-fighting with the ground
    private GameObject currentMarkerInstance;

    [Header("Path Line Settings")]
    public bool drawPathLine = true;
    [Tooltip("Material for the path line. If null, a default one will be created.")]
    public Material pathLineMaterial;
    public Color pathLineColor = Color.yellow;
    public float pathLineWidth = 0.1f;

    private LineRenderer lineRenderer;
    private NavMeshAgent playerAgent;
    private Vector3 currentDestination;
    private bool isVisualsActive = false;

    void Start()
    {
        // Get the LineRenderer component
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();

        // Attempt to get the player agent via PointAndClickMovement instance
        if (PointAndClickMovement.Instance != null)
        {
            playerAgent = PointAndClickMovement.Instance.GetPlayerAgent();
        }

        if (playerAgent == null)
        {
            Debug.LogError("[ClickVisualizer] Player NavMeshAgent not found. Make sure PointAndClickMovement is initialized and has an agent. Disabling visualizer.");
            enabled = false; // Disable this script if player agent can't be found
            return;
        }

        // Subscribe to events from PointAndClickMovement
        PointAndClickMovement.OnMoveCommandIssued += HandleMoveCommand;
        PointAndClickMovement.OnMovementStoppedOrCancelled += HandleMovementStopOrCancel;

        HideVisuals(); // Start with visuals hidden
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        PointAndClickMovement.OnMoveCommandIssued -= HandleMoveCommand;
        PointAndClickMovement.OnMovementStoppedOrCancelled -= HandleMovementStopOrCancel;

        // Clean up marker instance if it exists
        if (currentMarkerInstance != null)
        {
            Destroy(currentMarkerInstance);
        }
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("[ClickVisualizer] LineRenderer component missing!");
            drawPathLine = false; // Cannot draw line without renderer
            return;
        }

        if (pathLineMaterial == null)
        {
            // Create a simple unlit material for the line
            Shader unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader != null)
            {
                pathLineMaterial = new Material(unlitShader);
                pathLineMaterial.color = pathLineColor;
            }
            else
            {
                Debug.LogWarning("[ClickVisualizer] 'Unlit/Color' shader not found. Line may not render correctly. Please assign a material.");
                // Fallback to a default particle material if Unlit/Color isn't available (less ideal)
                pathLineMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            }
        }
        lineRenderer.material = pathLineMaterial;
        lineRenderer.startColor = pathLineColor;
        lineRenderer.endColor = pathLineColor;
        lineRenderer.startWidth = pathLineWidth;
        lineRenderer.endWidth = pathLineWidth;
        lineRenderer.positionCount = 0; // Start with no points
        lineRenderer.enabled = false;
    }

    private void HandleMoveCommand(Vector3 destination)
    {
        if (playerAgent == null) return;

        currentDestination = destination;
        isVisualsActive = true;

        // Show/Move Click Marker
        if (clickMarkerPrefab != null)
        {
            if (currentMarkerInstance == null)
            {
                currentMarkerInstance = Instantiate(clickMarkerPrefab, destination + Vector3.up * markerYOffset, Quaternion.identity);
            }
            else
            {
                currentMarkerInstance.transform.position = destination + Vector3.up * markerYOffset;
                currentMarkerInstance.SetActive(true);
            }
        }

        // Enable LineRenderer for updating in Update()
        if (drawPathLine && lineRenderer != null)
        {
            lineRenderer.enabled = true;
            // Initial path draw for responsiveness, will be refined in Update
            UpdatePathLine();
        }
    }

    private void HandleMovementStopOrCancel()
    {
        HideVisuals();
    }

    void Update()
    {
        if (!isVisualsActive || playerAgent == null)
        {
            // This failsafe is still good. If isVisualsActive is false, ensure they are hidden.
            if (lineRenderer != null && lineRenderer.enabled)
            {
                // Debug.Log("[ClickVisualizer] Update: isVisualsActive is false, ensuring visuals hidden.");
                HideVisuals();
            }
            return;
        }

        if (!playerAgent.pathPending && !playerAgent.hasPath)
        {
            // Check if we are "significantly" far from the intended destination.
            // If we are very close, an arrival event *should* have fired or will fire.
            if (currentDestination != Vector3.zero && // Ensure currentDestination was set
                (playerAgent.transform.position - currentDestination).sqrMagnitude > (playerAgent.stoppingDistance * playerAgent.stoppingDistance * 1.5f)) // 1.2 * stoppingDistance squared
            {
                Debug.LogWarning("[ClickVisualizer] Update: Player agent has no path and is not pending, and far from dest. Hiding visuals as safety.");
                HideVisuals();
                return;
            }
        }

        // Update Path Line if visuals are active and line drawing is enabled
        if (drawPathLine && lineRenderer != null && lineRenderer.enabled)
        {
            UpdatePathLine();
        }
    }

    private void UpdatePathLine()
    {
        if (playerAgent.pathPending || !playerAgent.hasPath || playerAgent.path.corners.Length < 1)
        {
            // If path is being calculated, not valid, or has no corners, draw a straight line from player to destination or hide
            if ((playerAgent.transform.position - currentDestination).sqrMagnitude > 0.01f) // Only draw if not at destination
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, playerAgent.transform.position + Vector3.up * 0.1f); // Slightly above player feet
                lineRenderer.SetPosition(1, currentDestination + Vector3.up * 0.1f); // Slightly above ground
            }
            else
            {
                lineRenderer.positionCount = 0; // Hide if too close
            }
            return;
        }

        // Path exists and has corners
        var pathCorners = playerAgent.path.corners;
        lineRenderer.positionCount = pathCorners.Length + 1;

        // Start the line from the player's current position
        lineRenderer.SetPosition(0, playerAgent.transform.position + Vector3.up * 0.1f); // Start slightly above player's feet for visibility

        // Set the rest of the points from the NavMeshAgent's path corners
        for (int i = 0; i < pathCorners.Length; i++)
        {
            lineRenderer.SetPosition(i + 1, pathCorners[i] + Vector3.up * 0.1f); // Draw points slightly above ground
        }
    }

    private void HideVisuals()
    {
        isVisualsActive = false;
        if (currentMarkerInstance != null)
        {
            currentMarkerInstance.SetActive(false);
        }
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0; // Clear points
        }
    }
}