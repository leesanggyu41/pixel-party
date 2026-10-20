using UnityEngine;
using Fusion;

public enum WeaponType
{
    Sword,
    Bow,
    Dagger,
    Staff
}
public enum WeaponUnlockStage
{
    Stage1 = 1,
    Stage2 = 2,
    Stage3 = 3
}

public static class WeaponIds
{
    public const int WoodenBranch = 0;
    public const int IronSword = 1;

    public const int HuntingDagger = 250;
    public const int HuntingBow = 500;
    public const int BeginnerStaff = 750;
}

public abstract class WeaponBase : NetworkBehaviour
{
    [Networked] public PlayerRef Owner { get; set; }

    public string weaponName = "Weapon";
    [Header("Weapon Stats")]
    public float damage = 10f;
    public float attackDelay = 1f;
    public WeaponType weaponType;

    [Header("Reward")]
    public WeaponUnlockStage unlockStage;

    [Header("Weapon References")]
    public Transform weaponPoint;
    public Animator animator;

    [Header("Weapon UI")]
    public Sprite weaponIcon;
    public string skillName = "무기 스킬";
    [TextArea(2, 4)]
    public string skillDescription = "무기 스킬 설명";
    [Header("Weapon Info")]
    public int weaponId;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

    // 프리팹에 설정된 루트 회전 (활처럼 루트가 Y축 180° 회전된 프리팹도 있고, 단검처럼 자식만 회전된 프리팹도 있다).
    // 스폰 직후(부모가 붙기 전)의 값을 저장해 두었다가, 손에 붙일 때 이 값을 로컬 회전으로 사용한다.
    public Quaternion HeldLocalRotation { get; private set; } = Quaternion.identity;

    // 프리팹의 로컬 스케일. 스폰 직후(부모가 없을 때)의 값을 저장해 둔다.
    // 부모 없이 월드 값이 유지된 채 떨어져 나오면 스케일이 -1로 굳어버릴 수 있어서, 붙일 때마다 이 값으로 되돌린다.
    public Vector3 HeldLocalScale { get; private set; } = Vector3.one;

    // Spawned()가 호출된 뒤에만 [Networked] 값에 접근할 수 있다.
    // 소환 직후나 Despawn 직후의 무기에 공격/스킬이 들어오면 예외가 나므로 상태를 추적한다.
    private bool isNetworkReady;

    public bool IsNetworkReady => isNetworkReady;

    public override void Spawned()
    {
        isNetworkReady = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isNetworkReady = false;
    }

    protected virtual void Awake()
    {
        HeldLocalRotation = transform.localRotation;
        HeldLocalScale = transform.localScale;
    }

    protected abstract void PerformAttack(float facingDirection);

    // 이 무기를 들고 있는 플레이어의 PlayerData (없으면 null)
    protected PlayerData OwnerData
    {
        get
        {
            if (Runner != null &&
                Runner.TryGetPlayerObject(Owner, out NetworkObject ownerObject) &&
                ownerObject.TryGetComponent(out PlayerData data))
            {
                return data;
            }

            return null;
        }
    }

    // 이번 공격에만 적용되는 추가 피해 비율 (예: 0.3 = +30%). PrepareAttack에서 설정, 호스트에서만 사용
    protected float AttackDamageBonus { get; set; }

    // 무기 기본 공격력 × 소유자의 공격력 배율(상점 업그레이드 포함) × (1 + 이번 공격 추가 피해)
    protected float FinalDamage
    {
        get
        {
            PlayerData data = OwnerData;
            float multiplier = data != null && data.finalDamageMultiplier > 0f ? data.finalDamageMultiplier : 1f;

            return damage * multiplier * (1f + AttackDamageBonus);
        }
    }

    // ===== 공격속도 =====
    // 1이 기본, 1.2면 20% 빠르게 공격. 버프가 있는 무기(헤이스트 등)가 재정의한다
    protected virtual float GetAttackSpeedMultiplier() => 1f;

    // 공격 판정 직전에 호출 (호스트). 강화 공격 같은 상태를 소비/설정하는 용도
    protected virtual void PrepareAttack() { }

