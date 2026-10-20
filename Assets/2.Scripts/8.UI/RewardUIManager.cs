using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class RewardChapterSettings
{
    public int chapterNumber = 1;

    [Header("골드 보상 범위")]
    public int minGold = 10;
    public int maxGold = 50;

    [Header("경험치 보상 범위")]
    public int minExperience = 10;
    public int maxExperience = 50;
}

public class RewardUIManager : MonoBehaviour
{
    public static RewardUIManager Instance { get; private set; }

    [Header("챕터별 보상 설정")]
    [SerializeField] private RewardChapterSettings[] chapterSettings;

    [Header("UI")]
    [SerializeField] private GameObject darkBackground;
    [SerializeField] private RectTransform rewardLayout;

    [Header("Reward Slot Prefabs")]
    [SerializeField] private WeaponRewardSlotUI weaponSlotPrefab;
    [SerializeField] private GoldRewardSlotUI goldSlotPrefab;
    [SerializeField] private ExperienceRewardSlotUI experienceSlotPrefab;

    [Header("Animation")]
    [SerializeField] private Vector2 rewardTargetPosition;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Test Rewards")]
    [SerializeField] private WeaponBase testWeapon;
    [SerializeField] private int testGoldAmount = 100;
    [SerializeField] private int testExperienceAmount = 50;

    private RewardSlotUI[] currentSlots = new RewardSlotUI[3];
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        darkBackground.SetActive(false);
    }

    public void ShowRewardUI(int chapter)
    {
        if (isShowing)
            return;

        isShowing = true;
        darkBackground.SetActive(true);

        SetupRewards(chapter);

        rewardLayout
            .DOAnchorPos(rewardTargetPosition, animationDuration)
            .SetEase(Ease.OutCubic);
    }
    public void SelectReward(RewardSlotUI selectedSlot)
    {
        if (!isShowing)
            return;

        if (selectedSlot == null || selectedSlot.CurrentReward == null)
            return;

        RewardData reward = selectedSlot.CurrentReward;

        Debug.Log($"보상 선택: {reward.rewardType}");

        switch (reward.rewardType)
        {
            case RewardType.Weapon:
                Debug.Log($"선택한 무기: {reward.weapon.weaponName}");
                Debug.Log($"무기 ID: {reward.weaponId}");
                ToRewardManager.Instance.WeaponReward(reward.weaponId);
                break;

            case RewardType.gold:
                Debug.Log($"골드 보상: {reward.amount}");
                ToRewardManager.Instance.GoldReward(reward.amount);
                break;

            case RewardType.Experience:
                Debug.Log($"경험치 보상: {reward.amount}");
                ToRewardManager.Instance.ExperienceReward(reward.amount);
                break;
        }

        StageManager.Instance?.NotifyRewardSelected();

        CloseRewardUI();
    }

    private void CloseRewardUI()
    {
        isShowing = false;

        rewardLayout.DOKill();

        rewardLayout
            .DOAnchorPos(
                new Vector2(rewardLayout.anchoredPosition.x, -1200f),
                animationDuration
            )
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                darkBackground.SetActive(false);
                ClearSlots();
            });
    }

    private void SetupRewards(int chapter)
    {
        ClearSlots();

        RewardData[] rewards = CreateRandomRewards(chapter);

        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i] == null)
                continue;

            RewardSlotUI prefab = GetSlotPrefab(rewards[i].rewardType);

            if (prefab == null)
            {
                Debug.LogError(
                    $"[{rewards[i].rewardType}] 슬롯 프리팹이 연결되지 않았습니다."
                );
                continue;
            }

            RewardSlotUI slot = Instantiate(prefab, rewardLayout);
            slot.SetReward(rewards[i]);
            currentSlots[i] = slot;
        }
    }

    private RewardSlotUI GetSlotPrefab(RewardType type)
    {
        switch (type)
        {
            case RewardType.Weapon:
                return weaponSlotPrefab;

            case RewardType.gold:
                return goldSlotPrefab;

            case RewardType.Experience:
                return experienceSlotPrefab;

            default:
                return null;
        }
    }

    private RewardChapterSettings GetChapterSettings(int chapter)
    {
        foreach (RewardChapterSettings settings in chapterSettings)
        {
            if (settings != null &&
                settings.chapterNumber == chapter)
            {
                return settings;
            }
        }

        Debug.LogError($"챕터 {chapter}의 보상 설정이 없습니다.");
        return null;
    }
    private RewardData CreateRandomReward(
    int chapter,
    RewardChapterSettings settings)
    {
        // 0 = 골드, 1 = 경험치, 2 = 무기
        int type = Random.Range(0, 3);

        switch (type)
        {
            case 0:
                return new RewardData
                {
                    rewardType = RewardType.gold,
                    amount = Random.Range(
                        settings.minGold,
                        settings.maxGold + 1)
                };

            case 1:
                return new RewardData
                {
                    rewardType = RewardType.Experience,
                    amount = Random.Range(
                        settings.minExperience,
                        settings.maxExperience + 1)
                };

            case 2:
                return CreateRandomWeaponReward();

            default:
                return null;
        }
    }
    private RewardData[] CreateRandomRewards(int chapter)
    {
        RewardChapterSettings settings = GetChapterSettings(chapter);

        if (settings == null)
            return new RewardData[0];

        RewardData[] rewards = new RewardData[3];

        for (int i = 0; i < rewards.Length; i++)
        {
            rewards[i] = CreateRandomReward(chapter, settings);

            if (rewards[i] == null)
            {
                Debug.LogWarning($"보상 {i + 1}번 생성에 실패했습니다.");
            }
        }

        return rewards;
    }
    private RewardData CreateWeaponReward(int weaponId)
    {
        WeaponBase weapon = WeaponDatabase.Instance.GetWeapon(weaponId);

        if (weapon == null)
        {
            Debug.LogWarning($"무기 ID {weaponId}를 찾을 수 없습니다.");
            return null;
        }

        return new RewardData
        {
            rewardType = RewardType.Weapon,
            weaponId = weapon.weaponId,
            weapon = weapon
        };
    }

    private RewardData CreateRandomWeaponReward()
    {
        WeaponBase[] weapons = WeaponDatabase.Instance.GetAllWeapons();

        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogWarning("등록된 무기가 없습니다.");
            return null;
        }

        System.Collections.Generic.List<WeaponBase> candidates =
            new System.Collections.Generic.List<WeaponBase>();

        foreach (WeaponBase weapon in weapons)
        {
            if (weapon != null)
                candidates.Add(weapon);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("사용 가능한 무기가 없습니다.");
            return null;
        }

        int randomIndex = Random.Range(0, candidates.Count);
        WeaponBase selectedWeapon = candidates[randomIndex];

        Debug.Log(
            $"랜덤 무기 보상: {selectedWeapon.weaponName}, " +
            $"ID: {selectedWeapon.weaponId}"
        );

        return CreateWeaponReward(selectedWeapon.weaponId);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < currentSlots.Length; i++)
        {
            if (currentSlots[i] != null)
                Destroy(currentSlots[i].gameObject);

            currentSlots[i] = null;
        }
    }
}