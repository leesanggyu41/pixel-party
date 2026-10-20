using UnityEngine;

public static class PlayerColorutility
{
    private static Color[] playerColors = new Color[]
    {
        new Color(0.9f, 0.2f, 0.2f),   // 1P 빨강
        new Color(0.2f, 0.4f, 0.9f),   // 2P 파랑
        new Color(0.2f, 0.8f, 0.3f),   // 3P 초록
        new Color(0.95f, 0.85f, 0.2f), // 4P 노랑
        new Color(0.8f, 0.4f, 0.9f),   // 5P 보라
    };

    public static Color GetPlayerColor(int playerIndex)
    {
        int index = Mathf.Clamp(playerIndex-1, 0, playerColors.Length - 1); // 이 줄 추가
        return playerColors[index];
    }

    public static string GetLabel(int joinOrder) => $"{joinOrder}P";
}
