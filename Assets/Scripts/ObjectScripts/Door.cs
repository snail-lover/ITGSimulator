using UnityEngine;

public class Door : BaseInteract
{
    public float openAngle = 90f; // The angle when the door is open
    public float closeAngle = 0f; // The angle when the door is closed
    public float rotationSpeed = 5f; // Smooth rotation speed
    
    private bool isOpen = false; // Track if the door is open
    private Quaternion openRotation; // The target rotation for the open state
    private Quaternion closeRotation; // The target rotation for the closed state

    protected override void Start()
    {
        base.Start();
        // Calculate the target rotations based on localEulerAngles
        openRotation = Quaternion.Euler(0f, openAngle, 0f);
        closeRotation = Quaternion.Euler(0f, closeAngle, 0f);
        
    }

    public override void Interact()
    {
        //prints the name of the object in console when interacted with
        base.Interact();
        // Toggle between open and close
        isOpen = !isOpen;

        // Start the rotation coroutine
        StopAllCoroutines(); // In case there's already a rotation in progress
        StartCoroutine(RotateDoor(isOpen ? openRotation : closeRotation));
    }

    private System.Collections.IEnumerator RotateDoor(Quaternion targetRotation)
    {
        // Smoothly rotate the door to the target angle
        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.01f)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetRotation,
                Time.deltaTime * rotationSpeed
            );
            yield return null;
        }

        // Snap to target rotation at the end
        transform.localRotation = targetRotation;
    }
}
