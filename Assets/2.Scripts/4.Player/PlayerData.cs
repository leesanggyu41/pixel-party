using Fusion;
using UnityEngine;

public class PlayerData : NetworkBehaviour, IDamageable
{
    // 패시브/영구 업그레이드 수치
    [Networked] public int UpgradeBonusHp { get; set; } = 0;
    [Networked] public float UpgradeBonusDamageMultiplier { get; set; } = 0f;
    [Networked] public int UpgradeBonusCooltime { get; set; } = 0;
    [Networked] public int UpgradeBonusMana { get; set; } = 0;

    [Header("Level / Economy")]
    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int Level { get; set; } = 1;

    [Networked, OnChangedRender(nameof(OnExperienceChanged))]
    public int Experience { get; set; } = 0;

    [Networked, OnChangedRender(nameof(OnGoldChanged))]
    public int Gold { get; set; } = 0;



    // 네트워크 동기화용 최종 HP 수치
    [Networked, OnChangedRender(nameof(OnHpChanged))] public int CurrentHp { get; set; } = 16;
    [Networked, OnChangedRender(nameof(OnMaxHpChanged))] public int MaxHp { get; set; } = 16;
    [Networked, OnChangedRender(nameof(OnManaChanged))] public int CurrentMana { get; set; } = 80;
    [Networked, OnChangedRender(nameof(OnMaxManaChanged))] public int MaxMana { get; set; } = 80;

    [Networked] public float finalDamageMultiplier { get; set; } = 0;
    [Networked] public int finalCooltime { get; set; } = 0;

    // 아바타가 변경되거나 업그레이드될 때 호출
    public void RecalculateMaxHp(int avatarBaseHp)
    {
        int prevMaxHp = MaxHp;

        // 최종 MaxHP = 아바타 기본 체력 + 영구 업그레이드 체력
        MaxHp = avatarBaseHp + UpgradeBonusHp;

        // 전직/스탯 변경 시 늘어난 체력 비율만큼 CurrentHp도 자연스럽게 보정
        int hpDifference = MaxHp - prevMaxHp;
        if (hpDifference > 0)
        {
            CurrentHp += hpDifference;
        }
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
    }

    public void RecalculateMaxMana(int avatarBaseMana)
    {
        int prevMaxMana = MaxMana;
        // 최종 MaxMana = 아바타 기본 마나 + 영구 업그레이드 마나
        MaxMana = avatarBaseMana + UpgradeBonusMana;

        // 전직/스탯 변경 시 늘어난 마나 비율만큼 CurrentMana도 자연스럽게 보정
        int manaDifference = MaxMana - prevMaxMana;
        if (manaDifference > 0)
        {
            CurrentMana += manaDifference;
        }
        CurrentMana = Mathf.Clamp(CurrentMana, 0, MaxMana);
    }

    public void RecalculateMana(int avatarBaseMana)
    {
        CurrentMana = Mathf.Clamp(avatarBaseMana, 0, MaxMana); // 기존 CurrentMana가 아니라 avatarBaseMana를 기준으로 세팅
    }

    public void RecalculateDamageMultiplier(float avatarBaseDamageMultiplier)
    {
        finalDamageMultiplier = avatarBaseDamageMultiplier + UpgradeBonusDamageMultiplier;
    }

    public void RecalculateCooltime(int avatarBaseCooltime)
    {
        finalCooltime = avatarBaseCooltime + UpgradeBonusCooltime;
    }

    // ===== 상점 업그레이드 =====
    public const int UpgradeHp = 0;
    public const int UpgradeDamage = 1;
    public const int UpgradeCooldown = 2;

    [Header("상점 업그레이드 (1회 구매당 증가량)")]
    [SerializeField] private int hpPerUpgrade = 4;                 // 체력 +4 (하트 1칸)
    [SerializeField] private float damagePerUpgrade = 0.1f;        // 공격력 +10%
    [SerializeField] private int cooldownPercentPerUpgrade = 5;    // 스킬 쿨타임 -5%
    [SerializeField] private int maxCooldownReductionPercent = 50; // 쿨타임 감소 상한(%)

    [Header("상점 가격 = 기본 가격 + 구매 횟수 × 증가분")]
    [SerializeField] private int hpBaseCost = 50;
    [SerializeField] private int hpCostIncrease = 25;
    [SerializeField] private int damageBaseCost = 50;
    [SerializeField] private int damageCostIncrease = 25;
    [SerializeField] private int cooldownBaseCost = 50;
    [SerializeField] private int cooldownCostIncrease = 25;

    [Networked] public int HpUpgradeLevel { get; set; }
    [Networked] public int DamageUpgradeLevel { get; set; }
    [Networked] public int CooldownUpgradeLevel { get; set; }

    public int GetUpgradeLevel(int type)
    {
        switch (type)
        {
            case UpgradeHp: return HpUpgradeLevel;
            case UpgradeDamage: return DamageUpgradeLevel;
            case UpgradeCooldown: return CooldownUpgradeLevel;
            default: return 0;
        }
    }