    [Networked] private TickTimer AttackCoolDown { get; set; }

    [Networked, OnChangedRender(nameof(OnAttackTriggered))]
    private int AttackTick { get; set; }

    public void Attack(float facingDirection)
    {
        if (!isNetworkReady)
            return;

        // 호스트 자신이 공격하는 경우
        if (Object.HasStateAuthority)
        {
            if (!AttackCoolDown.ExpiredOrNotRunning(Runner))
                return;

            ExecuteAttack(facingDirection);
            return;
        }

        // 일반 클라이언트는 호스트에게 공격 요청
        if (Object.HasInputAuthority)
        {
            RPC_RequestAttack(facingDirection);
        }
    }

    // State Authority에서만 실제 공격 판정 실행
    private void ExecuteAttack(float facingDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        AttackCoolDown = TickTimer.CreateFromSeconds(Runner, attackDelay / Mathf.Max(0.1f, GetAttackSpeedMultiplier()));

        AttackDamageBonus = 0f;
        PrepareAttack();

        PerformAttack(facingDirection);
        AttackDamageBonus = 0f;
        AttackTick++;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestAttack(float facingDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!AttackCoolDown.ExpiredOrNotRunning(Runner))
            return;

        ExecuteAttack(facingDirection);
    }

    // 공격이 나갈 때 모든 클라이언트에서 호출되는 연출 훅 (공격 이펙트 등). 무기별로 재정의
    protected virtual void OnAttackVisual() { }

    private void OnAttackTriggered()
    {
        OnAttackVisual();

        if (animator != null)
        {
            animator.ResetTrigger(AttackTriggerHash);
            animator.SetTrigger(AttackTriggerHash);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Animator가 연결되어 있지 않습니다!");
        }
    }

    // ===== 스킬 =====
    [Header("Skill")]
    [Tooltip("스킬 기본 쿨타임(초). 소유자의 쿨타임 감소(%)가 적용된다")]
    [SerializeField] protected float skillCooldown = 10f;

    [Networked] private TickTimer SkillCoolDown { get; set; }

    [Networked, OnChangedRender(nameof(OnSkillTriggered))]
    private int SkillTick { get; set; }

    // 스킬이 있는 무기만 true로 재정의
    public virtual bool HasSkill => false;

    public bool IsSkillReady => SkillCoolDown.ExpiredOrNotRunning(Runner);

    // 남은 쿨타임(초). 스킬 UI에서 사용
    public float SkillCooldownRemaining => SkillCoolDown.RemainingTime(Runner) ?? 0f;

    // 쿨타임 감소(%)가 반영된 실제 스킬 쿨타임 (10초 쿨타임에 30% 감소 → 7초)
    public float FinalSkillCooldown
    {
        get
        {
            PlayerData data = OwnerData;
            return data != null ? data.ApplyCooldownReduction(skillCooldown) : skillCooldown;
        }
    }

    public void UseSkill(float facingDirection)
    {
        if (!HasSkill || !isNetworkReady)
            return;

        if (Object.HasStateAuthority)
        {
            ExecuteSkill(facingDirection);
            return;
        }

        if (Object.HasInputAuthority)
        {
            RPC_RequestSkill(facingDirection);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSkill(float facingDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        ExecuteSkill(facingDirection);
    }

    // State Authority에서만 실행: 쿨타임 확인 → 쿨타임 시작 → 스킬 효과 → 모든 클라이언트에 연출 알림
    private void ExecuteSkill(float facingDirection)
    {
        if (!IsSkillReady)
            return;

        SkillCoolDown = TickTimer.CreateFromSeconds(Runner, FinalSkillCooldown);

        OnSkill(facingDirection);
        SkillTick++;
    }

    // 스킬의 실제 효과 (호스트에서만 호출됨). 무기별로 재정의
    protected virtual void OnSkill(float facingDirection) { }

    private void OnSkillTriggered()
    {
        OnSkillVisual();
    }

    // 스킬 발동 연출 (모든 클라이언트에서 호출됨). 무기별로 재정의
    protected virtual void OnSkillVisual() { }
}