using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The target to follow
    public Vector3 offset = new Vector3(0, 5, -10); // Initial offset from the target
    public float smoothSpeed = 0.125f; // Smooth movement speed

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f; // Speed for camera rotation when holding middle mouse button
    private float currentYaw; // Tracks the rotation around the target (yaw)

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f; // Speed of zoom
    public float minZoom = 2f; // Minimum zoom distance
    public float maxZoom = 15f; // Maximum zoom distance
    private float currentZoom; // Tracks current zoom level

    void Start()
    {
        currentZoom = offset.magnitude; // Set initial zoom based on the offset's distance
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Handle Camera Zoom
        HandleZoom();

        // Handle Camera Rotation
        HandleRotation();

        // Update the Camera Position and Rotation
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        // Use scroll wheel to adjust zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom); // Clamp the zoom
    }

    void HandleRotation()
    {
        // Rotate when holding the middle mouse button
        if (Input.GetMouseButton(2)) // Middle mouse button held
        {
            float mouseX = Input.GetAxis("Mouse X"); // Horizontal mouse movement
            currentYaw += mouseX * rotationSpeed; // Update yaw (rotation around Y-axis)
        }
    }

    void UpdateCameraPosition()
    {
        // Calculate the new offset based on the current zoom and yaw
        Quaternion rotation = Quaternion.Euler(0f, currentYaw, 0f); // Rotate around the Y-axis
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom); // Apply zoom and keep vertical offset

        // Smoothly move the camera to the desired position
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Make the camera look at the target
        transform.LookAt(target); // Adjust for vertical height
    }
}
