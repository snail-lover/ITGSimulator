using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MirrorCamera : MonoBehaviour
{
    [Header("Settings")]
    public Camera mainCamera;   // Drag your Main Camera here
    public Camera mirrorCamera; // Drag your Mirror Camera here
    public Transform mirrorPlane; // Drag the Plane/Quad object here

    // We use LateUpdate because your CameraFollow.cs uses LateUpdate.
    // This ensures the mirror calculates AFTER the player camera has moved.
    void LateUpdate()
    {
        if (mainCamera == null || mirrorCamera == null || mirrorPlane == null) return;

        UpdateMirrorCamera();
    }

    void UpdateMirrorCamera()
    {
        // 1. Position the Mirror Camera
        // We calculate where the camera would be if reflected across the mirror surface
        Vector3 direction = (mainCamera.transform.position - mirrorPlane.position);
        Vector3 reflectedPos = Vector3.Reflect(direction, mirrorPlane.up);
        
        mirrorCamera.transform.position = mirrorPlane.position + reflectedPos;

        // 2. Rotate the Mirror Camera
        // It needs to look at the reflection of the Main Camera's forward vector
        Vector3 mainCamForward = mainCamera.transform.forward;
        Vector3 reflectedForward = Vector3.Reflect(mainCamForward, mirrorPlane.up);
        
        // We also reflect the 'up' vector to keep the camera roll correct
        Vector3 mainCamUp = mainCamera.transform.up;
        Vector3 reflectedUp = Vector3.Reflect(mainCamUp, mirrorPlane.up);

        mirrorCamera.transform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);

        // 3. Match Camera Settings
        // Important for Zoom: Sync the FOV if your CameraFollow allows zooming!
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
        mirrorCamera.nearClipPlane = mainCamera.nearClipPlane;
        mirrorCamera.farClipPlane = mainCamera.farClipPlane;
    }
}