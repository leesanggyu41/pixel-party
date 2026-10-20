using UnityEngine;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : NetworkBehaviour
{
    private float moveSpeed, acceleration, deceleration, jumpForce;
    private float gravityScale, fallGravityMultiplier, coyoteTime, jumpBufferTime;

    private Transform groundCheck;
    private Animator anim;

    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded, wasGrounded, jumpPressed;
    private float coyoteTimer, jumpBufferTimer;
    private bool statsReady = false;

    // 계산 결과를 담는 네트워크 값 - Render()에서 모두가 이걸 읽음
    [Networked] private float NetSpeed { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private float NetFacingDirection { get; set; } = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    public override void Spawned()
    {
        // State Authority(호스트)만 실제 물리 시뮬레이션(Dynamic), 나머지는 순수 위치 반영만(Kinematic)
        rb.bodyType = Object.HasStateAuthority ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }

    public void SetStats(float moveSpeed, float acceleration, float deceleration, float jumpForce,
                          float gravityScale, float fallGravityMultiplier, float coyoteTime, float jumpBufferTime)
    {
        this.moveSpeed = moveSpeed;
        this.acceleration = acceleration;
        this.deceleration = deceleration;
        this.jumpForce = jumpForce;
        this.gravityScale = gravityScale;
        this.fallGravityMultiplier = fallGravityMultiplier;
        this.coyoteTime = coyoteTime;
        this.jumpBufferTime = jumpBufferTime;

        rb.gravityScale = gravityScale;
        statsReady = true;
    }

    public void SetGroundCheck(Transform gc) => groundCheck = gc;
    public void SetAnimator(Animator a) => anim = a;

    public override void FixedUpdateNetwork()
    {
        if (!statsReady || groundCheck == null) return;
        if (!Object.HasStateAuthority) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            moveInput = input.MoveX;
            jumpBufferTimer = input.JumpInput ? jumpBufferTime : jumpBufferTimer - Runner.DeltaTime;
        }

        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - Runner.DeltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            jumpPressed = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (moveInput != 0)
        {
            NetFacingDirection = Mathf.Sign(-moveInput); // localScale 대신 네트워크 값으로
        }

        HandleMove();
        HandleJump();
        HandleGravity();

        // 계산 끝난 결과를 네트워크 값으로 반영
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
        NetIsGrounded = isGrounded;
    }

    // 권한 상관없이 모든 클라이언트에서 매 프레임 실행됨
    public override void Render()
    {
        if (anim != null)
        {
            anim.SetFloat(SpeedHash, NetSpeed);
            anim.SetBool(IsGroundedHash, NetIsGrounded);
        }

        transform.localScale = new Vector3(NetFacingDirection, 1f, 1f);
    }

    private void HandleMove()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + speedDiff * accelRate * Runner.DeltaTime, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }
    }


    public void Jump(float force)
    {
        // 실제 물리를 계산하는 StateAuthority에서만 점프 적용
        if (Object == null || !Object.HasStateAuthority)
            return;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            force
        );

        jumpPressed = false;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }


    private void HandleGravity()
    {
        rb.gravityScale = rb.linearVelocity.y < 0 ? gravityScale * fallGravityMultiplier : gravityScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}