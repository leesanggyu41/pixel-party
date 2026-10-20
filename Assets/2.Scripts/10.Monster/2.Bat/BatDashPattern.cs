// BatDashPattern.cs
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 박쥐의 공격: WindUp → 플레이어를 향해 돌진(Attack) → 돌진 전 위치로 빠르게 복귀
// (고블린은 돌진한 자리에서 다시 움직이지만 박쥐는 출발했던 자리로 되돌아간다)
//
// 애니메이터 설정: int 파라미터 "AttackPhase" (0=평소(Idle=Move), 1=WindUp, 2=Attack)
//   Any State → BatWindUp : AttackPhase == 1
//   Any State → BatAttack : AttackPhase == 2
//   BatWindUp/BatAttack → Bat move : AttackPhase == 0   (Has Exit Time 끄기)
public class BatDashPattern : MonsterPatternBase
{
    [Header("Windup")]
    [SerializeField] private float windupDuration = 0.5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float maxDashDistance = 5f;
    [SerializeField] private float dashDamage = 8f;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Return")]
    [SerializeField] private float returnSpeed = 10f;
    [SerializeField] private float returnArriveDistance = 0.2f;
    [Tooltip("복귀 중 이 시간 동안 거의 못 움직이면 벽에 끼인 것으로 보고 복귀를 포기")]
    [SerializeField] private float stuckTimeout = 0.3f;

    [Header("Telegraph (돌진 경로 표시)")]
    [Tooltip("비워두면 코드가 빨간 선을 자동으로 만든다. 고블린의 Line처럼 직접 만든 LineRenderer(월드 좌표)를 넣어도 됨")]
    [SerializeField] private LineRenderer telegraphLine;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private float telegraphWidth = 0.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string phaseParameter = "AttackPhase";

    private const int PhaseNone = 0;
    private const int PhaseWindup = 1;
    private const int PhaseAttack = 2;

    private enum Step { Idle, Windup, Dash, Return }

    private static readonly Collider2D[] hitBuffer = new Collider2D[8];

    private int phaseHash;
    private Rigidbody2D rb;
    private ContactFilter2D hitFilter;
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    // 모든 클라이언트가 같은 애니메이션을 보도록 단계만 네트워크로 동기화
    [Networked, OnChangedRender(nameof(OnAnimPhaseChanged))]
    private int AnimPhase { get; set; }

    // 경로 표시선을 모든 클라이언트에서 같은 모양으로 그리기 위한 네트워크 값
    [Networked] private TickTimer WindupTimer { get; set; }
    [Networked] private Vector2 DashOrigin { get; set; }
    [Networked] private Vector2 DashDirection { get; set; }

    // 아래는 호스트에서만 쓰는 값
    private Step step;
    private TickTimer stepTimer;
    private Vector2 homePosition;
    private Transform target;
    private Vector2 lastReturnPosition;
    private float stuckTime;
    private float returnElapsed;
    private float maxReturnTime;

    private void Awake()
    {
        phaseHash = Animator.StringToHash(phaseParameter);
        rb = GetComponentInParent<Rigidbody2D>();
        hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayer, useTriggers = true };

        if (telegraphLine == null)
            telegraphLine = CreateTelegraphLine();

