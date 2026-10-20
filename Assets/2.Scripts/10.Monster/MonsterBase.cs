// MonsterBase.cs
using Fusion;
using UnityEngine;

public class MonsterBase : NetworkBehaviour, IDamageable
{
    [Header("Base Stats")]
    [SerializeField] private int maxHp = 20;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float defense = 0f;

    [Header("Patterns (1~4개, 자식 컴포넌트로 붙임)")]
    [SerializeField] private MonsterPatternBase[] patterns;

    [Header("Movement Options")]
    [Tooltip("true면 중력을 받지 않고 상하로도 이동한다 (박쥐)")]
    [SerializeField] private bool isFlying = false;
    [Tooltip("비행 몬스터가 타깃보다 이 높이 위에서 맴돈다")]
    [SerializeField] private float hoverHeight = 2f;
    [Tooltip("0보다 크면, 타깃이 이 거리 이하로 가까워졌을 때 뒤로 물러난다 (창고블린)")]
    [SerializeField] private float retreatDistance = 0f;
    [Tooltip("물러나기 시작한 뒤 이 값만큼 더 멀어져야 다시 접근한다 (떨림 방지)")]
    [SerializeField] private float retreatHysteresis = 1f;
    [SerializeField] private float retreatSpeedMultiplier = 1f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Animation (선택)")]
    [Tooltip("Idle/Move 애니메이션이 다를 때만 연결. 이동 속도를 Speed 파라미터로 전달")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";

    [Networked] public int CurrentHp { get; set; }
    [Networked] private int ActivePatternIndex { get; set; } = -1; // -1 = 패턴 실행 중 아님

    [Networked] private float NetFacingDirection { get; set; } = 1f;

    private Transform currentTarget;
    private bool isRetreating;

    private int speedHash;
    private Vector3 lastRenderPosition;
    private float smoothedSpeed;

    public override void Spawned()
    {
        CurrentHp = maxHp;

        if (isFlying && rb != null)
            rb.gravityScale = 0f;

        if (animator != null)
            speedHash = Animator.StringToHash(speedParameter);

        lastRenderPosition = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        FindNearestTarget();

        // 패턴 실행 중이면, 그 패턴만 계속 진행 (이동 안 함)
        if (ActivePatternIndex >= 0)
        {
            bool finished = patterns[ActivePatternIndex].TickPattern();
            if (finished)
            {
                ActivePatternIndex = -1;
            }
            return;
        }

        MoveTowardTarget();
        TryStartPattern();
    }

    private void FindNearestTarget()
    {
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
            {
                float dist = ((Vector2)(transform.position - playerObj.transform.position)).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = playerObj.transform;
                }
            }
        }

        currentTarget = closest;
    }

    private void MoveTowardTarget()
    {
        if (currentTarget == null || rb == null) return;

        float direction = Mathf.Sign(currentTarget.position.x - transform.position.x); // 좌우 방향만
        NetFacingDirection = direction; // 물러나는 중에도 항상 타깃을 바라본다

        if (isFlying)
        {
            MoveFlying();
            return;
        }

        float moveDirection = direction;
        float speed = moveSpeed;

        if (retreatDistance > 0f)
        {
            UpdateRetreatState();

            if (isRetreating)
            {
                moveDirection = -direction;
                speed *= retreatSpeedMultiplier;
            }
        }

        rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocity.y); // Y는 그대로 둬서 중력이 적용되게
    }

    // 너무 가까우면 물러나고, 충분히 멀어지면 다시 접근 (경계에서 떨지 않도록 히스테리시스 적용)
    private void UpdateRetreatState()
    {
        float distance = Mathf.Abs(currentTarget.position.x - transform.position.x);

        if (isRetreating)
        {
            if (distance > retreatDistance + retreatHysteresis)
                isRetreating = false;
        }
        else if (distance < retreatDistance)
        {
            isRetreating = true;
        }
    }

    private void MoveFlying()
    {
        Vector2 hoverPoint = (Vector2)currentTarget.position + Vector2.up * hoverHeight;
        Vector2 toPoint = hoverPoint - (Vector2)transform.position;

        // 목표 지점 근처에서는 멈춰서 떨림 방지
        rb.linearVelocity = toPoint.sqrMagnitude > 0.04f
            ? toPoint.normalized * moveSpeed
            : Vector2.zero;
    }

    private void TryStartPattern()
    {
        if (currentTarget == null) return;

        float sqrDistance = ((Vector2)(transform.position - currentTarget.position)).sqrMagnitude;

        for (int i = 0; i < patterns.Length; i++)
        {
            float range = patterns[i].TriggerRange;
            if (!patterns[i].IsOnCooldown && sqrDistance <= range * range)
            {
                if (rb != null) rb.linearVelocity = Vector2.zero; // 패턴 시작하면 이동 멈춤
                patterns[i].TryStart(currentTarget);
                ActivePatternIndex = i;
                return;
            }
        }
    }
    public override void Render()
    {
        transform.localScale = new Vector3(-NetFacingDirection, 1f, 1f); // 필요시 부호 조정

        UpdateSpeedAnimation();
    }

    // 모든 클라이언트에서 실제 이동 거리로 속도를 계산해 Idle/Move를 전환 (별도 네트워크 동기화 불필요)
    private void UpdateSpeedAnimation()
    {
        if (animator == null) return;

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        float speed = Mathf.Abs(transform.position.x - lastRenderPosition.x) / deltaTime;
        lastRenderPosition = transform.position;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, speed, 0.3f);
        animator.SetFloat(speedHash, smoothedSpeed);
    }

    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority) return;

        float actualDamage = Mathf.Max(1f, amount - defense);
        CurrentHp = Mathf.Max(0, CurrentHp - Mathf.RoundToInt(actualDamage));

        if (CurrentHp <= 0)
        {
            StageManager.Instance?.OnMonsterDefeated();
            Runner.Despawn(Object);
        }
    }
}