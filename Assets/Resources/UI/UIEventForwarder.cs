using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// This script intercepts UI events (like scroll and drag) and forwards them 
/// to the parent ScrollRect. This allows a ScrollView to function correctly 
/// even when the pointer is over a child element (like a button).
/// </summary>
public class UIEventForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private ScrollRect scrollRect;

    private void Awake()
    {
        // Find the parent ScrollRect component.
        scrollRect = GetComponentInParent<ScrollRect>();
    }

    // --- Drag Events ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            scrollRect.OnBeginDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            scrollRect.OnDrag(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            scrollRect.OnEndDrag(eventData);
        }
    }

    // --- Scroll Event (Mouse Wheel) ---

    public void OnScroll(PointerEventData eventData)
    {
        if (scrollRect != null)
        {
            scrollRect.OnScroll(eventData);
        }
    }
}