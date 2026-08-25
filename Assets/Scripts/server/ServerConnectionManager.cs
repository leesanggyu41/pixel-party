// ServerConnectionManager.cs
// 서버(로비) 접속을 담당하는 싱글톤. 씬이 바뀌어도 유지되어야 하므로 DontDestroyOnLoad.
using UnityEngine;
using Fusion;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;

public class ServerConnectionManager : MonoBehaviour
{
    public static ServerConnectionManager Instance;

    [Header("Scene Index")]
    [SerializeField] private int lobbySceneIndex = 1; // 접속 성공 시 이동할 로비씬

    [Header("UI Reference")]
    [SerializeField] private GameObject errorWindow;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject retryButton;

    private NetworkRunner runner;

    private void Awake()
    {
        // 싱글톤 - 이미 존재하면 이번 인스턴스는 파기 (씬 재진입 시 중복 방지)
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        if (errorWindow != null) errorWindow.SetActive(false);
        await ConnectToServer();
    }

    // Runner 생성 공통 함수 - 이후 방 생성/참가 때도 재사용됨
    private NetworkRunner CreateRunner(string runnerName)
    {
        if (runner != null)
        {
            Destroy(runner.gameObject);
            runner = null;
        }

        GameObject runnerObj = new GameObject(runnerName);
        DontDestroyOnLoad(runnerObj);

        runner = runnerObj.AddComponent<NetworkRunner>();
        runner.ProvideInput = true; // 이 클라이언트가 입력을 보낼 거라는 표시

        return runner;
    }

    public NetworkRunner GetLobbyRunner() => runner;

    // 서버(로비)에 접속 - 씬 시작 시 자동 호출, 재시도 버튼에서도 호출
    public async Task ConnectToServer()
    {
        if (retryButton != null) retryButton.SetActive(false);
        if (errorWindow != null) errorWindow.SetActive(false);
        UpdateStatus("연결 확인 중...");

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowError("네트워크 연결이 없습니다.");
            return;
        }

        CreateRunner("LobbyRunner");

        try
        {
            UpdateStatus("로비 접속 중...");
            var result = await runner.JoinSessionLobby(SessionLobby.ClientServer);

            if (result.Ok)
            {
                UpdateStatus("로비 접속 성공!");
                SceneManager.LoadScene(lobbySceneIndex);
            }
            else
            {
                ShowError($"로비 접속 실패: {result.ShutdownReason}");
            }
        }
        catch (System.Exception e)
        {
            ShowError($"오류 발생: {e.Message}");
        }
    }

    public void OnRetryButtonClick()
    {
        _ = ConnectToServer();
    }

    private void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[Server] {msg}");
    }

    private void ShowError(string msg)
    {
        if (errorWindow != null) errorWindow.SetActive(true);
        if (errorText != null) errorText.text = msg;
        UpdateStatus("접속 실패");
        if (retryButton != null) retryButton.SetActive(true);
        Debug.LogError($"[Server] {msg}");
    }
}