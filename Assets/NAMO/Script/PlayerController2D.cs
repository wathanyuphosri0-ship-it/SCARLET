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
    [SerializeField] private float jumpCutMultiplier = 0.5f; // ชะลอเมื่อปล่อยปุ่ม jump เร็ว
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

    // Components & States
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;

    // Timers & Flags
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isDashing;
    private bool canDash = true;
    private bool isWallSliding;
    private bool isWallJumping;

    public bool IsFacingRight => isFacingRight;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    private void Update()
    {
        if (isDashing) return;

        // Input Handling
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Ground & Wall Check
        bool isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        bool isTouchingWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, groundLayer);

        // Coyote Time
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            canDash = true; // รีเซ็ต Dash เมื่อแตะพื้น
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump Buffer
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Execute Jump
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            Jump();
        }

        // Variable Jump Height (กดค้างโดดสูง กดแป๊บเดียวโดดเตี้ย)
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
        }

        // Dash Input
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(PerformDash());
        }

        // Wall Slide Logic
        if (isTouchingWall && !isGrounded && horizontalInput != 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }

        // Wall Jump
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            WallJump();
        }

        // Dynamic Gravity Adjustments
        ApplyGravityAdjustments();

        // Flip Character
        if (!isWallJumping)
        {
            if (horizontalInput > 0 && !isFacingRight) Flip();
            else if (horizontalInput < 0 && isFacingRight) Flip();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing || isWallJumping) return;

        // Movement with smooth Acceleration/Deceleration
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - rb.velocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void WallJump()
    {
        isWallJumping = true;
        float jumpDir = isFacingRight ? -1f : 1f;
        rb.velocity = new Vector2(jumpDir * wallJumpForce.x, wallJumpForce.y);
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
        rb.velocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void ApplyGravityAdjustments()
    {
        if (rb.velocity.y < 0)
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

    // ฟังก์ชันสำหรับ Pogoing (เรียกใช้เมื่อฟันลงโดนศัตรู/หนาม)
    public void Bounce(float bounceForce)
    {
        rb.velocity = new Vector2(rb.velocity.x, bounceForce);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck) Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        if (wallCheck) Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
    }
}