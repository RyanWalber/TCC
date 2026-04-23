using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float acceleration = 50f;
    [SerializeField] float deceleration = 70f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 12f;
    [SerializeField] int maxJumps = 2;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.12f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float coyoteTime = 0.12f; // small forgiveness after leaving ground
    [SerializeField] float jumpBufferTime = 0.12f; // buffer press before landing

    [Header("Dash")]
    [SerializeField] float dashSpeed = 22f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 0.9f;
    [SerializeField] bool allowAirDash = true;

    // Public read-only state
    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsIdle => Mathf.Abs(horizontal) < 0.05f && IsGrounded && !IsDashing;

    // Events (useful for VFX / SFX later)
    public event Action OnJump;
    public event Action OnDashStart;
    public event Action OnDashEnd;

    // internals
    Rigidbody2D rb;
    float horizontal;
    float lastInputDirection = 0f; // stores last non-zero horizontal input direction (-1 or 1)
    // Input System actions
    InputAction moveAction;
    InputAction jumpAction;
    InputAction dashAction;
    int jumpsLeft;
    float lastGroundTime = -10f;
    float lastJumpPressedTime = -10f;

    float dashTimer = 0f;
    float dashCooldownTimer = 0f;
    float savedGravityScale;
    float dashDirection = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        savedGravityScale = rb.gravityScale;

        // Setup Input System actions so this script works even when "Active Input Handling" is set to Input System only
        moveAction = new InputAction("Move", InputActionType.Value);
        // Composite binding for WASD
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        // Composite binding for arrow keys
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        // Gamepad analog
        moveAction.AddBinding("<Gamepad>/leftStick/x");
        moveAction.Enable();

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += OnJumpPerformed;
        jumpAction.Enable();

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/rightShoulder");
        dashAction.performed += OnDashPerformed;
        dashAction.Enable();
    }

    void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        lastJumpPressedTime = Time.time;
    }

    void OnDashPerformed(InputAction.CallbackContext ctx)
    {
        if (dashCooldownTimer <= 0f)
            TryStartDash();
    }

    void Start()
    {
        jumpsLeft = maxJumps;
    }

    void OnDisable()
    {
        // disable & dispose input actions
        if (moveAction != null) { moveAction.Disable(); moveAction.Dispose(); moveAction = null; }
        if (jumpAction != null) { jumpAction.performed -= OnJumpPerformed; jumpAction.Disable(); jumpAction.Dispose(); jumpAction = null; }
        if (dashAction != null) { dashAction.performed -= OnDashPerformed; dashAction.Disable(); dashAction.Dispose(); dashAction = null; }
    }

    void Update()
    {
        // Read input using Input System actions
        if (moveAction != null)
            horizontal = moveAction.ReadValue<float>();

        // remember last pressed direction so dash can use it even after release
        if (Mathf.Abs(horizontal) > 0.1f)
            lastInputDirection = Mathf.Sign(horizontal);

        // Timers
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;
        if (dashTimer > 0f)
            dashTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        UpdateGrounded();
        HandleJumpBuffering();

        if (IsDashing)
        {
            // maintain dash velocity
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            if (dashTimer <= 0f)
                EndDash();
            return;
        }

        // Horizontal movement with simple acceleration
        float targetVelX = horizontal * moveSpeed;
        float velX = Mathf.MoveTowards(rb.linearVelocity.x, targetVelX, (Mathf.Abs(targetVelX) > Mathf.Abs(rb.linearVelocity.x) ? acceleration : deceleration) * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
    }

    void UpdateGrounded()
    {
        if (groundCheck != null)
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
            if (IsGrounded)
            {
                lastGroundTime = Time.time;
                jumpsLeft = maxJumps;
            }
            // keep lastGroundTime unchanged when leaving; leaving time already recorded
            if (!wasGrounded && IsGrounded)
            {
                // landed
            }
        }
    }

    void HandleJumpBuffering()
    {
        // if player pressed jump recently and within coyote time / grounded, perform jump
        bool canUseCoyote = (Time.time - lastGroundTime) <= coyoteTime;
        bool jumpBuffered = (Time.time - lastJumpPressedTime) <= jumpBufferTime;

        if (jumpBuffered && (IsGrounded || canUseCoyote || jumpsLeft > 0))
        {
            PerformJump();
            lastJumpPressedTime = -10f;
        }
    }

    void PerformJump()
    {
        // consume a jump
        if (!IsGrounded && jumpsLeft <= 0)
            return;

        // Reset vertical velocity for consistent jump height
        Vector2 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (!IsGrounded)
            jumpsLeft = Mathf.Max(0, jumpsLeft - 1);

        OnJump?.Invoke();
    }

    void TryStartDash()
    {
        if (IsDashing)
            return;

        if (!allowAirDash && !IsGrounded)
            return;

        // determine direction: prefer current input, then last input you pressed, then velocity, then facing
        if (Mathf.Abs(horizontal) > 0.1f)
            dashDirection = Mathf.Sign(horizontal);
        else if (Mathf.Abs(lastInputDirection) > 0.1f)
            dashDirection = Mathf.Sign(lastInputDirection);
        else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            dashDirection = Mathf.Sign(rb.linearVelocity.x);
        else
            dashDirection = transform.localScale.x >= 0f ? 1f : -1f;

        StartDash();
    }

    public void StartDash()
    {
        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        savedGravityScale = rb.gravityScale;
        rb.gravityScale = 0f; // prevent falling during dash
        OnDashStart?.Invoke();
    }

    void EndDash()
    {
        IsDashing = false;
        rb.gravityScale = savedGravityScale;
        OnDashEnd?.Invoke();
    }

    // Public helpers
    public void ResetJumps()
    {
        jumpsLeft = maxJumps;
    }

    public void ForceStopDash()
    {
        if (IsDashing)
            EndDash();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
