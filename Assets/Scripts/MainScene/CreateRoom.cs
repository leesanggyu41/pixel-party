using UnityEngine;
using Fusion;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CreateRoom : MonoBehaviour
{
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private TMP_InputField passwordInputField;

    private int MaxPlayers = 5;

    public async void CreateRoomButton()
    {
        string roomName = roomNameInputField.text.Trim();
        string password = passwordInputField.text.Trim();
        bool isPrivate = !string.IsNullOrEmpty(password);

        //나중에 스팀연동해서 스팀이름,s Room 이런식으로 바꿀거임
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "DefaultRoomName";
        }

        int myAscension = PlayerProgressData.GetUnlockedAscension();

        var customProps = new Dictionary<string, SessionProperty>
        {
            { "HasPassword", isPrivate ? 1 : 0 },
            { "Password", isPrivate ? password : "" },
            { "Ascension", myAscension },
        };

        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager가 씬에 없습니다.");
            return;
        }

        await RoomManager.Instance.CreateRoom(roomName, customProps, MaxPlayers);
        Debug.Log($"방 '{roomName}' 생성 요청 완료 (비밀번호 여부: {isPrivate})");
    }
}
