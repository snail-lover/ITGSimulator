using UnityEngine;
using UnityEngine.AI;

public class Door : BaseInteract
{
    public float openAngle = 90f; // The angle when the door is open
    public float closeAngle = 0f; // The angle when the door is closed
    public float rotationSpeed = 5f; // Smooth rotation speed
    
    private bool isOpen = false; // Track if the door is open
    private Quaternion openRotation; // The target rotation for the open state
    private Quaternion closeRotation; // The target rotation for the closed state
    private NavMeshObstacle navObstacle;

    protected  void Start()
    {
        // Calculate the target rotations based on localEulerAngles
        openRotation = Quaternion.Euler(0f, openAngle, 0f);
        closeRotation = Quaternion.Euler(0f, closeAngle, 0f);
        navObstacle = GetComponent<NavMeshObstacle>();
    }

    public override void Interact()
    {
        //prints the name of the object in console when interacted with
        // Toggle between open and close
        isOpen = !isOpen;

 // Toggle NavMeshObstacle carving based on door state
         if (navObstacle != null)
         {
             navObstacle.carving = !isOpen; // Enable carving only when the door is closed
         }


        // Start the rotation coroutine
        StopAllCoroutines(); // In case there's already a rotation in progress
        StartCoroutine(RotateDoor(isOpen ? openRotation : closeRotation));
        base.Interact();
        
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
