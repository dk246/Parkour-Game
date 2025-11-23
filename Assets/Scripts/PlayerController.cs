using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 25f;
    public float rotationSpeed = 12f;
    public bool disableAirControl = true;

    [Header("Jump / Air")]
    public float jumpVelocity = 7f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.12f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.12f;
    public LayerMask groundLayer = ~0;
    [Range(0f, 1f)]
    public float minGroundNormalY = 0.65f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    // Core component
    private Rigidbody rb;

    // Movement
    private Vector3 inputDirection;

    // Ground & jump state
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool hasJumped = false;

    // Animator hashes (optional but faster than string lookups)
    private int isWalkHash;
    private int isJumpHash;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Rigidbody settings appropriate for a physics-driven character
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        isWalkHash = Animator.StringToHash("isWalk");
        isJumpHash = Animator.StringToHash("isJump");

        wasGrounded = true;
    }

    void Update()
    {
        HandleInput();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();
        ApplyRotation();
        ProcessJumpPhysics();
        ApplyBetterGravity();
    }

    void HandleInput()
    {
        float h = SimpleInput.GetAxisRaw("Horizontal");
        float v = SimpleInput.GetAxisRaw("Vertical");

        // Convert input into world direction (relative to camera if available)
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            inputDirection = right * h + forward * v;
        }
        else
        {
            inputDirection = new Vector3(h, 0f, v);
        }

        if (inputDirection.magnitude > 1f)
            inputDirection.Normalize();

        // Jump input -> set jump buffer (read in FixedUpdate)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
            if (animator != null)
                animator.SetBool(isJumpHash, true);
        }
    }

    void UpdateTimers()
    {
        if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    void CheckGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float checkDistance = groundCheckDistance + 0.1f;
        RaycastHit hit;
        bool hitGround = Physics.Raycast(origin, Vector3.down, out hit, checkDistance, groundLayer, QueryTriggerInteraction.Ignore);

        if (hitGround && hit.normal.y < minGroundNormalY)
            hitGround = false;

        wasGrounded = isGrounded;
        isGrounded = hitGround;

        if (isGrounded)
        {
            hasJumped = false;
            coyoteTimer = coyoteTime;
        }

        if (animator != null && wasGrounded != isGrounded)
            animator.SetBool(isJumpHash, !isGrounded);
    }

    void ApplyMovement()
    {
        if (disableAirControl && !isGrounded)
            return;

        Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 targetVel = inputDirection * moveSpeed;

        float lerpAmount = Mathf.Clamp01(acceleration * Time.fixedDeltaTime);
        Vector3 newVel = Vector3.Lerp(currentVel, targetVel, lerpAmount);

        rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
    }

    void ApplyRotation()
    {
        if (inputDirection.magnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(inputDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    void ProcessJumpPhysics()
    {
        bool canUseCoyote = coyoteTimer > 0f;
        bool bufferedJump = jumpBufferTimer > 0f;

        if (bufferedJump && (isGrounded || canUseCoyote) && !hasJumped)
        {
            DoJump();
            jumpBufferTimer = 0f;
        }
    }

    void DoJump()
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);

        hasJumped = true;
        isGrounded = false;
        coyoteTimer = 0f;

        if (animator != null)
        {
            animator.SetBool(isJumpHash, true);
            animator.SetBool(isWalkHash, false);
        }
    }

    void ApplyBetterGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            Vector3 extraGravity = Vector3.up * (Physics.gravity.y * (fallMultiplier - 1f));
            rb.AddForce(extraGravity * rb.mass, ForceMode.Force);
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            Vector3 extraGravity = Vector3.up * (Physics.gravity.y * (lowJumpMultiplier - 1f));
            rb.AddForce(extraGravity * rb.mass, ForceMode.Force);
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = inputDirection.magnitude > 0.01f;
        bool hasVerticalMotion = Mathf.Abs(rb.linearVelocity.y) > 0.15f;
        bool shouldWalk = isGrounded && !hasVerticalMotion && isMoving;

        animator.SetBool(isWalkHash, shouldWalk);
        animator.SetBool(isJumpHash, !isGrounded);
    }

    // UI-friendly jump trigger (can be wired to a Button OnClick)
    public void OnJumpButton()
    {
        bool canUseCoyote = coyoteTimer > 0f;

        if ((isGrounded || canUseCoyote) && !hasJumped)
        {
            DoJump();
        }
    }
}