using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Visuals")]
    public Animator animator; // <-- ASSIGN THIS IN INSPECTOR (The Child Model)

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 10f;

    private Rigidbody rb;
    private Vector3 _inputDirection;
    private float _currentSpeedModifier = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnEnable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave += CaptureState;
            WorldDataManager.Instance.OnAfterLoad += RestoreState;
        }
    }

    void OnDisable()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.OnBeforeSave -= CaptureState;
            WorldDataManager.Instance.OnAfterLoad -= RestoreState;
        }
    }

    public void SetMoveDirection(Vector3 direction)
    {
        _inputDirection = direction;
    }

    void Update()
    {
        // --- ANIMATION UPDATE ---
        // We do this in Update (per frame) for smoothness
        if (animator != null)
        {
            // Calculate actual speed based on Rigidbody velocity
            // We ignore Y (jumping/falling) for the walk animation usually
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;

            // "Speed" matches the float parameter name you created in Phase 4
            // 0.1f is damp time to make the animation transition smooth
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        Move();
        Turn();
    }

    private void Move()
    {
        // 1. Calculate the target speed
        // If no keys are pressed, _inputDirection is (0,0,0), so targetVelocity becomes 0.
        Vector3 targetVelocity = _inputDirection * (moveSpeed * _currentSpeedModifier);

        // 2. Preserve Gravity
        // We only want to control X and Z. We must keep the current Y (falling speed).
        Vector3 velocityChange = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        // 3. Apply directly
        // Previously we had an "if (input < 0.1) return" here. 
        // Removing that ensures that when input is 0, we FORCE the velocity to becomes 0.

        // Optional: If you want it slightly smoother (less jerky) but still tight, 
        // use Vector3.MoveTowards instead of direct assignment:
        // rb.velocity = Vector3.MoveTowards(rb.velocity, velocityChange, 20f * Time.fixedDeltaTime);

        // For maximum responsiveness (Start/Stop instantly), use this:
        rb.linearVelocity = velocityChange;
    }

    private void Turn()
    {
        if (_inputDirection.magnitude < 0.1f) return;

        Quaternion targetRotation = Quaternion.LookRotation(_inputDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime);
    }

    // --- SAVE / LOAD SYSTEM ---
    private void CaptureState()
    {
        if (WorldDataManager.Instance != null)
        {
            WorldDataManager.Instance.saveData.playerState.lastKnownPosition = transform.position;
        }
    }

    private void RestoreState()
    {
        if (WorldDataManager.Instance != null)
        {
            Vector3 savedPosition = WorldDataManager.Instance.saveData.playerState.lastKnownPosition;
            if (savedPosition != Vector3.zero)
            {
                rb.position = savedPosition;
            }
        }
    }
}