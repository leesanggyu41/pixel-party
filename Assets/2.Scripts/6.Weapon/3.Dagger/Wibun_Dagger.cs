using Fusion;
using UnityEngine;

// 단검 스킬 "가열":
// 기본공격이 한 번 강화되어 공격력의 30%만큼 추가 피해를 입힌다.
// 스킬을 쓰면 단검이 달아올라(주황색 + 불꽃) 다음 기본공격 한 번에 추가 피해가 적용되고 사라진다.
public class Wibun_Dagger : Swordbase
{
    [Header("Heat (가열)")]
    [Tooltip("강화 공격의 추가 피해 비율 (0.3 = 공격력의 30%)")]
    [SerializeField] private float bonusDamageRatio = 0.3f;
    [SerializeField] private Color heatTint = new Color(1f, 0.55f, 0.25f, 1f);
    [SerializeField] private Color flameColor = new Color(1f, 0.45f, 0.1f, 1f);

    [Networked, OnChangedRender(nameof(OnHeatChanged))]
    private NetworkBool HeatReady { get; set; }

    [Networked, OnChangedRender(nameof(OnHeatBurst))]
    private int HeatBurstTick { get; set; }

    [Header("Attack Effect (기본공격 이펙트)")]
    [Tooltip("공격할 때 재생되고, 애니메이션이 끝나면 바로 사라지는 이펙트 프리팹 (DaggerEffact)")]
    [SerializeField] private GameObject attackEffectPrefab;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private ParticleSystem flameAura;

    public override bool HasSkill => true;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    // 스킬 사용: 다음 기본공격을 강화 상태로 만든다 (이미 강화 상태면 유지)
    protected override void OnSkill(float facingDirection)
    {
        HeatReady = true;
    }

    // 공격 직전: 강화 상태라면 추가 피해를 적용하고 강화를 소비
    protected override void PrepareAttack()
    {
        if (!HeatReady)
            return;

        AttackDamageBonus = bonusDamageRatio;
        HeatReady = false;
        HeatBurstTick++;
    }

    // 기본공격이 나갈 때 (모든 클라이언트): 공격 범위 앞쪽에 베기 이펙트를 한 번 재생
    protected override void OnAttackVisual()
    {
        if (attackEffectPrefab == null || weaponPoint == null)
            return;

        // 무기 스프라이트는 기본이 왼쪽이므로, 월드 스케일이 양수면 왼쪽, 음수면 오른쪽을 보는 중
        float facingDirection = transform.lossyScale.x >= 0f ? -1f : 1f;

        Vector3 effectPosition = weaponPoint.position + new Vector3(facingDirection * HitboxOffset, 0f, 0f);

        // 이펙트 스프라이트도 기본이 왼쪽 기준이라 오른쪽을 볼 때만 좌우 반전
        RuntimeEffects.PlayAnimatedEffect(attackEffectPrefab, effectPosition, flipX: facingDirection > 0f);
    }

    // 강화 상태가 켜지거나 꺼질 때 (모든 클라이언트): 단검 색 변경 + 불꽃 기운
    private void OnHeatChanged()
    {
        bool ready = HeatReady;

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = ready ? heatTint : originalColors[i];

        if (flameAura == null)
        {
            flameAura = RuntimeEffects.CreateAura(
                transform, Vector3.zero,
                flameColor,
                new Color(1f, 0.1f, 0f, 0f),
                emissionRate: 22f, speed: 1.5f, size: 0.1f, lifetime: 0.5f, radius: 0.2f
            );
        }

        if (ready)
            flameAura.Play();
        else
            flameAura.Stop();
    }

    // 강화 공격이 나가는 순간 (모든 클라이언트): 불꽃이 터짐
    private void OnHeatBurst()
    {
        RuntimeEffects.PlayBurst(
            transform.position,
            new Color(1f, 0.8f, 0.3f, 1f),
            new Color(1f, 0.15f, 0f, 0f),
            count: 30, speed: 5f, size: 0.14f, lifetime: 0.45f
        );
    }
}
