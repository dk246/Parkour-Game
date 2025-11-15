using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 6f;
    public float rotationSpeed = 15f;

    [Header("References")]
    // If left empty the script will use Camera.main when available.
    public Transform cameraTransform;

    // Animator & Rigidbody will be auto-filled if left null in the inspector
    public Animator animator;
    public Rigidbody rb;

    private Vector3 input;
    private bool isGrounded = true;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Raw input from WASD/arrow keys (X = right, Z = forward)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Convert input to camera-relative movement (so "W" goes where the camera looks)
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 camRelative = camRight * h + camForward * v;
            input = camRelative.magnitude > 1f ? camRelative.normalized : camRelative;
        }
        else
        {
            input = new Vector3(h, 0f, v).normalized;
        }

        // Walk animation
        bool isWalking = input.magnitude > 0.01f;
        if (animator != null) animator.SetBool("isWalk", isWalking);

        // Rotate character to face movement direction (only when walking)
        if (isWalking)
        {
            // Smoothly rotate toward movement direction
            Quaternion targetRot = Quaternion.LookRotation(input);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        // Move the player using Rigidbody (keeps physics consistent)
        if (rb != null)
        {
            Vector3 newPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    void Jump()
    {
        if (rb == null) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
        if (animator != null) animator.SetBool("isJump", true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // When player touches the ground (check collision normal to avoid walls)
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            if (animator != null) animator.SetBool("isJump", false);
        }
    }
}