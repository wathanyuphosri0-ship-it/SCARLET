using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 9f;
    private float moveSpeed;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 60f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private int maxJumps = 2;
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

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isOverheated = false;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpsRemaining;
    private bool isDashing;
    private bool canDash = true;
    private bool isWallSliding;
    private bool isWallJumping;

    private readonly Collider2D[] wallOverlapResults = new Collider2D[2];

    public bool IsFacingRight => isFacingRight;
    public bool IsDashing => isDashing;
    public bool IsOverheated => isOverheated;

    public bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        jumpsRemaining = maxJumps;
        moveSpeed = baseMoveSpeed;
    }

    private void Update()
    {
        if (isDashing) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        bool grounded = IsGrounded();
        bool isTouchingWall = Physics2D.OverlapBoxNonAlloc(wallCheck.position, wallCheckSize, 0f, wallOverlapResults, groundLayer) > 0;

        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpsRemaining = maxJumps;
            canDash = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && jumpsRemaining > 0)
        {
            if (!grounded && coyoteTimeCounter <= 0f && jumpsRemaining == maxJumps) jumpsRemaining--;
            if (jumpsRemaining > 0) Jump();
        }

        if (Input.GetButtonUp("Jump") && GetVelocityY() > 0f)
        {
            SetVelocity(GetVelocityX(), GetVelocityY() * jumpCutMultiplier);
            coyoteTimeCounter = 0f;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash) StartCoroutine(PerformDash());

        if (isTouchingWall && !grounded && horizontalInput != 0f)
        {
            isWallSliding = true;
            SetVelocity(GetVelocityX(), Mathf.Clamp(GetVelocityY(), -wallSlideSpeed, float.MaxValue));
        }
        else isWallSliding = false;

        if (Input.GetButtonDown("Jump") && isWallSliding) WallJump();

        ApplyGravityAdjustments();

        if (!isWallJumping)
        {
            if (horizontalInput > 0 && !isFacingRight) Flip();
            else if (horizontalInput < 0 && isFacingRight) Flip();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing || isWallJumping) return;

        float targetSpeed = horizontalInput * moveSpeed;
        float speedDif = targetSpeed - GetVelocityX();
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDif * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
    }

    public void ApplySpeedBuff(float multiplier) => moveSpeed = baseMoveSpeed * multiplier;
    public void RemoveSpeedBuff() => moveSpeed = baseMoveSpeed;
    public void ApplyOverheatPenalty(float duration, float slowMultiplier) => StartCoroutine(OverheatRoutine(duration, slowMultiplier));

    private IEnumerator OverheatRoutine(float duration, float slowMultiplier)
    {
        isOverheated = true;
        moveSpeed = baseMoveSpeed * slowMultiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = baseMoveSpeed;
        isOverheated = false;
        Debug.Log("<color=green>[OVERHEAT END] หายจาก Overheat แล้ว!</color>");
    }

    private void Jump()
    {
        SetVelocity(GetVelocityX(), jumpForce);
        jumpsRemaining--;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
    }

    private void WallJump()
    {
        isWallJumping = true;
        float jumpDir = isFacingRight ? -1f : 1f;
        SetVelocity(jumpDir * wallJumpForce.x, wallJumpForce.y);
        jumpsRemaining = maxJumps - 1;
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
        SetVelocity(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void ApplyGravityAdjustments()
    {
        if (GetVelocityY() < 0) rb.gravityScale = gravityScale * fallGravityMultiplier;
        else rb.gravityScale = gravityScale;
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
        SetVelocity(GetVelocityX(), bounceForce);
        jumpsRemaining = maxJumps - 1;
    }

    // Helper methods รองรับทั้ง Unity รุ่นใหม่ (linearVelocity) และรุ่นเก่า (velocity)
    private float GetVelocityX() => rb.velocity.x;
    private float GetVelocityY() => rb.velocity.y;
    private void SetVelocity(float x, float y) => rb.velocity = new Vector2(x, y);

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