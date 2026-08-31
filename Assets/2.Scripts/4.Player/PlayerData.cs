using UnityEngine;
using Fusion;

public class PlayerData : NetworkBehaviour
{
    // 패시브/영구 업그레이드 수치
    [Networked] public int UpgradeBonusHp { get; set; } = 0; 
    [Networked] public float UpgradeBonusDamageMultiplier { get; set; } = 0f;
    [Networked] public int UpgradeBonusCooltime { get; set; } = 0;

    // 네트워크 동기화용 최종 HP 수치
    /// <summary>
    /// [Networked, OnChangedRender(nameof(OnHpChanged))] public int CurrentHp { get; set; } = 16;
    /// </summary>
    //[Networked, OnChangedRender(nameof(OnMaxHpChanged))] public int MaxHp { get; set; } = 16;

    [Networked] public float finalDamageMultiplier { get; set; } = 0;
    [Networked] public int finalCooltime { get; set; } = 0;

    // 아바타가 변경되거나 업그레이드될 때 호출
    // public void RecalculateMaxHp(int avatarBaseHp)
    // {
    //     int prevMaxHp = MaxHp;
        
    //     // 최종 MaxHP = 아바타 기본 체력 + 영구 업그레이드 체력
    //     MaxHp = avatarBaseHp + UpgradeBonusHp;

    //     // 전직/스탯 변경 시 늘어난 체력 비율만큼 CurrentHp도 자연스럽게 보정
    //     int hpDifference = MaxHp - prevMaxHp;
    //     if (hpDifference > 0)
    //     {
    //         CurrentHp += hpDifference;
    //     }
    //     CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
    // }

    public void RecalculateDamageMultiplier(float avatarBaseDamageMultiplier)
    {
        // 최종 데미지 배율 = 아바타 기본 데미지 배율 + 영구 업그레이드 데미지 배율
        finalDamageMultiplier = avatarBaseDamageMultiplier + UpgradeBonusDamageMultiplier;
        // 필요 시 다른 로직 추가 가능
    }
    public void RecalculateCooltime(int avatarBaseCooltime)
    {
        // 최종 쿨타임 = 아바타 기본 쿨타임 + 영구 업그레이드 쿨타임
        finalCooltime = avatarBaseCooltime + UpgradeBonusCooltime;
        // 필요 시 다른 로직 추가 가능
    }

    // 상점에서 체력 업그레이드 클릭 시
    public void AddHpUpgrade(int amount = 4)
    {
        if (!HasStateAuthority) return;

        UpgradeBonusHp += amount;
        
        // 현재 조종 중인 아바타의 BaseHP를 가져와 재계산
        //if (SpawnedAvatar != null && SpawnedAvatar.TryGetComponent<PlayerAvatar>(out var avatar))
        //{
            // RecalculateMaxHp(...) 실행
        //}
    }

    //private void OnHpChanged() => InGameUIManager.Instance.OnPlayerHpChanged(this);
    //private void OnMaxHpChanged() => InGameUIManager.Instance.OnPlayerMaxHpChanged(this);
}