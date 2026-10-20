using UnityEngine;

public enum RewardType
{
    gold,
    Weapon,
    Experience
}
[System.Serializable]
public class RewardData
{
    public RewardType rewardType;
    public int amount;

    public int weaponId;

    public WeaponBase weapon;
}
