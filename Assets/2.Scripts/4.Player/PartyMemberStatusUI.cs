using UnityEngine;
using System.Collections.Generic;
using Fusion;

public class PartyMemberStatusUI : MonoBehaviour
{
    [SerializeField] private PartyMemberSlotUI slotPrefab;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private NetworkRunner Runner;

    private const int MaxPartyMembers = 4;

    private readonly List<PartyMemberSlotUI> partyMemberSlots = new List<PartyMemberSlotUI>();

    private int localJoinOrder = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Runner = RoomManager.Instance.gameRunner;
        for (int i = 0; i < MaxPartyMembers; i++)
        {
            PartyMemberSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.Clear();
            partyMemberSlots.Add(slot);
        }
    }

    private void Update()
    {
        if (Runner == null)
        {
            if (RoomManager.Instance != null)
            {
                Runner = RoomManager.Instance.gameRunner;
            }
            else
            {
                Debug.LogWarning("Runner is null and RoomManager.Instance is also null.");
                return;
            }
        }

        int slotIndex = 0;

        // Runner.ActivePlayers는 모든 클라이언트에서 동일하게 계산되는 진짜 동기화된 목록
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == Runner.LocalPlayer) continue; // 나 자신은 제외
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject)) continue;
            if (!playerObject.TryGetComponent<NetworkInfo>(out var info)) continue;

            if (slotIndex >=  partyMemberSlots.Count) break;

            partyMemberSlots[slotIndex].Setup(info);
            slotIndex++;
        }

        for (int i = slotIndex; i < partyMemberSlots.Count; i++)
        {
            partyMemberSlots[i].Clear();
        }
    }
}
