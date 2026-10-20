using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    public static InGameUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private HeartUIManager heartUIManager;

    [SerializeField] private TMP_Text levelText;

    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private Slider experienceSlider;

    [SerializeField] private TMP_Text goldText;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    public void RefreshPlayerStatus(PlayerData data)
    {
        if (data == null) return;

        OnPlayerLevelChanged(data);
        OnPlayerExperienceChanged(data);
        OnPlayerGoldChanged(data);

        OnPlayerMaxHpChanged(data);
        OnPlayerHpChanged(data);
        OnPlayerMaxManaChanged(data);
        OnPlayerManaChanged(data);
    }


    // =========================
    // HP
    // =========================

    public void OnPlayerHpChanged(PlayerData playerData)
    {
        if (!playerData.Object.HasInputAuthority) return;

        heartUIManager.UpdateHeartDisplay(playerData.CurrentHp);
    }

    public void OnPlayerMaxHpChanged(PlayerData playerData)
    {
        if (!playerData.Object.HasInputAuthority) return;

        heartUIManager.InitHearts(playerData.MaxHp);
    }


    // =========================
    // Mana
    // =========================

    public void OnPlayerManaChanged(PlayerData playerData)
    {
        if (!playerData.Object.HasInputAuthority) return;

        heartUIManager.UpdateManaDisplay(playerData.CurrentMana);
    }

    public void OnPlayerMaxManaChanged(PlayerData playerData)
    {
        if (!playerData.Object.HasInputAuthority) return;

        heartUIManager.InitMana(playerData.MaxMana);
    }


    // =========================
    // Level
    // =========================

    public void OnPlayerLevelChanged(PlayerData data)
    {
        if (!data.Object.HasInputAuthority) return;
        if (levelText == null) return;

        levelText.text = $"Lv. {data.Level}";
    }


    // =========================
    // Experience
    // =========================

    public void OnPlayerExperienceChanged(PlayerData data)
    {
        if (!data.Object.HasInputAuthority) return;

        int requiredExp = data.GetRequiredExperience();

        if (experienceText != null)
        {
            experienceText.text = $"{data.Experience} / {requiredExp}";
        }

        if (experienceSlider != null)
        {
            experienceSlider.maxValue = requiredExp;
            experienceSlider.value = data.Experience;
        }
    }


    // =========================
    // Gold
    // =========================

    public void OnPlayerGoldChanged(PlayerData data)
    {
        if (!data.Object.HasInputAuthority) return;
        if (goldText == null) return;

        goldText.text = data.Gold.ToString();
    }
}