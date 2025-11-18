using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public float jumpForce = 6f;
    public float rotationSpeed = 20f; // Increased for snappier rotation
    public float movementSmoothing = 0.1f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = -1;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;
    public Rigidbody rb;

    private Vector3 input;
    private Vector3 targetInput;
    private Vector3 smoothVelocity;
    private Vector3 currentVelocity;
    private Quaternion targetRotation;
    private bool isGrounded = true;
    private bool wasGrounded = true;
    private bool jumpRequested = false;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        // Ensure Rigidbody is set up for smooth movement
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Check ground with raycast for more reliable detection
        CheckGroundStatus();

        // Raw input from WASD/arrow keys
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Convert input to camera-relative movement
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
            Vector3 camRelative = camRight * h + camForward * v;
            targetInput = camRelative.magnitude > 1f ? camRelative.normalized : camRelative;
        }
        else
        {
            targetInput = new Vector3(h, 0f, v).normalized;
        }

        // Walk animation
        bool isWalking = targetInput.magnitude > 0.01f;
        if (animator != null) animator.SetBool("isWalk", isWalking);

        // Update jump animation based on ground status
        if (animator != null && wasGrounded != isGrounded)
        {
            animator.SetBool("isJump", !isGrounded);
            wasGrounded = isGrounded;
        }

        // Calculate target rotation (but don't apply yet - will apply in FixedUpdate)
        if (isWalking)
        {
            targetRotation = Quaternion.LookRotation(targetInput);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        // Smooth input for better movement feel
        input = Vector3.Lerp(input, targetInput, movementSmoothing * 60f * Time.fixedDeltaTime);

        // Smooth rotation in FixedUpdate to sync with movement
        if (input.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Smooth movement using Vector3.SmoothDamp
        Vector3 targetVelocity = input * moveSpeed;
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref smoothVelocity, movementSmoothing);

        // Move the player
        if (rb != null)
        {
            Vector3 newPos = rb.position + currentVelocity * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }

        // Handle jump in FixedUpdate for consistent physics
        if (jumpRequested)
        {
            Jump();
            jumpRequested = false;
        }
    }

    void CheckGroundStatus()
    {
        // Raycast downward to check ground
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            if (hit.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }

        // Additional sphere check for better detection on edges
        if (Physics.CheckSphere(transform.position, 0.2f, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void Jump()
    {
        if (rb == null) return;

        // Reset vertical velocity before jumping for consistent jump height
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
        wasGrounded = false;

        if (animator != null) animator.SetBool("isJump", true);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    return;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    isGrounded = true;
                    if (animator != null) animator.SetBool("isJump", false);
                    return;
                }
            }
        }
    }
}