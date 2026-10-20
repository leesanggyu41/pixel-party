using Fusion;
using UnityEngine;

// 윙보우 스킬 "헤이스트":
// 무기의 공격속도를 10% + (추가 쿨타임 감소의 40%)만큼 일정 시간 증가시킨다.
public class WingBow : ProjectileBase
{
    [Header("Haste (헤이스트)")]
    [SerializeField] private float hasteDuration = 5f;
    [Tooltip("기본 공격속도 증가량(%)")]
    [SerializeField] private float baseBonusPercent = 10f;
    [Tooltip("소유자의 쿨타임 감소(%) 중 공격속도로 전환되는 비율 (0.4 = 40%)")]
    [SerializeField] private float cooldownConversion = 0.4f;

    [Header("Haste Effect")]
    [SerializeField] private Color hasteColor = new Color(0.6f, 1f, 0.4f, 1f);

    [Networked] private TickTimer HasteTimer { get; set; }
    [Networked] private float HasteBonus { get; set; } // 0.1 = +10%

    private ParticleSystem hasteAura;

    public override bool HasSkill => true;

    private bool IsHasteActive => HasteTimer.IsRunning && !HasteTimer.Expired(Runner);

    protected override void OnSkill(float facingDirection)
    {
        PlayerData data = OwnerData;
        float extraCooldownReduction = data != null ? data.finalCooltime : 0f;

        // 공격속도 증가(%) = 10 + 추가 쿨타임 감소(%) × 0.4
        float bonusPercent = baseBonusPercent + extraCooldownReduction * cooldownConversion;

        HasteBonus = bonusPercent / 100f;
        HasteTimer = TickTimer.CreateFromSeconds(Runner, hasteDuration);
    }

    protected override float GetAttackSpeedMultiplier()
    {
        return IsHasteActive ? 1f + HasteBonus : 1f;
    }

    // 발동 순간: 활 주변으로 초록빛 바람이 퍼져나감
    protected override void OnSkillVisual()
    {
        RuntimeEffects.PlayBurst(
            transform.position,
            hasteColor,
            new Color(hasteColor.r, hasteColor.g, hasteColor.b, 0f),
            count: 24, speed: 4f, size: 0.12f, lifetime: 0.5f
        );
    }

    // 지속 시간 동안: 활에서 바람 기운이 솟고, 공격 애니메이션도 빨라진다 (모든 클라이언트)
    public override void Render()
    {
        bool active = IsHasteActive;

        if (hasteAura == null)
        {
            hasteAura = RuntimeEffects.CreateAura(
                transform, Vector3.zero,
                hasteColor,
                new Color(hasteColor.r, hasteColor.g, hasteColor.b, 0f),
                emissionRate: 18f, speed: 1.2f, size: 0.09f, lifetime: 0.6f, radius: 0.35f
            );
        }

        if (active && !hasteAura.isPlaying)
            hasteAura.Play();
        else if (!active && hasteAura.isPlaying)
            hasteAura.Stop();

        if (animator != null)
            animator.speed = active ? 1f + HasteBonus : 1f;
    }
}
