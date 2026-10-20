using UnityEngine;
using TMPro;

public class NicknameDisplay : MonoBehaviour
{
    public Transform nicknameCanvas; // NicknameCanvas 오브젝트 연결
    [SerializeField] private TextMeshProUGUI nicknameText;

    private NetworkInfo playerInfo;
    private string lastNickname;
    private int lastJoinOrder = int.MinValue;
    private bool lastFacingRight;
    private bool rotationInit;

    private void Awake()
    {
        playerInfo = GetComponent<NetworkInfo>();
    }

    private void LateUpdate()
    {
        if (playerInfo == null || nicknameText == null) return;

        string nickname = playerInfo.Nickname.ToString();
        if (nickname != lastNickname)
        {
            lastNickname = nickname;
            nicknameText.text = nickname;
        }

        int joinOrder = playerInfo.JoinOrder;
        if (joinOrder != lastJoinOrder)
        {
            lastJoinOrder = joinOrder;
            nicknameText.color = PlayerColorutility.GetPlayerColor(joinOrder);
        }

        // 캐릭터(Player 루트)가 오른쪽을 볼 때(localScale.x가 음수) 텍스트도 같이 뒤집히니
        // 캔버스만 반대로 180도 돌려서 상쇄
        bool facingRight = transform.localScale.x < 0f;
        if (!rotationInit || facingRight != lastFacingRight)
        {
            rotationInit = true;
            lastFacingRight = facingRight;
            nicknameCanvas.localRotation = facingRight ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }
    }
}