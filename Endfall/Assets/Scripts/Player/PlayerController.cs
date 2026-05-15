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
    private Animator anim;
    private float horizontal;
    private bool isGrounded;
    private int jumpsLeft;
    private bool isDashing;
    private float dashTimer;
    private float savedGravity;
    private bool facingRight = true;
    private bool overrideFacing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        savedGravity = rb.gravityScale;
        facingRight = transform.localScale.x >= 0f;
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

        if (!overrideFacing)
        {
            if (horizontal > 0)
            {
                facingRight = true;
                UpdateLocalScale();
            }
            else if (horizontal < 0)
            {
                facingRight = false;
                UpdateLocalScale();
            }
        }

        bool estaAndando = Mathf.Abs(horizontal) > 0.001f;
        if (anim != null)
        {
            anim.SetBool("isWalking", estaAndando);
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

    private void UpdateLocalScale()
    {
        transform.localScale = new Vector3(facingRight ? 1f : -1f, 1f, 1f);
    }

    public void SetFacing(bool faceRight, bool setOverride)
    {
        facingRight = faceRight;
        overrideFacing = setOverride;
        UpdateLocalScale();
    }

    public void ClearFacingOverride()
    {
        overrideFacing = false;
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {
            rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
        }
    }
}