    public int GetUpgradeCost(int type)
    {
        int level = GetUpgradeLevel(type);
        switch (type)
        {
            case UpgradeHp: return hpBaseCost + hpCostIncrease * level;
            case UpgradeDamage: return damageBaseCost + damageCostIncrease * level;
            case UpgradeCooldown: return cooldownBaseCost + cooldownCostIncrease * level;
            default: return int.MaxValue;
        }
    }

    public bool IsUpgradeMaxed(int type)
    {
        return type == UpgradeCooldown &&
               UpgradeBonusCooltime + cooldownPercentPerUpgrade > maxCooldownReductionPercent;
    }

    // 상점 버튼에서 호출 - 로컬 플레이어의 구매 요청을 호스트로 보낸다
    public void RequestUpgrade(int type)
    {
        if (!Object.HasInputAuthority) return;

        RPC_RequestUpgrade(type);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUpgrade(int type)
    {
        if (IsUpgradeMaxed(type)) return;
        if (!TrySpendGold(GetUpgradeCost(type))) return;

        switch (type)
        {
            case UpgradeHp:
                HpUpgradeLevel++;
                UpgradeBonusHp += hpPerUpgrade;
                break;

            case UpgradeDamage:
                DamageUpgradeLevel++;
                UpgradeBonusDamageMultiplier += damagePerUpgrade;
                break;

            case UpgradeCooldown:
                CooldownUpgradeLevel++;
                UpgradeBonusCooltime += cooldownPercentPerUpgrade;
                break;
        }

        RecalculateFromAvatar();
    }

    // 아바타의 기본 스탯 + 업그레이드 수치로 최종 스탯을 다시 계산
    private void RecalculateFromAvatar()
    {
        // PlayerAvatar는 플레이어 루트의 자식으로 스폰되므로 자식에서 찾는다
        PlayerAvatar avatar = GetComponentInChildren<PlayerAvatar>();
        if (avatar == null) return;

        RecalculateMaxHp(avatar.BaseMaxHp);
        RecalculateDamageMultiplier(avatar.BaseDamageMultiplier);
        RecalculateCooltime(avatar.BaseCooltime);
    }

    // 스킬 쿨타임 감소(%) 적용: 10초, 30% 감소 → 7초
    public float ApplyCooldownReduction(float baseCooldown)
    {
        float percent = Mathf.Clamp(finalCooltime, 0, maxCooldownReductionPercent);
        return baseCooldown * (1f - percent / 100f);
    }

    public void AddExperience(int amount)
    {
        if (!HasStateAuthority) return;

        if (amount <= 0) return;

        Experience += amount;

        CheckLevelUp();
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestGoldReward(int amount)
    {
        if (amount <= 0)
            return;

        AddGold(amount);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestExperienceReward(int amount)
    {
        if (amount <= 0)
            return;

        AddExperience(amount);
    }

    private void CheckLevelUp()
    {
        int requiredExp = GetRequiredExperience();

        while (Experience >= requiredExp)
        {
            Experience -= requiredExp;
            Level++;

            requiredExp = GetRequiredExperience();

            Debug.Log(
                $"[{Object.InputAuthority}] 레벨업! 현재 레벨: {Level}"
            );
        }

        // 전직 레벨에 도달했다면 들고 있는 무기에 맞는 직업으로 전직
        if (TryGetComponent(out AvatarController avatarController))
        {
            avatarController.TryPromote();
        }
    }
    public int GetRequiredExperience()
    {
        return 100 + (Level - 1) * 50;
    }
    public void AddGold(int amount)
    {
        if (!HasStateAuthority) return;

        if (amount <= 0) return;

        Gold += amount;

        Debug.Log(
            $"[{Object.InputAuthority}] 골드 획득: +{amount} / 현재 골드: {Gold}"
        );
    }
    public bool TrySpendGold(int amount)
    {
        if (!HasStateAuthority) return false;

        if (amount <= 0) return false;
        if (Gold < amount) return false;

        Gold -= amount;

        return true;
    }

    // IDamageable 구현 - 무기 쪽에서 TryGetComponent<IDamageable>로 호출
    public void TakeDamage(float amount)
    {
        if (!HasStateAuthority) return;

        CurrentHp = Mathf.Clamp(CurrentHp - Mathf.RoundToInt(amount), 0, MaxHp);

        if (CurrentHp <= 0)
        {
            // TODO: 사망 처리
        }
    }

    private void OnLevelChanged()
    {
        InGameUIManager.Instance?.OnPlayerLevelChanged(this);
    }

    private void OnExperienceChanged()
    {
        InGameUIManager.Instance?.OnPlayerExperienceChanged(this);
    }

    private void OnGoldChanged()
    {
        InGameUIManager.Instance?.OnPlayerGoldChanged(this);
    }

    private void OnHpChanged() => InGameUIManager.Instance?.OnPlayerHpChanged(this);
    private void OnMaxHpChanged() => InGameUIManager.Instance?.OnPlayerMaxHpChanged(this);
    private void OnManaChanged() => InGameUIManager.Instance?.OnPlayerManaChanged(this);
    private void OnMaxManaChanged() => InGameUIManager.Instance?.OnPlayerMaxManaChanged(this);
}