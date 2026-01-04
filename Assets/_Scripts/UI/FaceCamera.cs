// --- START OF FILE FaceCamera.cs ---

using UnityEngine;

/// <summary>
/// A simple component that makes the GameObject it's attached to always face the main camera.
/// This is often called "Billboarding".
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Camera _mainCamera;

    void Awake()
    {
        // Find and cache the main camera on awake for efficiency.
        _mainCamera = Camera.main;
    }

    // We use LateUpdate because it runs after all other updates,
    // including the camera's movement. This prevents any visual jitter.
    void LateUpdate()
    {
        // If we don't have a reference to the camera (e.g., it was destroyed and recreated), try to find it again.
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return; // Exit if no camera is found.
        }

        // Calculate the rotation needed to look at the camera.
        // We want the bark's "forward" direction (its Z-axis) to point directly away from the camera.
        Quaternion cameraRotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);

        // Apply the calculated rotation to this GameObject.
        transform.rotation = cameraRotation;
    }
}