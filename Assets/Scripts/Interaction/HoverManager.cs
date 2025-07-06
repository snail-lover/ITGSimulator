using UnityEngine;
using UnityEngine.EventSystems; // Required for checking if mouse is over UI

public class HoverManager : MonoBehaviour
{
    public float maxHoverDistance = 100f; // Max distance for raycast
    public LayerMask hoverableLayers;    // Layers to check for IHoverable objects

    private IHoverable currentHoveredObject;
    private IHoverable lastHoveredObject;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("HoverManager: Main Camera not found! Hovering will not work.");
            enabled = false; // Disable this script if no camera
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Optional: Prevent hover detection if mouse is over a UI element (like an inventory panel)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // If mouse is over UI, ensure any previously hovered 3D object has its hover hidden
            if (lastHoveredObject != null)
            {
                lastHoveredObject.HideHover();
                lastHoveredObject = null;
            }
            currentHoveredObject = null; // Reset current hovered
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        currentHoveredObject = null; // Reset at the start of the frame

        if (Physics.Raycast(ray, out hit, maxHoverDistance, hoverableLayers))
        {
            // Check if the hit object (or its parent, etc.) has an IHoverable component
            IHoverable hoverable = hit.collider.GetComponentInParent<IHoverable>();
            if (hoverable != null)
            {
                currentHoveredObject = hoverable;
            }
        }

        // Manage state changes
        if (currentHoveredObject != lastHoveredObject)
        {
            if (lastHoveredObject != null)
            {
                lastHoveredObject.HideHover();
            }
            if (currentHoveredObject != null)
            {
                currentHoveredObject.WhenHovered();
            }
            lastHoveredObject = currentHoveredObject;
        }
    }
}