        telegraphLine.gameObject.SetActive(false);
    }

    private LineRenderer CreateTelegraphLine()
    {
        GameObject lineObject = new GameObject("TelegraphLine");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true; // 몬스터가 뒤집혀도(localScale) 선이 틀어지지 않도록 월드 좌표 사용
        line.positionCount = 2;
        line.startWidth = telegraphWidth;
        line.endWidth = telegraphWidth;
        line.startColor = telegraphColor;
        line.endColor = telegraphColor;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 50;

        return line;
    }

    public override void TryStart(Transform newTarget)
    {
        if (!Object.HasStateAuthority) return;
        if (IsOnCooldown || step != Step.Idle) return;

        target = newTarget;
        homePosition = transform.position; // 돌진 후 돌아올 자리
        damagedTargets.Clear();

        // 돌진 경로는 WindUp 시작 순간에 확정 - 표시선이 보여주는 경로가 실제 돌진 경로가 되어 피할 수 있다
        Vector2 origin = homePosition;
        Vector2 direction = newTarget != null
            ? ((Vector2)newTarget.position - origin).normalized
            : Vector2.right * -Mathf.Sign(transform.lossyScale.x);

        DashOrigin = origin;
        DashDirection = direction;

        step = Step.Windup;
        stepTimer = TickTimer.CreateFromSeconds(Runner, windupDuration);
        WindupTimer = stepTimer;
        AnimPhase = PhaseWindup;
    }

    // MonsterBase가 매 틱 호출. 복귀까지 끝나면 true
    public override bool TickPattern()
    {
        if (!Object.HasStateAuthority) return step == Step.Idle;

        switch (step)
        {
            case Step.Windup:
                TickWindup();
                break;

            case Step.Dash:
                TickDash();
                break;

            case Step.Return:
                return TickReturn();
        }

        return false;
    }

    private void TickWindup()
    {
        if (!stepTimer.Expired(Runner)) return;

        step = Step.Dash;
        stepTimer = TickTimer.CreateFromSeconds(Runner, maxDashDistance / dashSpeed);
        AnimPhase = PhaseAttack;
    }

    private void TickDash()
    {
        if (stepTimer.Expired(Runner))
        {
            BeginReturn();
            return;
        }

        if (rb != null)
            rb.linearVelocity = DashDirection * dashSpeed;

        int count = Physics2D.OverlapCircle(transform.position, hitRadius, hitFilter, hitBuffer);

        for (int i = 0; i < count; i++)
        {
            IDamageable damageable = hitBuffer[i].GetComponentInParent<IDamageable>();

            if (damageable == null || !damagedTargets.Add(damageable))
                continue;

            damageable.TakeDamage(dashDamage);
        }
    }

    private void BeginReturn()
    {
        step = Step.Return;
        stepTimer = TickTimer.None;
        AnimPhase = PhaseNone;

        // 복귀 중 벽에 끼는 것을 감지하기 위한 값 초기화
        Vector2 position = transform.position;
        lastReturnPosition = position;
        stuckTime = 0f;
        returnElapsed = 0f;
        maxReturnTime = (homePosition - position).magnitude / returnSpeed * 2f + 0.5f;
    }

    private bool TickReturn()
    {
        Vector2 position = transform.position;
        Vector2 toHome = homePosition - position;

        if (toHome.magnitude <= returnArriveDistance)
            return FinishPattern();

        returnElapsed += Runner.DeltaTime;

        // 이동량이 기대치보다 계속 작으면 벽/지형에 끼인 것으로 보고 복귀를 포기한다
        float moved = (position - lastReturnPosition).magnitude;
        lastReturnPosition = position;
        float expected = returnSpeed * Runner.DeltaTime;
        stuckTime = moved < expected * 0.2f ? stuckTime + Runner.DeltaTime : 0f;

        if (stuckTime >= stuckTimeout || returnElapsed >= maxReturnTime)
            return FinishPattern();

        if (rb != null)
            rb.linearVelocity = toHome.normalized * returnSpeed;

        return false;
    }

    // 정지하고 쿨타임 시작 (복귀가 끝났거나 끼어서 포기했을 때 공통)
    private bool FinishPattern()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        step = Step.Idle;
        target = null;
        CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);

        return true;
    }

    // 네트워크로 동기화된 타이머 기준이라 모든 클라이언트에서 같은 속도로 선이 늘어난다
    public override void Render()
    {
        if (telegraphLine == null) return;

        bool windingUp = AnimPhase == PhaseWindup && WindupTimer.IsRunning;

        if (telegraphLine.gameObject.activeSelf != windingUp)
            telegraphLine.gameObject.SetActive(windingUp);

        if (!windingUp) return;

        float remaining = WindupTimer.RemainingTime(Runner) ?? 0f;
        float progress = Mathf.Clamp01(1f - remaining / windupDuration);

        Vector2 start = DashOrigin;
        Vector2 end = start + DashDirection * maxDashDistance * progress;

        telegraphLine.SetPosition(0, start);
        telegraphLine.SetPosition(1, end);
    }

    private void OnAnimPhaseChanged()
    {
        if (animator != null)
            animator.SetInteger(phaseHash, AnimPhase);
    }
}
