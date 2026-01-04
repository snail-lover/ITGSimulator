using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Game.Core; // Assuming this is where GlobalOcclusionManager lives

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Core Settings")]
    public Transform target;
    public Vector3 initialOffsetFromTarget = new Vector3(0, 15, -5);
    public float positionSmoothFactor = 10f;
    public enum MouseButton { Left = 0, Right = 1, Middle = 2 }

    [Header("Input")]
    public MouseButton rotateButton = MouseButton.Middle;

    [Header("Rotation Settings")]
    public float yawSpeed = 5f;
    public float pitchSpeed = 3f;
    public float minPitch = 10f;
    public float maxPitch = 85f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    private float currentZoom;

    [Header("LookAt Settings")]
    public Vector3 targetOcclusionCheckOffset = Vector3.up * 1.0f;

    [Header("Smoothing")]
    public float targetPositionSmoothFactor = 12f;
    private Vector3 smoothedTargetPos;

    // Internal state
    private float currentYaw;
    private float currentPitch;

    // Manual-control / transition support
    [Header("Manual Control")]
    public bool isManuallyControlled = false;
    private Coroutine _transitionCoroutine;
    private Vector3 _manualTargetPosition;
    private Quaternion _manualTargetRotation;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentPitch = transform.eulerAngles.x;
        currentYaw = transform.eulerAngles.y;
    }

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: Target not assigned!", this);
            enabled = false;
            return;
        }

        transform.position = target.position + initialOffsetFromTarget;
        smoothedTargetPos = target.position;
        currentZoom = Mathf.Clamp((transform.position - target.position).magnitude, minZoom, maxZoom);

        Vector3 euler = transform.rotation.eulerAngles;
        currentYaw = euler.y;
        currentPitch = euler.x;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        var gom = GlobalOcclusionManager.Instance;
        if (gom != null)
        {
            if (gom.playerTarget == null) gom.playerTarget = target;
            if (gom.gameCamera == null) gom.gameCamera = GetComponent<Camera>() ?? Camera.main;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, target.position, targetPositionSmoothFactor * Time.deltaTime);

        if (isManuallyControlled) return;

        HandleZoom();
        HandleRotation();
        UpdateCameraPosition();
    }

    void HandleZoom()
    {
        // REMOVED: Draggable reference caused error CS0103

        // Block zoom while UI is dragging
        if (UIInputState.IsDragging) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    void HandleRotation()
    {
        if (Input.GetMouseButton((int)rotateButton))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw += mouseX * yawSpeed;
            currentPitch -= mouseY * pitchSpeed;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredOffset = rotation * new Vector3(0f, 0f, -currentZoom);
        Vector3 desiredPosition = smoothedTargetPos + desiredOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothFactor * Time.deltaTime);
        transform.LookAt(smoothedTargetPos + targetOcclusionCheckOffset);
    }

    public Coroutine TransitionToViewCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _manualTargetPosition = targetPosition;
        _manualTargetRotation = targetRotation;
        _transitionCoroutine = StartCoroutine(DoTransition(targetPosition, targetRotation, duration));
        return _transitionCoroutine;
    }

    private IEnumerator DoTransition(Vector3 endPosition, Quaternion endRotation, float duration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (!isManuallyControlled)
            {
                _transitionCoroutine = null;
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        transform.position = endPosition;
        transform.rotation = endRotation;
        _transitionCoroutine = null;
    }

    public void SetManualControl(bool manual)
    {
        isManuallyControlled = manual;
        if (!manual && _transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }
    }
}