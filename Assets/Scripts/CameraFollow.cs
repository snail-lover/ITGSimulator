using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The target to follow
    public Vector3 offset = new Vector3(0, 15, -5); // Adjusted for top-down view
    public float smoothSpeed = 0.125f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;
    private float currentYaw;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f; // Adjust for top-down limits
    public float maxZoom = 20f;
    private float currentZoom;

    void Start()
    {
        currentZoom = offset.magnitude; // Set initial zoom
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom); // Clamp the zoom
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton(2)) // Rotate with middle mouse button
        {
            float mouseX = Input.GetAxis("Mouse X");
            currentYaw += mouseX * rotationSpeed;
        }
    }

    void UpdateCameraPosition()
    {
        // Adjust the rotation to tilt the camera downward
        Quaternion rotation = Quaternion.Euler(45f, currentYaw, 0f); // 45° down, yaw rotates around the Y-axis
        Vector3 rotatedOffset = rotation * new Vector3(0, offset.y, -currentZoom); // Adjust for zoom and height

        // Smoothly move to the desired position
        Vector3 desiredPosition = target.position + rotatedOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Make the camera look at the target
        transform.LookAt(target);
    }
}
