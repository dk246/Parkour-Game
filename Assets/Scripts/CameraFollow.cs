using UnityEngine;

[AddComponentMenu("Camera/Third Person Camera (Mouse Orbit, Follow)")]
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    // If left empty the camera will search for the GameObject tagged "Player"
    public Transform target;
    public string playerTag = "Player";

    [Header("Distance & Height")]
    public float distance = 4f;
    public float height = 1.6f;

    [Header("Rotation")]
    public float yawSpeed = 160f;   // horizontal mouse sensitivity
    public float pitchSpeed = 80f;  // vertical mouse sensitivity
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;

    private float yaw = 0f;
    private float pitch = 10f;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null) TryFindPlayer();

        // Optional: lock cursor for mouse look. Remove if you don't want this behavior.
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
        }

        // Read mouse input
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * yawSpeed * Time.deltaTime;
        pitch -= my * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Build rotation from yaw/pitch (camera does NOT follow player's rotation)
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Desired camera position: behind the target at the set distance and height
        Vector3 desiredPosition = target.position + Vector3.up * height - (rot * Vector3.forward) * distance;

        // Smooth position
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);

        // Always look at the target (or use rotation directly so camera orientation matches yaw/pitch)
        transform.rotation = rot;
    }
}