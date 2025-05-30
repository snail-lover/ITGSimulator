using UnityEngine;
using UnityEngine.AI;

public class Door : BaseInteract
{
    public float openAngleY = 90f;  // More descriptive: this is the target Y Euler angle
    public float closeAngleY = 0f; // More descriptive: this is the target Y Euler angle
    public float rotationSpeed = 5f;

    private bool isOpen = false;
    private Quaternion openRotationTarget; // Renamed for clarity
    private Quaternion closeRotationTarget; // Renamed for clarity
    private NavMeshObstacle navObstacle;

    // Store the initial non-Y rotations
    private float initialLocalEulerX;
    private float initialLocalEulerZ;

    protected void Start()
    {
        // Get the door's current local Euler angles when the game starts
        Vector3 initialLocalEuler = transform.localEulerAngles;
        initialLocalEulerX = initialLocalEuler.x;
        initialLocalEulerZ = initialLocalEuler.z;

        // Calculate the target rotations, preserving the initial X and Z,
        // and only using openAngleY and closeAngleY for the Y component.
        openRotationTarget = Quaternion.Euler(initialLocalEulerX, openAngleY, initialLocalEulerZ);
        closeRotationTarget = Quaternion.Euler(initialLocalEulerX, closeAngleY, initialLocalEulerZ);

        navObstacle = GetComponent<NavMeshObstacle>();

        // Optional: Snap to the initial closed state correctly
        // This ensures the door starts visually as defined by closeAngleY
        // and its existing X and Z rotations.
        if (!isOpen) // If you want it to start closed
        {
            transform.localRotation = closeRotationTarget;
        }
        else // If you want it to start open
        {
            transform.localRotation = openRotationTarget;
        }
    }

    public override void Interact()
    {
        isOpen = !isOpen;

        if (navObstacle != null)
        {
            navObstacle.carving = !isOpen;
        }

        StopAllCoroutines();
        // Use the correctly calculated targets
        StartCoroutine(RotateDoor(isOpen ? openRotationTarget : closeRotationTarget));
        // base.Interact(); // Call base.Interact() if it does something important.
        // If it just prints name, it's fine here or at the start.
    }

    private System.Collections.IEnumerator RotateDoor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.01f)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }
        transform.localRotation = targetRotation;
    }
}