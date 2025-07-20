// --- START OF FILE HighlightOnTarget.cs ---
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(IClickable))] // It can only be highlighted if it's clickable
public class HighlightOnTarget : MonoBehaviour
{
    private Renderer objectRenderer;
    private IClickable clickableComponent;

    // Using a MaterialPropertyBlock is much more efficient than creating new Material instances.
    private MaterialPropertyBlock propBlock;

    // We cache the shader property IDs for performance.
    private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
        clickableComponent = GetComponent<IClickable>();
        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        // Subscribe to the events when this component becomes active
        PointAndClickMovement.OnNewTargetSet += HandleNewTarget;
        PointAndClickMovement.OnTargetCleared += HandleTargetCleared;

        // Check initial state in case the game starts with this as a target
        HandleInitialState();
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        PointAndClickMovement.OnNewTargetSet -= HandleNewTarget;
        PointAndClickMovement.OnTargetCleared -= HandleTargetCleared;
    }

    private void HandleInitialState()
    {
        // This is useful if the game is loaded and this object was the last target
        if (PointAndClickMovement.currentTarget == clickableComponent)
        {
            SetHighlight(true);
        }
        else
        {
            SetHighlight(false);
        }
    }

    private void HandleNewTarget(IClickable newTarget)
    {
        // Is the new target us?
        if (newTarget == clickableComponent)
        {
            SetHighlight(true);
        }
        else // It's a different target, so we should not be highlighted
        {
            SetHighlight(false);
        }
    }

    private void HandleTargetCleared()
    {
        // The target was cleared (e.g., clicked ground), so we are no longer the target.
        SetHighlight(false);
    }

    private void SetHighlight(bool isHighlighted)
    {
        // Get the current properties from the renderer
        objectRenderer.GetPropertyBlock(propBlock);

        // Set our outline width property
        propBlock.SetFloat(OutlineWidthID, isHighlighted ? 0.01f : 0f); // Or use a public variable for width

        // Apply the modified properties back to the renderer
        objectRenderer.SetPropertyBlock(propBlock);
    }
}