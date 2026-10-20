using UnityEngine;
using Fusion;

public class ToRewardManager : MonoBehaviour
{
    // 싱글톤 인스턴스 설정 (외부 접근 가능하도록 수정)
    public static ToRewardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GoldReward(int amount)
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        foreach (var player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                player.RPC_RequestGoldReward(amount);
                return;
            }
        }

        Debug.LogWarning("로컬 플레이어의 PlayerData를 찾지 못했습니다.");
    }

    public void ExperienceReward(int amount)
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        foreach (var player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                player.RPC_RequestExperienceReward(amount);
                return;
            }
        }

        Debug.LogWarning("로컬 플레이어의 PlayerData를 찾지 못했습니다.");
    }

    public void WeaponReward(int weaponId)
    {
        PlayerData[] players = FindObjectsOfType<PlayerData>();

        foreach (PlayerData player in players)
        {
            if (player.Object == null || !player.Object.HasInputAuthority)
                continue;

            WeaponController weaponController =
                player.GetComponent<WeaponController>();

            if (weaponController == null)
            {
                Debug.LogError("플레이어의 WeaponController를 찾을 수 없습니다.");
                return;
            }

            weaponController.RPC_RequestEquipRewardWeapon(weaponId);
            return;
        }

        Debug.LogWarning("로컬 플레이어를 찾지 못했습니다.");
    }
}