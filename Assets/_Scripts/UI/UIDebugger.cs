// UIDebugger.cs
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Create a pointer event data to pass to the raycaster
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            // Create a list to hold all the raycast results
            List<RaycastResult> results = new List<RaycastResult>();

            // Raycast against the UI
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                // The first result (at index 0) is the topmost UI element hit.
                Debug.Log($"<color=green>UI DEBUGGER HIT:</color> {results[0].gameObject.name}", results[0].gameObject);
            }
            else
            {
                Debug.Log("<color=orange>UI DEBUGGER HIT: Nothing.</color> Click went to the 3D world.");
            }
        }
    }
}