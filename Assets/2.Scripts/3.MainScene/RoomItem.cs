using UnityEngine;
using Fusion;
using TMPro;

public class RoomItem : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private GameObject lockIcon;

    [SerializeField] private GameObject joinButton;

    private SessionInfo _sessionInfo;

    public void Setup(SessionInfo sessionInfo)
    {
        _sessionInfo = sessionInfo;
        roomNameText.text = sessionInfo.Name;
        playerCountText.text = $"{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}";
        if (sessionInfo.Properties.TryGetValue("HasPassword", out var isPasswordProp))
        {
            bool hasPassword = (int)isPasswordProp == 1; // int로 캐스팅 후 비교
            lockIcon.SetActive(hasPassword);
        }
        else
        {
            lockIcon.SetActive(false);
        }
        if (!sessionInfo.IsOpen)
        {
            joinButton.SetActive(false);
        }
        else
        {
            joinButton.SetActive(true);
        }
    }

    public void OnJoinButtonClicked()
    {
        if (lockIcon.activeSelf)
        {
            // 비밀번호 있는 방 - 입력창 띄우고, 맞으면 그때 참가
            // PasswordPromptUI.Instance.Show(_sessionInfo, OnPasswordConfirmed); // 나중에 비밀번호 UI 만들면 연결
        }
        else
        {
            RoomManager.Instance.JoinSession(_sessionInfo);
        }
    }
}
