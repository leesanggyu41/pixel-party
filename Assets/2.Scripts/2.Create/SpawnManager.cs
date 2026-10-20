using UnityEngine;
using Fusion;
using System;
using System.Collections.Generic;
using Fusion.Sockets;
using UnityEngine.InputSystem;

public class SpawnManager : MonoBehaviour, INetworkRunnerCallbacks
{

    public NetworkPrefabRef playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        // 게임이 이미 시작된 상태면, 이 플레이어는 레이스 컨디션으로 들어온 것 - 즉시 추방
        if (GameStartManager.Instance != null && GameStartManager.Instance.IsGameStarted)
        {
            Debug.LogWarning($"게임 시작 후 입장 시도 감지 - {player} 추방");
            runner.Disconnect(player);
            return;
        }

        Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, 0f);
        NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        runner.SetPlayerObject(player, playerObject);
        spawnedPlayers.Add(player, playerObject);


    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
        {
            runner.Despawn(playerObject);
            spawnedPlayers.Remove(player);
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        return new Vector3(index * 2f, 1f, 0f);
    }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerNetworkInput();

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed)
                data.MoveX = -1f;
            else if (Keyboard.current.rightArrowKey.isPressed)
                data.MoveX = 1f;

            // wasPressedThisFrame 직접 읽는 대신, 래치에 쌓인 값을 소비
            data.JumpInput = LocalInputLatch.JumpPending;
            data.AttackInput = LocalInputLatch.AttackPending;

            data.SkillInput = LocalInputLatch.SkillPending;
            data.CharacterSkillInput = LocalInputLatch.CharacterSkillPending;
            LocalInputLatch.CharacterSkillPending = false;

            LocalInputLatch.JumpPending = false;
            LocalInputLatch.AttackPending = false;
            LocalInputLatch.SkillPending = false;
        }

        input.Set(data);
    }


    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
