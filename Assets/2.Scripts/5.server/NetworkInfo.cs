// NetworkPlayerInfo.cs 수정
using Fusion;
using UnityEngine;

public class NetworkInfo : NetworkBehaviour
{
    [Networked] public NetworkString<_16> Nickname { get; set; }
    [Networked] public int JoinOrder { get; set; }

    private bool hasRegistered = false;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            string myNickname = $"Player{Random.Range(1000, 9999)}";
            RPC_ReportNickname(myNickname);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && !hasRegistered && PartyManager.Instance != null)
        {
            JoinOrder = PartyManager.Instance.GetNextJoinOrder();
            hasRegistered = true;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReportNickname(string nickname)
    {
        Nickname = nickname;
    }
}