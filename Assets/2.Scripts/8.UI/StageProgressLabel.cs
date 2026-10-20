using TMPro;
using UnityEngine;

// 게임 시작 전에는 기존 텍스트(승천 N)를 그대로 두고,
// 게임이 시작되면 "챕터-스테이지" (1-1, 1-2, 1-3, 2-1 ...) 로 표시한다.
public class StageProgressLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private int stagesPerChapter = 3;

    private string defaultText;
    private string lastText;

    private void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        defaultText = label.text;
    }

    private void Update()
    {
        GameStartManager manager = GameStartManager.Instance;
        if (manager == null) return;

        // 한 번이라도 게임을 시작했다면 (스테이지 진행 중이거나, 클리어 후 로비 복귀 상태) 진행도를 표시
        bool showProgress = manager.IsGameStarted || manager.ClearedStageCount > 0;

        string text = showProgress
            ? GetStageText(manager.ClearedStageCount)
            : defaultText;

        if (text == lastText) return;

        lastText = text;
        label.text = text;
    }

    private string GetStageText(int clearedStageCount)
    {
        int chapter = clearedStageCount / stagesPerChapter + 1;
        int stage = clearedStageCount % stagesPerChapter + 1;
        return $"{chapter}-{stage}";
    }
}
