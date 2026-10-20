// StageMapInfo.cs
using UnityEngine;
using Fusion;

public class StageMapInfo : MonoBehaviour
{
    [Header("플레이어 스폰 위치 (인원 수만큼)")]
    public Transform[] PlayerSpawnPoints;

    [Header("몬스터 스폰 위치")]
    public Transform[] MonsterSpawnPoints;
    public NetworkPrefabRef MonsterPrefab;

    [Header("스폰 위치별 몬스터 (선택) - MonsterSpawnPoints와 같은 순서, 비워둔 칸은 MonsterPrefab 사용")]
    public NetworkPrefabRef[] MonsterPrefabs;

    public NetworkPrefabRef GetMonsterPrefab(int spawnIndex)
    {
        if (MonsterPrefabs != null && spawnIndex < MonsterPrefabs.Length && MonsterPrefabs[spawnIndex].IsValid)
            return MonsterPrefabs[spawnIndex];

        return MonsterPrefab;
    }
}