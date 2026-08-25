using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform heartContainer; // Horizontal Layout Group이 붙은 부모
    [SerializeField] private GameObject heartPrefab;    // Filled 방식 하트 Prefab

    private List<Image> heartImages = new List<Image>();
    private const int HP_PER_HEART = 4; // 하트 1개당 HP 4

    // 최대 체력에 맞춰 하트 개수를 새로 세팅 (업그레이드 시 호출)
    public void InitHearts(int maxHp)
    {
        foreach (Transform child in heartContainer)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();

        int heartCount = Mathf.CeilToInt((float)maxHp / HP_PER_HEART);

        for (int i = 0; i < heartCount; i++)
        {
            GameObject newHeart = Instantiate(heartPrefab, heartContainer);
            Image heartImg = newHeart.GetComponent<Image>();
            heartImages.Add(heartImg);
        }
    }

    // 현재 HP 수치에 따라 각 하트의 fillAmount를 계산해 갱신
    public void UpdateHeartDisplay(int currentHp)
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            int minHpForThisHeart = i * HP_PER_HEART;
            int hpInThisHeart = Mathf.Clamp(currentHp - minHpForThisHeart, 0, HP_PER_HEART);

            heartImages[i].fillAmount = (float)hpInThisHeart / HP_PER_HEART;
        }
    }
}