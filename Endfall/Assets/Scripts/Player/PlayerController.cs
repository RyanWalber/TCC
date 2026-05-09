using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public int maxJumps = 2;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public float dashSpeed = 22f;
    public float dashDuration = 0.15f;

    private Rigidbody2D rb;
    private float horizontal;
    private bool isGrounded;
    private int jumpsLeft;
    private bool isDashing;
    private float dashTimer;
    private float savedGravity;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        savedGravity = rb.gravityScale;
    }

    void Update()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                rb.gravityScale = savedGravity;
            }
            return;
        }

        horizontal = Input.GetAxisRaw("Horizontal");

        if (horizontal > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontal < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            jumpsLeft = maxJumps;
        }

        if (Input.GetKeyDown(KeyCode.Space) && jumpsLeft > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            jumpsLeft--;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isDashing = true;
            dashTimer = dashDuration;
            rb.gravityScale = 0;
            rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        }
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        }
    }
}