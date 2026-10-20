using UnityEngine;

public class GoldRewardSlotUI : RewardSlotUI
{
    [Header("골드 보상")]
    [SerializeField] private Sprite goldIcon;

    protected override void DisplayReward(RewardData data)
    {
        if (data.rewardType != RewardType.gold)
        {
            Debug.LogWarning("골드 보상 데이터가 올바르지 않습니다.");
            ClearReward();
            return;
        }

        SetRewardInfo(
            "골드",
            goldIcon,
            $"{data.amount} 골드"
        );
    }
}