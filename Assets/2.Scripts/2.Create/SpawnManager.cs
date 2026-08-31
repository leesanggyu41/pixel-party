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
        if (runner.IsServer)
        {
            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, 0f);
            NetworkObject playerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
            Debug.Log($"Player {player.PlayerId} joined. Spawned at {spawnPosition}");
            spawnedPlayers.Add(player, playerObject);
        }
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

            data.JumpInput = Keyboard.current.spaceKey.wasPressedThisFrame;
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
