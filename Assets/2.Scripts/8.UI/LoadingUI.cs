using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;

    public void Show() => loadingPanel.SetActive(true);
    public void Hide() => loadingPanel.SetActive(false);
}
