using UnityEngine;

public class WeaponRewardSlotUI : RewardSlotUI
{
    protected override void DisplayReward(RewardData data)
    {
        if (data.rewardType != RewardType.Weapon ||
            data.weapon == null)
        {
            Debug.LogWarning("무기 보상 데이터가 올바르지 않습니다.");
            ClearReward();
            return;
        }

        WeaponBase weapon = data.weapon;

        string description =
            $"무기군 : {GetWeaponTypeName(weapon.weaponType)}\n" +
            $"공격력 : {weapon.damage}\n" +
            $"공격 딜레이 : {weapon.attackDelay}\n\n" +
            $"<b>{weapon.skillName}</b>\n" +
            $"{weapon.skillDescription}";

        SetRewardInfo(
            weapon.weaponName,
            weapon.weaponIcon,
            description
        );
    }

    private string GetWeaponTypeName(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Sword:
                return "검";

            case WeaponType.Bow:
                return "활";

            case WeaponType.Dagger:
                return "단검";

            case WeaponType.Staff:
                return "지팡이";

            default:
                return "알 수 없음";
        }
    }
}