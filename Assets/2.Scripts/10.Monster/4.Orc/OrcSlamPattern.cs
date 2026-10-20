// OrcSlamPattern.cs
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 오크의 공격: 팔을 높이 드는 WindUp → 땅을 쾅 찍어 주변에 광역 피해
//  - WindUp 동안 바닥에 공격 범위(붉은 원)가 표시된다 (바깥 원 = 전체 범위, 안쪽 원 = 차오르는 진행도)
//  - 땅을 찍는 순간(impactDelay) 범위 안의 플레이어에게 한 번만 피해. 점프로 피할 수 있다
//
// 애니메이터 설정: int 파라미터 "AttackPhase" (0=평소, 1=WindUp, 2=Attack)
//   Any State → orcWindUp : AttackPhase == 1
//   Any State → orcAttack : AttackPhase == 2
//   orcWindUp/orcAttack → orcIdle : AttackPhase == 0   (Has Exit Time 끄기)
public class OrcSlamPattern : MonsterPatternBase
{
    [Header("Timing")]
    [Tooltip("팔을 드는 시간 (orcWindUp 애니메이션 길이와 비슷하게)")]
    [SerializeField] private float windupDuration = 0.55f;
    [Tooltip("Attack 애니메이션 시작 후 실제로 땅을 찍는 순간까지의 시간")]
    [SerializeField] private float impactDelay = 0.25f;
    [Tooltip("Attack 애니메이션 전체 길이 (이 시간이 지나야 패턴 종료)")]
    [SerializeField] private float slamDuration = 0.7f;

    [Header("Slam")]
    [SerializeField] private float slamRadius = 2.5f;
    [SerializeField] private float slamDamage = 12f;
    [Tooltip("플레이어 발이 지면에서 이 높이보다 위에 있으면 피한 것으로 처리 (점프 회피)")]
    [SerializeField] private float jumpDodgeHeight = 0.8f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Telegraph (범위 표시)")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private float telegraphWidth = 0.08f;

    [Header("Impact Effect")]
    [SerializeField] private Color dustColor = new Color(0.75f, 0.65f, 0.5f, 1f);

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string phaseParameter = "AttackPhase";

    private const int PhaseNone = 0;
    private const int PhaseWindup = 1;
    private const int PhaseAttack = 2;
    private const int CirclePoints = 40;

    private static readonly Collider2D[] hitBuffer = new Collider2D[16];

    private int phaseHash;
    private Collider2D bodyCollider;
    private ContactFilter2D hitFilter;
    private LineRenderer outerRing;
    private LineRenderer progressRing;
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    // 모든 클라이언트가 같은 연출을 보도록 네트워크로 동기화
    [Networked, OnChangedRender(nameof(OnAnimPhaseChanged))]
    private int AnimPhase { get; set; }

    [Networked] private TickTimer WindupTimer { get; set; }
    [Networked] private Vector2 SlamCenter { get; set; }

    [Networked, OnChangedRender(nameof(OnImpact))]
    private int ImpactTick { get; set; }

    // 아래는 호스트에서만 쓰는 값
    private int step; // 0=대기, 1=WindUp, 2=Attack
    private TickTimer stepTimer;
    private TickTimer impactTimer;
    private bool impactDone;

    private void Awake()
    {
        phaseHash = Animator.StringToHash(phaseParameter);
        bodyCollider = GetComponentInParent<Collider2D>();
        hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayer, useTriggers = true };

