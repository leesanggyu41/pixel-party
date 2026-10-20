using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShopUI: MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("Shop UI")]
    [SerializeField] private RectTransform shopPanel;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float slideDistance = 500f;

    [Header("업그레이드 정보 텍스트 (선택, 비워두면 갱신 안 함)")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text hpCostText;
    [SerializeField] private TMP_Text damageCostText;
    [SerializeField] private TMP_Text cooldownCostText;

    [Header("구매 버튼 (코드에서 클릭 이벤트를 연결)")]
    [SerializeField] private Button hpButton;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button cooldownButton;

    private PlayerData localPlayer;
    private Vector2 originalPosition;
    private bool isOpen = false;
    private bool isAnimating = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        originalPosition = shopPanel.anchoredPosition;

        if (hpButton != null) hpButton.onClick.AddListener(OnClickHpUpgrade);
        if (damageButton != null) damageButton.onClick.AddListener(OnClickDamageUpgrade);
        if (cooldownButton != null) cooldownButton.onClick.AddListener(OnClickCooldownUpgrade);
    }

    private void Start()
    {
        // 시작할 때는 안 보이게 아래쪽으로 이동
        shopPanel.anchoredPosition = originalPosition + Vector2.down * slideDistance;
        shopPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
         if (Keyboard.current != null &&
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isOpen)
                CloseShop();
            else
                OpenShop();
        }

        if (isOpen)
            RefreshTexts();
    }

    // ===== 버튼 OnClick에 연결 =====
    public void OnClickHpUpgrade() => RequestUpgrade(PlayerData.UpgradeHp);
    public void OnClickDamageUpgrade() => RequestUpgrade(PlayerData.UpgradeDamage);
    public void OnClickCooldownUpgrade() => RequestUpgrade(PlayerData.UpgradeCooldown);

    private void RequestUpgrade(int type)
    {
        if (!TryGetLocalPlayer()) return;

        localPlayer.RequestUpgrade(type);
    }

    private bool TryGetLocalPlayer()
    {
        if (localPlayer != null && localPlayer.Object != null && localPlayer.Object.HasInputAuthority)
            return true;

        localPlayer = null;

        foreach (PlayerData player in FindObjectsByType<PlayerData>(FindObjectsSortMode.None))
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                localPlayer = player;
                return true;
            }
        }

        return false;
    }

    private void RefreshTexts()
    {
        if (!TryGetLocalPlayer()) return;

        SetText(hpText, $"체력 증가:{localPlayer.UpgradeBonusHp}");
        SetText(damageText, $"공격력 증가:{Mathf.RoundToInt(localPlayer.UpgradeBonusDamageMultiplier * 100f)}%");
        SetText(cooldownText, $"쿨타임 감소:{localPlayer.UpgradeBonusCooltime}%");

        SetText(hpCostText, GetCostString(PlayerData.UpgradeHp));
        SetText(damageCostText, GetCostString(PlayerData.UpgradeDamage));
        SetText(cooldownCostText, GetCostString(PlayerData.UpgradeCooldown));
    }

    private string GetCostString(int type)
    {
        return localPlayer.IsUpgradeMaxed(type) ? "MAX" : localPlayer.GetUpgradeCost(type).ToString();
    }

    // 값이 바뀔 때만 텍스트를 갱신해 매 프레임 UI 재생성을 막는다
    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
            target.text = value;
    }

    public void OpenShop()
    {
        if (isOpen || isAnimating) return;

        isOpen = true;
        isAnimating = true;

        shopPanel.gameObject.SetActive(true);

        shopPanel.DOKill();

        shopPanel.DOAnchorPos(originalPosition, animationDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                isAnimating = false;
            });
    }

    public void CloseShop()
    {
        if (!isOpen || isAnimating) return;

        isOpen = false;
        isAnimating = true;

        shopPanel.DOKill();

        shopPanel.DOAnchorPos(
            originalPosition + Vector2.down * slideDistance,
            animationDuration
        )
        .SetEase(Ease.InCubic)
        .OnComplete(() =>
        {
            shopPanel.gameObject.SetActive(false);
            isAnimating = false;
        });
    }
}