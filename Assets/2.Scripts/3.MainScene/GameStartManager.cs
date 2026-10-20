// GameStartManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    public static GameStartManager Instance { get; private set; }

    [Header("씬 인덱스")]
    [SerializeField] private int waitingMapSceneIndex;

    [Header("스테이지 세팅 (현재 로드된 맵 기준으로 갈아끼움)")]
    [SerializeField] private Transform[] playerSpawnPoints; // 이 필드는 맵마다 달라야 하므로, 실제로는 맵 쪽 오브젝트에서 가져오는 게 맞음 (아래 3번 참고)

    [Networked] public NetworkBool IsGameStarted { get; set; }
    [Networked] public int CurrentMapIndex { get; set; } = -1;

    [Networked] public int ClearedStageCount { get; set; }

    private bool waitingForStageMapLoad = false;
    private bool waitingForLobbyReturn = false;

    // 다음에 시작할 스테이지의 빌드 인덱스 (마지막 스테이지를 넘어가면 마지막 스테이지 반복)
    public int NextStageBuildIndex =>
        Mathf.Min(waitingMapSceneIndex + 1 + ClearedStageCount, SceneManager.sceneCountInBuildSettings - 1);

    public override void Spawned()
    {
        Instance = this;
        Runner.AddCallbacks(this);

        if (Object.HasStateAuthority && CurrentMapIndex == -1)
        {
            LoadMap(waitingMapSceneIndex, isStageMap: false);
        }
    }

    public void StartGame(int stageBuildIndex)
    {
        if (!Object.HasStateAuthority || IsGameStarted) return;

        IsGameStarted = true;

        if (Runner.SessionInfo.IsValid)
        {
            Runner.SessionInfo.IsVisible = false;
            Runner.SessionInfo.IsOpen = false;
        }

        LoadMap(stageBuildIndex, isStageMap: true);
    }

    public void LoadMap(int mapBuildIndex, bool isStageMap)
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentMapIndex != -1)
        {
            Runner.UnloadScene(SceneRef.FromIndex(CurrentMapIndex));
        }

        CurrentMapIndex = mapBuildIndex;
        waitingForStageMapLoad = isStageMap; // 스테이지 맵일 때만 이후 몬스터 스폰 절차 진행
        Runner.LoadScene(SceneRef.FromIndex(mapBuildIndex), LoadSceneMode.Additive);
    }

    public void EnterMaintenance()
    {
        LoadMap(waitingMapSceneIndex, isStageMap: false);
    }

    // 스테이지 클리어 후 호출: 스테이지 맵을 언로드하고 로비 맵을 다시 로드한다
    public void ReturnToLobby()
    {
        if (!Object.HasStateAuthority || !IsGameStarted) return;

        ClearedStageCount++;

        // 남아있는 투사체 정리 (씬 언로드로는 사라지지 않는 스폰 오브젝트)
        foreach (Projectile projectile in FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            Runner.Despawn(projectile.Object);
        }

        waitingForLobbyReturn = true;
        LoadMap(waitingMapSceneIndex, isStageMap: false);
    }

    // 씬 로드가 완전히 끝난 시점 - 여기서 위치 이동 + 몬스터 소환 시작
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!Object.HasStateAuthority) return;

        if (waitingForLobbyReturn)
        {
            waitingForLobbyReturn = false;
            MovePlayersToLobby();

            // 로비 맵 로드가 끝난 뒤에야 다시 시작할 수 있게 해서 Play 버튼이 이때 나타난다
            IsGameStarted = false;
            return;
        }

        if (!waitingForStageMapLoad) return;

        waitingForStageMapLoad = false;
        StartCoroutine(SetupStageRoutine());
    }

    private void MovePlayersToLobby()
    {
        // 로비 맵에 StageMapInfo가 있으면 그 스폰 포인트를, 없으면 기본 위치를 사용
        StageMapInfo lobbyInfo = FindFirstObjectByType<StageMapInfo>();
        if (lobbyInfo != null && lobbyInfo.PlayerSpawnPoints != null && lobbyInfo.PlayerSpawnPoints.Length > 0)
        {
            MoveAllPlayersToSpawn(lobbyInfo.PlayerSpawnPoints);
            return;
        }

        int index = 0;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
            {
                playerObj.transform.position = new Vector3(index * 2f - 3f, 1f, 0f);
                index++;
            }
        }
    }

    private IEnumerator SetupStageRoutine()
    {
        // 이 시점에 스테이지 맵의 StageMapInfo(스폰포인트, 몹스폰위치)를 찾음
        StageMapInfo mapInfo = FindFirstObjectByType<StageMapInfo>();
        if (mapInfo == null)
        {
            Debug.LogError("StageMapInfo를 찾을 수 없습니다.");
            yield break;
        }

        // 1. 모든 플레이어를 안전한 스폰 포인트로 이동
        MoveAllPlayersToSpawn(mapInfo.PlayerSpawnPoints);

        yield return new WaitForSeconds(0.2f); // 위치 이동이 네트워크로 전파될 짧은 여유

        // 2. 몬스터 스폰 위치에 느낌표 UI 먼저 표시
        foreach (var spawnPoint in mapInfo.MonsterSpawnPoints)
        {
            RPC_ShowSpawnWarning(spawnPoint.position);
        }

        // 3. 3초 대기
        yield return new WaitForSeconds(3f);

        // 4. 실제 몬스터 소환
        int monsterCount = mapInfo.MonsterSpawnPoints.Length;

        Debug.Log($"[GameStartManager] 몬스터 수: {monsterCount}");

        if (StageManager.Instance == null)
        {
            Debug.LogError("[GameStartManager] StageManager.Instance가 NULL입니다!");
        }
        else
        {
            Debug.Log($"[GameStartManager] StageManager 발견! StateAuthority: {StageManager.Instance.HasStateAuthority}");

            StageManager.Instance.SetMonsterCount(monsterCount);
        }

        for (int i = 0; i < mapInfo.MonsterSpawnPoints.Length; i++)
        {
            Runner.Spawn(
                mapInfo.GetMonsterPrefab(i),
                mapInfo.MonsterSpawnPoints[i].position,
                Quaternion.identity
            );
        }
    }

    private void MoveAllPlayersToSpawn(Transform[] spawnPoints)
    {
        int index = 0;
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject playerObj))
            {
                Transform point = spawnPoints[index % spawnPoints.Length];
                playerObj.transform.position = point.position;
                index++;
            }
        }
    }

    // 느낌표 UI는 로컬 연출이라 모든 클라이언트에서 각자 재생
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowSpawnWarning(Vector3 position)
    {
        SpawnWarningUI.Instance.ShowWarningAt(position, duration: 3f);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    // ... 나머지 INetworkRunnerCallbacks 빈 구현 ...
}