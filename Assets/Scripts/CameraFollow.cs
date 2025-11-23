using UnityEngine;

[AddComponentMenu("Camera/Smooth Third Person Camera")]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string playerTag = "Player";
    public Vector3 offset = new Vector3(0f, 1.6f, 0f);

    [Header("Distance & Zoom")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float zoomSpeed = 2f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 70f;
    public bool invertY = false;

    [Header("Rotation Input")]
    [Tooltip("Mouse button index that enables rotation while held. 0 = left, 1 = right, 2 = middle")]
    public int rotateMouseButton = 1;
    [Tooltip("When true, cursor will be locked while rotating and unlocked when released.")]
    public bool lockCursorWhileRotating = true;

    [Header("Smoothing (Lower = Smoother)")]
    [Range(0.01f, 0.3f)]
    public float positionDamping = 0.1f;
    [Range(0.01f, 0.3f)]
    public float rotationDamping = 0.1f;

    [Header("Collision")]
    public bool checkCollision = true;
    public float collisionBuffer = 0.3f;
    public LayerMask collisionLayers = -1;

    // Current rotation angles
    private float currentYaw;
    private float currentPitch;

    // Target rotation angles (what we're rotating towards)
    private float targetYaw;
    private float targetPitch;

    // Current distance from target
    private float currentDistance;

    // Smoothing velocities (kept for compatibility)
    private Vector3 velocityPosition;
    private float velocityYaw;
    private float velocityPitch;
    private float velocityDistance;

    void Start()
    {
        if (target == null)
            FindTarget();

        // Initialize rotation values from the camera's current orientation
        Vector3 angles = transform.eulerAngles;
        currentYaw = targetYaw = angles.y;
        currentPitch = targetPitch = angles.x;
        currentDistance = distance;

        // Do not lock cursor by default. Locking happens only while rotating (if enabled).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void FindTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
            target = player.transform;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }

        // Only update camera when game is running
        if (Time.timeScale <= 0f) return;

        HandleInput();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        // Zoom with scroll wheel (always allowed)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        // Rotate only while the configured mouse button is held down
        if (Input.GetMouseButton(rotateMouseButton))
        {
            // Optionally lock cursor while rotating for a better mouselook feel
            if (lockCursorWhileRotating && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            targetYaw += mouseX;
            targetPitch += (invertY ? mouseY : -mouseY);
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

            // Smoothly approach the target angles
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref velocityYaw, rotationDamping);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref velocityPitch, rotationDamping);
        }
        else
        {
            // Release cursor lock on mouse up (if we locked it)
            if (lockCursorWhileRotating && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Even when not rotating, smoothly approach target angles so camera stays stable.
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref velocityYaw, rotationDamping);
            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref velocityPitch, rotationDamping);
        }
    }

    void UpdateCameraPosition()
    {
        currentDistance = Mathf.SmoothDamp(currentDistance, distance, ref velocityDistance, 0.1f);
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 focusPoint = target.position + offset;
        Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * currentDistance);

        if (checkCollision)
        {
            Vector3 direction = desiredPosition - focusPoint;
            float targetDistance = direction.magnitude;

            if (targetDistance > 0.001f)
            {
                RaycastHit hit;
                if (Physics.Raycast(focusPoint, direction.normalized, out hit, targetDistance, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    desiredPosition = hit.point - direction.normalized * collisionBuffer;
                }
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocityPosition, positionDamping);
        transform.LookAt(focusPoint);
    }

    void OnDisable()
    {
        // Ensure cursor unlocked when component is disabled
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // If app lost focus, release the lock to avoid trapping the cursor
        if (!hasFocus && Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}