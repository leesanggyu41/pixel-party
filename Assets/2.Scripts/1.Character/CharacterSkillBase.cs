// CharacterSkillBase.cs
using Fusion;
using UnityEngine;

// 캐릭터(전직 클래스)의 고유 스킬 기반 클래스. 아바타 프리팹(PlayerAvatar가 붙은 오브젝트)에 파생 스킬을 붙인다.
//  - C키로 발동하며, 실제 효과(Execute)는 호스트에서만 실행된다
//  - 쿨타임은 소유자의 쿨타임 감소(%)가 적용된다 (10초, 30% 감소 → 7초)
//  - 발동 연출(OnSkillVisual)은 모든 클라이언트에서 실행된다
// 새 스킬은 이 클래스를 상속하고 Execute / OnSkillVisual 만 구현하면 된다.
public abstract class CharacterSkillBase : NetworkBehaviour
{
    [Header("Character Skill")]
    public string skillName = "캐릭터 스킬";
    [TextArea(2, 4)]
    public string skillDescription = "캐릭터 스킬 설명";
    public Sprite skillIcon;

    [Tooltip("스킬 기본 쿨타임(초). 소유자의 쿨타임 감소(%)가 적용된다")]
    [SerializeField] protected float skillCooldown = 10f;

    [Networked] private TickTimer SkillCoolDown { get; set; }

    [Networked, OnChangedRender(nameof(OnSkillTriggered))]
    private int SkillTick { get; set; }

    // 이 아바타를 조종하는 플레이어의 PlayerData (아바타는 플레이어 루트의 자식으로 붙는다)
    protected PlayerData OwnerData => GetComponentInParent<PlayerData>();

    public bool IsSkillReady => SkillCoolDown.ExpiredOrNotRunning(Runner);

    // 남은 쿨타임(초). 스킬 UI에서 사용
    public float SkillCooldownRemaining => SkillCoolDown.RemainingTime(Runner) ?? 0f;

    // 쿨타임 감소(%)가 반영된 실제 쿨타임
    public float FinalSkillCooldown
    {
        get
        {
            PlayerData data = OwnerData;
            return data != null ? data.ApplyCooldownReduction(skillCooldown) : skillCooldown;
        }
    }

    // 입력 처리 쪽(PlayerAvatar)에서 호출
    public void TryUse(float facingDirection)
    {
        if (Object.HasStateAuthority)
        {
            ExecuteInternal(facingDirection);
            return;
        }

        if (Object.HasInputAuthority)
        {
            RPC_RequestSkill(facingDirection);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSkill(float facingDirection)
    {
        if (!Object.HasStateAuthority)
            return;

        ExecuteInternal(facingDirection);
    }

    // State Authority에서만 실행: 쿨타임 확인 → 쿨타임 시작 → 스킬 효과 → 모든 클라이언트에 연출 알림
    private void ExecuteInternal(float facingDirection)
    {
        if (!IsSkillReady)
            return;

        SkillCoolDown = TickTimer.CreateFromSeconds(Runner, FinalSkillCooldown);

        Execute(facingDirection);
        SkillTick++;
    }

    // 스킬의 실제 효과 (호스트에서만 호출됨)
    protected abstract void Execute(float facingDirection);

    private void OnSkillTriggered()
    {
        OnSkillVisual();
    }

    // 스킬 발동 연출 (모든 클라이언트에서 호출됨)
    protected virtual void OnSkillVisual() { }
}
