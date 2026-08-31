using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class MainMenuUIController : MonoBehaviour
{
    [Header("Main UI RectTransforms")]
    [SerializeField] private RectTransform clickToStartText;
    [SerializeField] private RectTransform mainButtonsGroup;

    [Header("Sub Menu Panels")]
    [SerializeField] private RectTransform createRoomPanel;
    [SerializeField] private RectTransform joinRoomPanel;
    [SerializeField] private RectTransform settingPanel;
    [SerializeField] private RectTransform quitPanel;

    [Header("Y Position Settings")]
    [SerializeField] private float textHideY = -600f;   
    [SerializeField] private float buttonsHideY = -800f; 
    [SerializeField] private float buttonsShowY = 0f;    

    [Header("Tween Speed Settings")]
    [SerializeField] private float hideDuration = 0.5f;
    [SerializeField] private float showDuration = 0.6f;
    [SerializeField] private float delayBetween = 0.15f;

    private bool isStarted = false;
    private RectTransform activeSubPanel = null; // 현재 열려있는 서브 패널 추적

    private void Start()
    {
        // 초기 위치 세팅
        if (mainButtonsGroup != null) mainButtonsGroup.anchoredPosition = new Vector2(0, buttonsHideY);
        InitPanelPosition(createRoomPanel);
        InitPanelPosition(joinRoomPanel);
        InitPanelPosition(settingPanel);
        InitPanelPosition(quitPanel);

        // Click to Start 텍스트 펄스 효과
        if (clickToStartText != null)
        {
            clickToStartText.DOScale(1.1f, 1.0f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void InitPanelPosition(RectTransform panel)
    {
        if (panel != null) panel.anchoredPosition = new Vector2(0, buttonsHideY);
    }

    private void Update()
    {
        bool isClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool isTouched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (!isStarted && (isClicked || isTouched))
        {
            StartMainMenuTransition();
        }
    }

    // 1. 게임 시작 (Click to Start -> 메인 버튼 그룹 등장)
    private void StartMainMenuTransition()
    {
        isStarted = true;

        Sequence seq = DOTween.Sequence();

        clickToStartText.DOKill();
        seq.Append(clickToStartText.DOAnchorPosY(textHideY, hideDuration).SetEase(Ease.InBack));
        seq.AppendInterval(delayBetween);
        seq.Append(mainButtonsGroup.DOAnchorPosY(buttonsShowY, showDuration).SetEase(Ease.OutBack));
    }

    // 2. 메인 버튼 -> 서브 패널(Create / Join / Setting)로 이동하는 공통 로직
    private void OpenSubPanel(RectTransform targetPanel)
    {
        if (targetPanel == null) return;

        activeSubPanel = targetPanel;
        Sequence seq = DOTween.Sequence();

        // 메인 버튼 내리기
        seq.Append(mainButtonsGroup.DOAnchorPosY(buttonsHideY, hideDuration).SetEase(Ease.InBack));
        seq.AppendInterval(delayBetween);
        // 대상 서브 패널 올리기
        seq.Append(targetPanel.DOAnchorPosY(0f, showDuration).SetEase(Ease.OutBack));
    }

    // --- 각 메인 버튼 onClick 이벤트 연결용 ---
    public void OnClickCreateRoom() => OpenSubPanel(createRoomPanel);
    public void OnClickJoinRoom()   => OpenSubPanel(joinRoomPanel);
    public void OnClickSetting()    => OpenSubPanel(settingPanel);
    public void OnClickQuit()       => OpenSubPanel(quitPanel);

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 3. 서브 패널의 Back 버튼 클릭 시 -> 메인 버튼 그룹으로 복귀
    public void OnClickBackButton()
    {
        if (activeSubPanel == null) return;

        Sequence seq = DOTween.Sequence();

        // 현재 서브 패널 내리기
        seq.Append(activeSubPanel.DOAnchorPosY(buttonsHideY, hideDuration).SetEase(Ease.InBack));
        seq.AppendInterval(delayBetween);
        // 메인 버튼 그룹 다시 올리기
        seq.Append(mainButtonsGroup.DOAnchorPosY(buttonsShowY, showDuration).SetEase(Ease.OutBack));

        seq.OnComplete(() => {
            activeSubPanel = null;
        });
    }
}