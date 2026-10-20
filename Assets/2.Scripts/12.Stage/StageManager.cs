using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;

public class StageManager : NetworkBehaviour
{
    public static StageManager Instance { get; private set; }

    [Networked]
    public int RemainingMonsterCount { get; private set; }

    [Networked]
    public NetworkBool IsStageCleared { get; private set; }

    [Header("스테이지 정보")]
    [SerializeField] private int chapterNumber = 1;

    [Header("확정 보상 (챕터가 올라갈수록 챕터당 증가량만큼 늘어남)")]
    [SerializeField] private int baseGold = 10;
    [SerializeField] private int goldPerChapter = 10;
    [SerializeField] private int baseExperience = 10;
    [SerializeField] private int experiencePerChapter = 10;

    [Header("보상 선택 제한 시간(초) - 지나면 자동으로 로비 복귀")]
    [SerializeField] private float rewardTimeout = 30f;

    private TickTimer returnTimer;
    private bool returning;
    private readonly HashSet<PlayerRef> rewardSelectedPlayers = new HashSet<PlayerRef>();

    private void Awake()
    {
        Instance = this;
    }

    public void SetMonsterCount(int count)
    {
        if (!HasStateAuthority)
            return;

        RemainingMonsterCount = count;
        IsStageCleared = false;

        Debug.Log($"스테이지 몬스터 수: {RemainingMonsterCount}");
    }

    public void OnMonsterDefeated()
    {
        if (!HasStateAuthority)
            return;

        if (IsStageCleared)
            return;

        RemainingMonsterCount--;

        Debug.Log($"몬스터 처치! 남은 몬스터: {RemainingMonsterCount}");

        if (RemainingMonsterCount <= 0)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        if (IsStageCleared)
            return;

        IsStageCleared = true;

        Debug.Log("스테이지 클리어!");

        GiveGuaranteedRewards();

        rewardSelectedPlayers.Clear();
        returnTimer = TickTimer.CreateFromSeconds(Runner, rewardTimeout);
        RPC_ShowRewardUI();
    }

    // 선택 보상과 별개로, 클리어 시 모든 플레이어에게 챕터에 비례한 확정 보상을 지급 (호스트에서만 실행)
    private void GiveGuaranteedRewards()
    {
        int chapterIndex = Mathf.Max(0, chapterNumber - 1);
        int gold = baseGold + goldPerChapter * chapterIndex;
        int experience = baseExperience + experiencePerChapter * chapterIndex;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
                continue;

            if (!playerObject.TryGetComponent(out PlayerData data))
                continue;

            data.AddGold(gold);
            data.AddExperience(experience);
        }

        Debug.Log($"확정 보상 지급: 골드 {gold}, 경험치 {experience} (챕터 {chapterNumber})");
    }

    // 클라이언트가 보상을 선택했을 때 호출 (모든 플레이어가 고르면 로비로 복귀)
    public void NotifyRewardSelected()
    {
        RPC_RewardSelected();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RewardSelected(RpcInfo info = default)
    {
        if (!IsStageCleared || returning)
            return;

        rewardSelectedPlayers.Add(info.Source);

        if (rewardSelectedPlayers.Count >= Runner.ActivePlayers.Count())
            ReturnToLobby();
    }

    public override void FixedUpdateNetwork()
    {
        // 선택을 안 하는 플레이어가 있어도 제한 시간이 지나면 로비로 복귀
        if (HasStateAuthority && IsStageCleared && !returning && returnTimer.Expired(Runner))
            ReturnToLobby();
    }

    private void ReturnToLobby()
    {
        returning = true;
        returnTimer = TickTimer.None;
        GameStartManager.Instance?.ReturnToLobby();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowRewardUI()
    {
        RewardUIManager.Instance.ShowRewardUI(chapterNumber);
    }
}