using UnityEngine;
using Fusion;

public class PlayerAvatar : NetworkBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseMaxHp = 16; // 전직 클래스별 기본 체력
    [SerializeField] private float baseDamageMultiplier = 1.0f; // 전직 클래스별 기본 데미지 배율
    [SerializeField] private int baseCooltime = 0; // 전직 클래스별 기본 쿨타임

    public PlayerData Data { get; private set; }

    public void Init(PlayerData data)
    {
        Data = data;
        
        // // 전직 스폰 시 최종 MaxHP를 계산해서 PlayerData에 전달
        // if (HasStateAuthority)
        // {
        //     Data.RecalculateMaxHp(baseMaxHp);
        //     Data.RecalculateDamageMultiplier(baseDamageMultiplier);
        //     Data.RecalculateCooltime(baseCooltime);
        // }
    }
}