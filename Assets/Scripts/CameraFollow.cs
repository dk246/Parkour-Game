using UnityEngine;

[AddComponentMenu("Camera/Third Person Camera (Mouse Orbit, Follow)")]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Distance & Height")]
    public float distance = 4f;
    public float height = 1.6f;

    [Header("Rotation")]
    public float yawSpeed = 240f;   // Increased for faster horizontal rotation
    public float pitchSpeed = 120f;  // Increased for faster vertical rotation
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float positionSmoothing = 0.05f; // Faster position smoothing
    public float rotationSmoothing = 0.08f; // Rotation smoothing

    private float yaw = 0f;
    private float pitch = 10f;
    private Vector3 positionVelocity = Vector3.zero;
    private Vector3 rotationVelocity = Vector3.zero;
    private Vector3 currentRotation;

    void Start()
    {
        if (target == null) TryFindPlayer();

        // Initialize rotation
        currentRotation = new Vector3(pitch, yaw, 0f);

        // Optional: lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void TryFindPlayer()
    {
        var go = GameObject.FindWithTag(playerTag);
        if (go != null) target = go.transform;
    }

    void Update()
    {
        if (target == null)
        {
            TryFindPlayer();
            return;
        }

        // Read mouse input with deltaTime for frame-rate independent rotation
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * yawSpeed * Time.deltaTime;
        pitch -= my * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Smooth rotation for camera
        Vector3 targetRotation = new Vector3(pitch, yaw, 0f);
        currentRotation = Vector3.SmoothDamp(currentRotation, targetRotation, ref rotationVelocity, rotationSmoothing);

        // Build rotation from smoothed yaw/pitch
        Quaternion rot = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);

        // Desired camera position
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition - (rot * Vector3.forward) * distance;

        // Smooth position with faster smoothing
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothing);

        // Set camera rotation
        transform.rotation = rot;
    }
}