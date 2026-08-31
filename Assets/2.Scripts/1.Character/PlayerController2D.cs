using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : NetworkBehaviour
{
    public Animator Anim;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 60f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    // 물리는 호스트만 계산하는 로컬 변수들
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpPressed;
    private float coyoteTimer;
    private float jumpBufferTimer;

    // ↓ 모든 클라이언트에 자동 동기화되는 값들 - 애니메이션은 이걸로 재생
    [Networked] private float NetSpeed { get; set; }
    [Networked] private NetworkBool NetIsGrounded { get; set; }
    [Networked] private float NetFacingDirection { get; set; } = 1f;
    [Networked, OnChangedRender(nameof(OnLandedChanged))] private int LandedTick { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    public override void FixedUpdateNetwork()
    {
        // 물리 계산은 오직 호스트(State Authority)만 실행
        if (!Object.HasStateAuthority) return;

        if (GetInput(out PlayerNetworkInput input))
        {
            moveInput = input.MoveX;

            if (input.JumpInput)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Runner.DeltaTime;
            }
        }

        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            LandedTick++; // 값이 바뀌면 모든 클라이언트에서 OnLandedChanged가 자동 호출됨
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Runner.DeltaTime;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            jumpPressed = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        if (moveInput != 0)
        {
            NetFacingDirection = Mathf.Sign(-moveInput);
        }
        transform.localScale = new Vector3(NetFacingDirection, 1f, 1f);

        HandleMove();
        HandleJump();
        HandleGravity();

        // 계산 끝난 결과를 네트워크 값으로 반영 - 이게 모든 클라이언트에 자동 전파됨
        NetSpeed = Mathf.Abs(rb.linearVelocity.x);
        NetIsGrounded = isGrounded;
    }

    // Render()는 권한 상관없이 모든 클라이언트(호스트 포함)에서 매 프레임 실행됨
    public override void Render()
    {
        if (Anim == null) return;

        Anim.SetFloat(SpeedHash, NetSpeed);
        Anim.SetBool(IsGroundedHash, NetIsGrounded);

        // 방향 전환도 여기서 매 프레임 반영 (프록시 클라이언트도 방향 보이게)
        transform.localScale = new Vector3(NetFacingDirection, 1f, 1f);
    }

    // LandedTick이 바뀌는 순간 모든 클라이언트에서 자동 호출됨
    private void OnLandedChanged()
    {
        if (Anim != null)
        {
            Anim.SetTrigger("JustLanded");
        }
    }

    private void HandleMove()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate * Runner.DeltaTime;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x + movement, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpPressed = false;
        }
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