// SpawnWarningUI.cs
using UnityEngine;
using System.Collections;

public class SpawnWarningUI : MonoBehaviour
{
    public static SpawnWarningUI Instance { get; private set; }

    [SerializeField] private GameObject warningIconPrefab; // 느낌표 UI 프리팹

    private void Awake()
    {
        Instance = this;
    }

    public void ShowWarningAt(Vector3 worldPosition, float duration)
    {
        GameObject icon = Instantiate(warningIconPrefab, worldPosition, Quaternion.identity);
        StartCoroutine(RemoveAfter(icon, duration));
    }

    private IEnumerator RemoveAfter(GameObject icon, float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(icon);
    }
}