        outerRing = CreateRing("SlamRangeRing", telegraphColor * new Color(1f, 1f, 1f, 0.5f));
        progressRing = CreateRing("SlamProgressRing", telegraphColor);
        SetRingsActive(false);
    }

    private LineRenderer CreateRing(string ringName, Color color)
    {
        GameObject ringObject = new GameObject(ringName);
        ringObject.transform.SetParent(transform, false);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true; // 몬스터가 뒤집혀도(localScale) 원이 틀어지지 않도록 월드 좌표 사용
        ring.loop = true;
        ring.positionCount = CirclePoints;
        ring.startWidth = telegraphWidth;
        ring.endWidth = telegraphWidth;
        ring.startColor = color;
        ring.endColor = color;
        ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.sortingOrder = 50;

        return ring;
    }

    // 몬스터 발밑(콜라이더 바닥)의 위치. 콜라이더가 없으면 트랜스폼 위치
    private Vector2 GetFeetPosition()
    {
        float feetY = bodyCollider != null ? bodyCollider.bounds.min.y : transform.position.y;
        return new Vector2(transform.position.x, feetY);
    }

    public override void TryStart(Transform target)
    {
        if (!Object.HasStateAuthority) return;
        if (IsOnCooldown || step != 0) return;

        SlamCenter = GetFeetPosition();

        step = 1;
        stepTimer = TickTimer.CreateFromSeconds(Runner, windupDuration);
        WindupTimer = stepTimer;
        AnimPhase = PhaseWindup;
    }

    // MonsterBase가 매 틱 호출. 패턴이 끝나면 true
    public override bool TickPattern()
    {
        if (!Object.HasStateAuthority) return step == 0;

        if (step == 1 && stepTimer.Expired(Runner))
        {
            step = 2;
            stepTimer = TickTimer.CreateFromSeconds(Runner, slamDuration);
            impactTimer = TickTimer.CreateFromSeconds(Runner, impactDelay);
            impactDone = false;
            AnimPhase = PhaseAttack;
        }
        else if (step == 2)
        {
            if (!impactDone && impactTimer.Expired(Runner))
            {
                impactDone = true;
                DoSlamDamage();
                ImpactTick++;
            }

            if (stepTimer.Expired(Runner))
            {
                step = 0;
                stepTimer = TickTimer.None;
                CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
                AnimPhase = PhaseNone;

                return true;
            }
        }

        return false;
    }

    private void DoSlamDamage()
    {
        damagedTargets.Clear();

        Vector2 center = SlamCenter;
        int count = Physics2D.OverlapCircle(center, slamRadius, hitFilter, hitBuffer);

        for (int i = 0; i < count; i++)
        {
            // 점프로 충격파 위를 넘어간 대상은 피해를 입지 않는다
            if (hitBuffer[i].bounds.min.y > center.y + jumpDodgeHeight)
                continue;

            IDamageable damageable = hitBuffer[i].GetComponentInParent<IDamageable>();

            if (damageable == null || !damagedTargets.Add(damageable))
                continue;

            damageable.TakeDamage(slamDamage);
        }
    }

    // WindUp 동안 공격 범위 표시 (네트워크 타이머 기준이라 모든 클라이언트에서 같은 속도로 차오른다)
    public override void Render()
    {
        bool windingUp = AnimPhase == PhaseWindup && WindupTimer.IsRunning;

        SetRingsActive(windingUp);

        if (!windingUp) return;

        float remaining = WindupTimer.RemainingTime(Runner) ?? 0f;
        float progress = Mathf.Clamp01(1f - remaining / windupDuration);

        Vector2 center = SlamCenter + Vector2.up * 0.05f;

        DrawCircle(outerRing, center, slamRadius);
        DrawCircle(progressRing, center, slamRadius * progress);
    }

    private void SetRingsActive(bool active)
    {
        if (outerRing != null && outerRing.gameObject.activeSelf != active)
            outerRing.gameObject.SetActive(active);

        if (progressRing != null && progressRing.gameObject.activeSelf != active)
            progressRing.gameObject.SetActive(active);
    }

    private static void DrawCircle(LineRenderer ring, Vector2 center, float radius)
    {
        for (int i = 0; i < CirclePoints; i++)
        {
            float angle = i / (float)CirclePoints * Mathf.PI * 2f;
            ring.SetPosition(i, new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f));
        }
    }

    // 땅을 찍는 순간 (모든 클라이언트): 먼지가 사방으로 퍼짐
    private void OnImpact()
    {
        RuntimeEffects.PlayBurst(
            SlamCenter + Vector2.up * 0.1f,
            dustColor,
            new Color(dustColor.r, dustColor.g, dustColor.b, 0f),
            count: 40, speed: 6f, size: 0.18f, lifetime: 0.5f
        );
    }

    private void OnAnimPhaseChanged()
    {
        if (animator != null)
            animator.SetInteger(phaseHash, AnimPhase);
    }
}
