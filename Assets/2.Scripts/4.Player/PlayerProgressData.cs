using UnityEngine;

public static class PlayerProgressData
{
    private const string AscensionKey = "UnlockedAscensionLevel";

    //승천 레벨 반환
    public static int GetUnlockedAscension()
    {
        return PlayerPrefs.GetInt(AscensionKey, 0);
    }

    //게임 클리어 시 승천 레벨 갱신
    public static void ReportClear(int clearedAscensionLevel)
    {
        int current = GetUnlockedAscension();
        int newUnlocked = clearedAscensionLevel + 1;

        if (newUnlocked > current)
        {
            PlayerPrefs.SetInt(AscensionKey, newUnlocked);
            PlayerPrefs.Save();
        }
    }
}
