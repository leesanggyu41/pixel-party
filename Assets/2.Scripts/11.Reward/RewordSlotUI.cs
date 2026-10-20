using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class RewardSlotUI : MonoBehaviour
{
    [Header("공통 UI")]
    [SerializeField] protected TMP_Text nameText;
    [SerializeField] protected Image rewardImage;
    [SerializeField] protected TMP_Text descriptionText;
    [SerializeField] private Button selectButton;

    public RewardData CurrentReward { get; private set; }

    protected virtual void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    public void SetReward(RewardData data)
    {
        if (data == null)
        {
            CurrentReward = null;
            SetRewardInfo("", null, "");
            return;
        }

        CurrentReward = data;
        DisplayReward(data);
    }

    private void OnSelectClicked()
    {
        if (CurrentReward == null)
            return;

        RewardUIManager.Instance?.SelectReward(this);
    }

    protected abstract void DisplayReward(RewardData data);

    protected void SetRewardInfo(
        string rewardName,
        Sprite icon,
        string description)
    {
        if (nameText != null)
            nameText.text = rewardName;

        if (rewardImage != null)
        {
            rewardImage.sprite = icon;
            rewardImage.enabled = icon != null;
        }

        if (descriptionText != null)
            descriptionText.text = description;
    }

    protected void ClearReward()
    {
        CurrentReward = null;

        SetRewardInfo("", null, "");
    }
}