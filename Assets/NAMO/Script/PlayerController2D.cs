using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 9f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private int maxJumps = 2; // จำกัดไว้ที่ Double Jump (2 ครั้ง)
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float gravityScale = 3.5f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Wall Mechanics")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private Vector2 wallJumpForce = new Vector2(10f, 15f);

    [Header("Ground & Wall Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.6f, 0.1f);
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 1.2f);
    [SerializeField] private LayerMask groundLayer;

    // Components & Internal States
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;

    // Timers & Flags
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpsRemaining; // ตัวนับจำนวนครั้งที่กระโดดได้ที่เหลืออยู่
    private bool isDashing;
    private bool canDash = true;
    private bool isWallSliding;
    private bool isWallJumping;

    // Allocation-free physics buffers for Unity 6
    private readonly Collider2D[] wallOverlapResults = new Collider2D[2];

    public bool IsFacingRight => isFacingRight;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        jumpsRemaining = maxJumps;
    }

    private void Update()
    {
        if (isDashing) return;

        // Input Handling
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Physics check
        bool isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        bool isTouchingWall = Physics2D.OverlapBoxNonAlloc(wallCheck.position, wallCheckSize, 0f, wallOverlapResults, groundLayer) > 0;

        // Coyote Time & Jump Reset Logic
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpsRemaining = maxJumps; // รีเซ็ตเป็น 2 เมื่อแตะพื้น
            canDash = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer Logic
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Execute Jump (เช็กจำนวนครั้งที่เหลือแบบเด็ดขาด)
        if (jumpBufferCounter > 0f && jumpsRemaining > 0)
        {
            // ถ้าไม่อยู่บนพื้นและหมดเวลา Coyote Time ให้หักสิทธิ์การโดดกลางอากาศ
            if (!isGrounded && coyoteTimeCounter <= 0f && jumpsRemaining == maxJumps)
            {
                jumpsRemaining--; // หักสิทธิ์การโดดครั้งแรกที่ร่วงขอบพื้น
            }

            if (jumpsRemaining > 0)
            {
                Jump();
            }
        }

        // Variable Jump Height
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
        }

        // Dash Trigger
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(PerformDash());
        }

        // Wall Slide Logic
        if (isTouchingWall && !isGrounded && horizontalInput != 0f)
        {
            isWallSliding = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }

        // Wall Jump Trigger
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            WallJump();
        }

        ApplyGravityAdjustments();

        // Direction Flip
        if (!isWallJumping)
        {
            if (horizontalInput > 0 && !isFacingRight) Flip();
            else if (horizontalInput < 0 && isFacingRight) Flip();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing || isWallJumping) return;

        // Velocity Force Calculation
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpsRemaining--; // ลดจำนวนการกระโดดลงทันที
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f; // เคลียร์ Coyote Time ทันทีเพื่อไม่ให้โดดซ้ำซ้อน
    }

    private void WallJump()
    {
        isWallJumping = true;
        float jumpDir = isFacingRight ? -1f : 1f;
        rb.linearVelocity = new Vector2(jumpDir * wallJumpForce.x, wallJumpForce.y);
        jumpsRemaining = maxJumps - 1; // Wall Jump นับเป็นกระโดดครั้งแรก
        jumpBufferCounter = 0f;

        Invoke(nameof(StopWallJump), 0.15f);
    }

    private void StopWallJump() => isWallJumping = false;

    private IEnumerator PerformDash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDir = isFacingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void ApplyGravityAdjustments()
    {
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        else
            rb.gravityScale = gravityScale;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void Bounce(float bounceForce)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
        jumpsRemaining = maxJumps - 1; // คืนสิทธิ์การทำ Double Jump กลางอากาศหลัง Pogoing
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        }
    }
}