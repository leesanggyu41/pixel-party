using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System;

public class RoomListManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static RoomListManager Instance { get; private set; }

    [Header("UI 설정")]
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Transform roomListContent;
    [SerializeField] private TMP_Text pageText;

    private int itemsPerPage = 4;
    private int currentPage = 0;
    private List<SessionInfo> allRooms = new List<SessionInfo>();
    private NetworkRunner _runner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
        }
    }

    void Start()
    {
        _runner = ServerConnectionManager.Instance.GetLobbyRunner();
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
            _runner.AddCallbacks(this);
        }
        else
        {
            _ = ServerConnectionManager.Instance.ConnectToServer();
        }
    }

    void Update()
    {
        if (pageText != null)
        {
            pageText.text = $"Page: {currentPage + 1}";
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (sessionList.Count == 0 && allRooms.Count == 0) return;

        allRooms = sessionList.Where(session => session.IsOpen).ToList();
        RefreshPage();

    }
    public void RefreshPage()
    {

        if(roomListContent == null) return;
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allRooms.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            var roomItemGO = Instantiate(roomItemPrefab, roomListContent);
            var roomItem = roomItemGO.GetComponent<RoomItem>();
            roomItem.Setup(allRooms[i]);
        }
    }

    private void CreateRoomItem(SessionInfo sessionInfo)
    {
        var roomItemGO = Instantiate(roomItemPrefab, roomListContent);
        var roomItem = roomItemGO.GetComponent<RoomItem>();
        roomItem.Setup(sessionInfo);
    }

    public async void OnClickRefreshButton()
    {
        if (_runner != null)
        {
            _runner.RemoveCallbacks(this);
            _runner.AddCallbacks(this);
            await _runner.JoinSessionLobby(SessionLobby.ClientServer);
        }
    }

    public void OnClickNextPage()
    {
        if ((currentPage + 1) * itemsPerPage < allRooms.Count)
        {
            currentPage++;
            RefreshPage();
        }
    }

    public void OnClickPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshPage();
        }
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}
