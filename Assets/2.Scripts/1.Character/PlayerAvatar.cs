using UnityEngine;
using Fusion;

public class PlayerAvatar : NetworkBehaviour
{
    [Header("Movement Stats")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 60f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Base Life/Combat Stats")]
    [SerializeField] private int baseMaxHp = 16;
    [SerializeField] private float baseDamageMultiplier = 1.0f;
    [SerializeField] private int baseCooltime = 0;
    [SerializeField] private int baseMana = 60;
    [SerializeField] private int baseMaxMana = 60;

    private int lastPushedHp = int.MinValue;
    private int lastPushedMaxHp = int.MinValue;
    private int lastPushedMana = int.MinValue;
    private int lastPushedMaxMana = int.MinValue;

    [Header("References (이 아바타 고유)")]
    [SerializeField] private Transform groundCheck;   // foot
    [SerializeField] private Transform weaponPoint;   // GameObject
    [SerializeField] private Animator animator;

    public float BaseDamageMultiplier => baseDamageMultiplier;
    public int BaseCooltime => baseCooltime;
    public int BaseMaxHp => baseMaxHp;
    public int BaseMana => baseMana;
    public int BaseMaxMana => baseMaxMana;

    private bool hasInitialized = false;
    private bool hasRefreshedUI = false;

    public PlayerData Data { get; private set; }

    private CharacterSkillBase characterSkill;

    public CharacterSkillBase CharacterSkill => characterSkill;

    private void Awake()
    {
        characterSkill = GetComponent<CharacterSkillBase>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!hasInitialized)
        {
            var data = GetComponentInParent<PlayerData>();
            var controller = GetComponentInParent<PlayerController2D>();
            var weaponController = GetComponentInParent<WeaponController>();

            if (data == null || controller == null || weaponController == null) return;

            Init(data, controller, weaponController);
            hasInitialized = true;
        }

        // 캐릭터 고유 스킬 (C키). 전직 캐릭터 프리팹에 CharacterSkillBase 파생 스킬을 붙이면 자동으로 연결된다
        if (characterSkill != null && GetInput(out PlayerNetworkInput input) && input.CharacterSkillInput)
        {
            float facingDirection = Mathf.Sign(-Data.transform.localScale.x);
            characterSkill.TryUse(facingDirection);
        }

        if (Object.HasInputAuthority &&
            InGameUIManager.Instance != null &&
            Data != null)
        {
            if (!hasRefreshedUI)
            {
                InGameUIManager.Instance.RefreshPlayerStatus(Data);
                hasRefreshedUI = true;
            }

            if (Data.MaxHp != lastPushedMaxHp)
            {
                InGameUIManager.Instance.OnPlayerMaxHpChanged(Data);
                lastPushedMaxHp = Data.MaxHp;
            }

            if (Data.CurrentHp != lastPushedHp)
            {
                InGameUIManager.Instance.OnPlayerHpChanged(Data);
                lastPushedHp = Data.CurrentHp;
            }

            if (Data.MaxMana != lastPushedMaxMana)
            {
                InGameUIManager.Instance.OnPlayerMaxManaChanged(Data);
                lastPushedMaxMana = Data.MaxMana;
            }

            if (Data.CurrentMana != lastPushedMana)
            {
                InGameUIManager.Instance.OnPlayerManaChanged(Data);
                lastPushedMana = Data.CurrentMana;
            }
            
        }
    }

    public void Init(PlayerData data, PlayerController2D controller, WeaponController weaponController)
    {
        Data = data;

        if (HasStateAuthority)
        {
            data.RecalculateMaxHp(baseMaxHp);
            data.RecalculateDamageMultiplier(baseDamageMultiplier);
            data.RecalculateCooltime(baseCooltime);
            data.RecalculateMaxMana(baseMaxMana);
            data.RecalculateMana(baseMana);
        }

        controller.SetStats(moveSpeed, acceleration, deceleration, jumpForce,
                             gravityScale, fallGravityMultiplier, coyoteTime, jumpBufferTime);
        controller.SetGroundCheck(groundCheck);
        controller.SetAnimator(animator);

        weaponController.SetWeaponPoint(weaponPoint);
        weaponController.TryEquipStartingWeapon();

        // UI 푸시 로직은 여기서 제거 - FixedUpdateNetwork에서 별도 처리
    }
}