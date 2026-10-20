using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;

public class GameStartButton : NetworkBehaviour
{
    [SerializeField] private GameObject _gameStartPanel;
    private void Awake()
    {
        // 권한이 확인되기 전까지는 모두에게 숨겨 둔다 (일반 플레이어에게 잠깐 보이는 것 방지)
        _gameStartPanel.SetActive(false);
    }

    // Start()는 네트워크 오브젝트가 스폰되기 전에 실행될 수 있어 Object가 유효하지 않다.
    // Spawned()에서 권한을 확인해야 방장만 버튼이 보인다.
    public override void Spawned()
    {
        _gameStartPanel.SetActive(Object.HasStateAuthority);
    }

    private void Update()
    {
        if (_gameStartPanel == null || Object == null || !Object.IsValid) return;

        // 방장에게만, 게임이 진행 중이 아닐 때(대기 상태, 스테이지 클리어 후 로비 복귀 포함) Play 버튼을 보여준다
        GameStartManager manager = GameStartManager.Instance;
        bool shouldShow = Object.HasStateAuthority && manager != null && !manager.IsGameStarted;

        if (_gameStartPanel.activeSelf != shouldShow)
            _gameStartPanel.SetActive(shouldShow);
    }

    public void OnClickStartButton()
    {
        if (Object.HasStateAuthority)
        {
            _gameStartPanel.SetActive(false);
            GameStartManager.Instance.StartGame(GameStartManager.Instance.NextStageBuildIndex);
        }
    }
}
