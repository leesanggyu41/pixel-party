using UnityEngine;

public class ExperienceRewardSlotUI : RewardSlotUI
{
    [Header("경험치 보상")]
    [SerializeField] private Sprite experienceIcon;

    protected override void DisplayReward(RewardData data)
    {
        if (data.rewardType != RewardType.Experience)
        {
            Debug.LogWarning("경험치 보상 데이터가 올바르지 않습니다.");
            ClearReward();
            return;
        }

        SetRewardInfo(
            "경험치",
            experienceIcon,
            $"{data.amount} 경험치"
        );
    }
}