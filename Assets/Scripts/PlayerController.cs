using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(CapsuleCollider))]
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
    public float jumpGroundIgnoreTime = 0.2f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.12f;
    public LayerMask groundLayer = ~0;
    [Range(0f, 1f)]
    public float minGroundNormalY = 0.65f;
    public float groundHeightTolerance = 0.05f;

    [Header("Landing confirmation")]
    public float groundConfirmTime = 0.15f;
    public float groundVelocityThreshold = 0.5f;

    [Header("References")]
    public Transform cameraTransform;
    public Animator animator;

    // Core components
    private Rigidbody rb;
    private BoxCollider box;

    // Movement
    private Vector3 inputDirection;

    // Ground state
    private bool isGrounded;
    private bool wasGrounded;

    // Jump timers (kept as fields, but simplified logic below)
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float jumpGroundIgnoreTimer;
    private float groundConfirmTimer;

    // Jump flags
    private bool hasJumped = false;
    private bool wasInAir = false;

    // Animation hashes
    private int isWalkHash;
    private int isJumpHash;

    void Awake()
    {
        // Cache components
        rb = GetComponent<Rigidbody>();
        //box = GetComponent<CapsuleCollider>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Rigidbody setup - keep rotation locked for a character
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        // Animator parameter hashes (optional but common practice)
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
        // Simple ground check
        if (jumpGroundIgnoreTimer > 0f)
            jumpGroundIgnoreTimer -= Time.fixedDeltaTime;

        CheckGround();

        ApplyMovement();
        ApplyRotation();
        ProcessJumpPhysics();
        ApplyBetterGravity();

        // Keep PreventWallSticking function present but simplified for beginners
        if (!isGrounded && jumpGroundIgnoreTimer > 0f)
            PreventWallSticking();
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

        // Simple jump input handling: set jump buffer timer so FixedUpdate can pick it up
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;

            if (animator != null)
                animator.SetBool(isJumpHash, true);
        }
    }

    void UpdateTimers()
    {
        // Keep simple timers (these exist but behavior is simplified)
        if (coyoteTimer > 0f)
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    void CheckGround()
    {
        // Simpler ground check: cast a short ray down from the character position
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float checkDistance = groundCheckDistance + 0.1f;
        RaycastHit hit;
        bool hitGround = Physics.Raycast(origin, Vector3.down, out hit, checkDistance, groundLayer, QueryTriggerInteraction.Ignore);

        if (hitGround)
        {
            // Ensure the surface is not too steep
            if (hit.normal.y < minGroundNormalY)
                hitGround = false;
        }

        wasGrounded = isGrounded;
        isGrounded = hitGround;

        if (isGrounded)
        {
            // Reset jump flag so player can jump again after landing
            hasJumped = false;
            coyoteTimer = coyoteTime;
        }

        // Keep simple animator update
        if (animator != null && wasGrounded != isGrounded)
            animator.SetBool(isJumpHash, !isGrounded);
    }

    void ApplyMovement()
    {
        // Optionally prevent air control (simple check)
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
        // Very simple jump condition: if jump was pressed recently and we're grounded
        bool canJump = jumpBufferTimer > 0f && isGrounded && !hasJumped;
        if (canJump)
        {
            DoJump();
        }
    }

    void DoJump()
    {
        // Reset vertical velocity for consistent jumps
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);

        // Basic jump state updates
        hasJumped = true;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        isGrounded = false;

        if (animator != null)
        {
            animator.SetBool(isJumpHash, true);
            animator.SetBool(isWalkHash, false);
        }
    }

    void ApplyBetterGravity()
    {
        // Apply a bit more gravity when falling so jumps feel snappier.
        // This is kept simple for beginners.
        if (rb.linearVelocity.y < 0f)
        {
            Vector3 extraGravity = Vector3.up * (Physics.gravity.y * (fallMultiplier - 1f));
            rb.AddForce(extraGravity * rb.mass, ForceMode.Force);
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            // Optional: make releasing jump earlier produce a shorter jump
            Vector3 extraGravity = Vector3.up * (Physics.gravity.y * (lowJumpMultiplier - 1f));
            rb.AddForce(extraGravity * rb.mass, ForceMode.Force);
        }
    }

    // Kept as a simple stub so function names stay the same for any external calls.
    void PreventWallSticking()
    {
        // Beginner-friendly version: do nothing special here.
        // Complex logic removed to keep code simple.
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

    void OnCollisionEnter(Collision collision)
    {
        // Simple ground collision check: if contact normal is mostly up, consider grounded
        if (((1 << collision.gameObject.layer) & groundLayer) == 0) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > minGroundNormalY)
            {
                isGrounded = true;
                if (animator != null)
                    animator.SetBool(isJumpHash, false);
                break;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) == 0) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > minGroundNormalY)
            {
                isGrounded = true;
                break;
            }
        }
    }

    // ---------------------------
    // UI integration
    // ---------------------------

    // Public method you can wire to a Unity UI Button (OnClick) to trigger a jump.
    // Using the button will set the jump buffer just like pressing Space.
    public void OnJumpButton()
    {
        jumpBufferTimer = jumpBufferTime;
        if (animator != null)
            animator.SetBool(isJumpHash, true);
    }


}