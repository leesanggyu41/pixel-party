using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Scene Number")]
    [SerializeField] private int mainSceneNumber = 2;

    [Header("Wait UI")]
    [SerializeField] private GameObject waitUI;

    public NetworkRunner gameRunner;


    [SerializeField] private NetworkPrefabRef playerPrefab;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public NetworkRunner GetGameRunner() => gameRunner;
    public async Task CreateRoom(string roomName, Dictionary<string, SessionProperty> customProps, int maxPlayers)
    {
        if (waitUI != null)
        {
            waitUI.SetActive(true);
        }

        if (gameRunner != null && gameRunner.IsRunning)
        {
            await gameRunner.Shutdown();
        }

        gameRunner = NetworkRunnerFactory.Create("GameRunner");

        SceneLoadListener sceneListener = gameRunner.gameObject.AddComponent<SceneLoadListener>();
        sceneListener.Setup(waitUI);
        gameRunner.AddCallbacks(sceneListener);

        SpawnManager spawnManager = gameRunner.gameObject.AddComponent<SpawnManager>();
        spawnManager.playerPrefab = playerPrefab;
        gameRunner.AddCallbacks(spawnManager);

        var result = await gameRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            PlayerCount = maxPlayers,
            SessionProperties = customProps,
            Scene = SceneRef.FromIndex(mainSceneNumber),
            SceneManager = gameRunner.GetComponent<NetworkSceneManagerDefault>(),
        });


        HandleStartResult(result, "방 생성");
    }


    private void HandleStartResult(StartGameResult result, string action)
    {
        if (result.Ok)
        {
            Debug.Log($"{action} 성공");
        }
        else
        {
            if (waitUI != null) waitUI.SetActive(false);
            Debug.LogError($"{action} 실패: {result.ShutdownReason}");
            NetworkRunnerFactory.Destroy(gameRunner);
            gameRunner = null;
        }
    }

    public async void JoinSession(SessionInfo sessionInfo)
    {
        if (waitUI != null)
        {
            waitUI.SetActive(true);
        }

        if (gameRunner != null && gameRunner.IsRunning)
        {
            await gameRunner.Shutdown();
        }

        gameRunner = NetworkRunnerFactory.Create("GameRunner");

        SceneLoadListener sceneListener = gameRunner.gameObject.AddComponent<SceneLoadListener>();
        sceneListener.Setup(waitUI);
        gameRunner.AddCallbacks(sceneListener);

        SpawnManager spawnManager = gameRunner.gameObject.AddComponent<SpawnManager>();
        spawnManager.playerPrefab = playerPrefab;
        gameRunner.AddCallbacks(spawnManager);

        var result = await gameRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionInfo.Name,
            Scene = SceneRef.FromIndex(mainSceneNumber),
            SceneManager = gameRunner.GetComponent<NetworkSceneManagerDefault>(),
        });

        HandleStartResult(result, "방 참가");
    }
}
