// DashPattern.cs
using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class DashPattern : MonsterPatternBase
{
    [Header("Dash Settings")]
    [SerializeField] private float telegraphDuration = 0.3f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float maxDashDistance = 4f;
    [SerializeField] private float dashDamage = 10f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Visual")]
    [SerializeField] private LineRenderer telegraphLine; // 빨간 돌진 경로 선
    [SerializeField] private Animator animator;

    private static readonly int WindupHash = Animator.StringToHash("Windup");
    private static readonly int DashHash = Animator.StringToHash("Dash");

    private enum Phase { Idle, Telegraph, Dashing }

    [Networked] private Phase CurrentPhase { get; set; }
    [Networked] private TickTimer PhaseTimer { get; set; }
    [Networked] private Vector2 DashDirection { get; set; }
    [Networked] private Vector2 DashStartPos { get; set; }
    [Networked, OnChangedRender(nameof(OnPhaseChangedVisual))] private int PhaseChangeTick { get; set; }

    private static readonly Collider2D[] hitBuffer = new Collider2D[8];
    private ContactFilter2D hitFilter;
    private Rigidbody2D rb;
    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        rb = GetComponentInParent<Rigidbody2D>();
        hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayer, useTriggers = true };
    }

    public override void TryStart(Transform target)
    {
        damagedTargets.Clear();
        if (!Object.HasStateAuthority) return;
        if (IsOnCooldown) return;
        if (CurrentPhase != Phase.Idle) return;

        DashDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        DashStartPos = transform.position;
        CurrentPhase = Phase.Telegraph;
        PhaseTimer = TickTimer.CreateFromSeconds(Runner, telegraphDuration);
        PhaseChangeTick++; // 애니메이션/연출 트리거
    }

    // MonsterBase가 매 틱 호출
    public override bool TickPattern()
    {
        if (Object.HasStateAuthority)
        {
            if (CurrentPhase == Phase.Telegraph && PhaseTimer.ExpiredOrNotRunning(Runner))
            {
                CurrentPhase = Phase.Dashing;
                PhaseTimer = TickTimer.CreateFromSeconds(Runner, maxDashDistance / dashSpeed);
                PhaseChangeTick++;
            }

            else if (CurrentPhase == Phase.Dashing)
            {
                if (PhaseTimer.ExpiredOrNotRunning(Runner))
                {
                    // 돌진 종료
                    if (rb != null)
                        rb.linearVelocity = Vector2.zero;

                    CurrentPhase = Phase.Idle;
                    PhaseTimer = TickTimer.None;
                    CooldownTimer = TickTimer.CreateFromSeconds(Runner, cooldown);
                    PhaseChangeTick++;

                    return true;
                }

                if (rb != null)
                    rb.linearVelocity = DashDirection * dashSpeed;

                // 돌진 중 플레이어 피해 판정
                int count = Physics2D.OverlapCircle(transform.position, 0.5f, hitFilter, hitBuffer);

                for (int i = 0; i < count; i++)
                {
                    IDamageable damageable = hitBuffer[i].GetComponentInParent<IDamageable>();

                    if (damageable == null || !damagedTargets.Add(damageable))
                        continue;

                    damageable.TakeDamage(dashDamage);
                }
            }

        }



        return CurrentPhase == Phase.Idle;
    }

    // 텔레그래프 시작/끝날 때 애니메이션 트리거 - 모든 클라이언트에서 실행됨
    private void OnPhaseChangedVisual()
    {
        if (CurrentPhase == Phase.Telegraph)
        {
            animator?.SetTrigger(WindupHash);
        }
        else if (CurrentPhase == Phase.Dashing)
        {
            animator?.SetTrigger(DashHash);
        }
    }

    // 빨간 선이 0.3초 동안 자라나는 연출 - 네트워크로 동기화된 타이머 기준이라 모두 동일하게 보임
    private void UpdateTelegraphLine()
    {
        if (telegraphLine == null) return;

        if (CurrentPhase == Phase.Telegraph)
        {
            telegraphLine.gameObject.SetActive(true);

            float remaining = PhaseTimer.RemainingTime(Runner) ?? 0f;
            float progress = Mathf.Clamp01(1f - remaining / telegraphDuration);

            Vector2 endPoint = DashStartPos + DashDirection * maxDashDistance * progress;
            telegraphLine.SetPosition(0, DashStartPos);
            telegraphLine.SetPosition(1, endPoint);
        }
        else
        {
            telegraphLine.gameObject.SetActive(false);
        }
    }
    public override void Render()
    {
        UpdateTelegraphLine();
    }
}