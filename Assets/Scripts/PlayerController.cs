using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SimpleCharacterController : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float jumpForce = 6f;

    public Animator animator;
    public Rigidbody rb;

    private Vector3 input;
    private bool isGrounded = true;

    void Update()
    {
        
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        input = new Vector3(h, 0, v).normalized;

        // Walk Animation 
        bool isWalking = input.magnitude > 0;
        animator.SetBool("isWalk", isWalking);

        
        if (isWalking)
        {
            transform.forward = input;
        }

        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    void FixedUpdate()
    {
        //Move the Player
        rb.MovePosition(rb.position + input * moveSpeed * Time.fixedDeltaTime);
    }

    void Jump()
    {
        // Apply upward force for a jump
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
        animator.SetBool("isJump", !isGrounded);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // When player touched the ground
        if (collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            animator.SetBool("isJump", !isGrounded);
        }
    }
}