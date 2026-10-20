// PartyManager.cs - 순번 배정용 카운터만 남김
using Fusion;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PartyManager : NetworkBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Networked] public int MaxAllowedAscension { get; set; }
    [Networked] public int SelectedAscension { get; set; }
    [Networked] private int NextJoinOrder { get; set; } // 순번 카운터도 네트워크 값으로

    private Dictionary<PlayerRef, int> playerAscensionRecords = new Dictionary<PlayerRef, int>();

    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    // 호스트에서만 호출됨 (RPC 타겟이 StateAuthority)
    public int GetNextJoinOrder()
    {
        NextJoinOrder++;
        return NextJoinOrder;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ReportAscension(PlayerRef player, int unlockedLevel)
    {
        playerAscensionRecords[player] = unlockedLevel;
        RecalculateMaxAllowed();
    }

    private void RecalculateMaxAllowed()
    {
        Debug.Log($"Recalculating MaxAllowedAscension. Current records: {string.Join(", ", playerAscensionRecords.Select(kv => $"Player{kv.Key.PlayerId}: {kv.Value}"))}");
        MaxAllowedAscension = playerAscensionRecords.Count == 0 ? 0 : playerAscensionRecords.Values.Min();
        if (SelectedAscension > MaxAllowedAscension) SelectedAscension = MaxAllowedAscension;

        if (Object.HasStateAuthority && Runner.SessionInfo.IsValid)
        {
            Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty> { { "Ascension", SelectedAscension } });
        }
    }

    public void SetSelectedAscension(int level)
    {
        if (!Object.HasStateAuthority) return;
        Debug.Log($"Setting SelectedAscension to {level}. MaxAllowedAscension is {MaxAllowedAscension}");
        SelectedAscension = Mathf.Clamp(level, 0, MaxAllowedAscension);
        if (Runner.SessionInfo.IsValid)
        {
            Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty> { { "Ascension", SelectedAscension } });
        }
    }
}