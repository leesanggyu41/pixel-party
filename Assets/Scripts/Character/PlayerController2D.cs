using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Sockets;

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

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool wasGrounded;
    private bool jumpPressed;
    private bool jumpHeldThisFrame; // 새 Input System은 GetButtonDown 대신 콜백/폴링으로 처리
    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
    }

    public override void FixedUpdateNetwork()
    {

        if (GetInput(out PlayerNetworkInput input))
        {
            moveInput = input.MoveX;
            if (input.JumpInput)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.fixedDeltaTime;
            }
        }

        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            Anim.SetTrigger("JustLanded");
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
            transform.localScale = new Vector3(Mathf.Sign(-moveInput), 1f, 1f);
        }

        HandleMove();
        HandleJump();
        HandleGravity();
    }

    public override void Render()
    {
        if (Anim == null) return;

        Anim.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        Anim.SetBool(IsGroundedHash, isGrounded);
    }

    private void HandleMove()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

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
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}