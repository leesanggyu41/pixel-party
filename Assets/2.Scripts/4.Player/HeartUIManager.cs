// HeartUIManager.cs 수정
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeartUIManager : MonoBehaviour
{
    [Header("HP UI References")]
    [SerializeField] private Transform heartContainer;
    [SerializeField] private GameObject heartPrefab;

    [Header("Mana UI References")]
    [SerializeField] private Slider manaSlider;
    [SerializeField] private TextMeshProUGUI manaText; // 추가

    private List<Image> heartImages = new List<Image>();
    private const int HP_PER_HEART = 4;

    private int currentMaxMana; // 텍스트 표시용으로 저장해둠

    public void InitHearts(int maxHp)
    {
        int heartCount = Mathf.CeilToInt((float)maxHp / HP_PER_HEART);

        // 기존 하트는 재사용하고 부족분만 생성, 남는 하트는 제거
        while (heartImages.Count < heartCount)
        {
            heartImages.Add(Instantiate(heartPrefab, heartContainer).GetComponent<Image>());
        }

        while (heartImages.Count > heartCount)
        {
            int last = heartImages.Count - 1;
            Destroy(heartImages[last].gameObject);
            heartImages.RemoveAt(last);
        }
    }

    public void UpdateHeartDisplay(int currentHp)
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            int minHpForThisHeart = i * HP_PER_HEART;
            int hpInThisHeart = Mathf.Clamp(currentHp - minHpForThisHeart, 0, HP_PER_HEART);
            heartImages[i].fillAmount = (float)hpInThisHeart / HP_PER_HEART;
        }
    }

    public void InitMana(int maxMana)
    {
        currentMaxMana = maxMana;
        manaSlider.maxValue = maxMana;

        if (manaText != null)
        {
            manaText.text = $"{(int)manaSlider.value}/{currentMaxMana}";
        }
    }

    public void UpdateManaDisplay(int currentMana)
    {
        manaSlider.value = currentMana;

        if (manaText != null)
        {
            manaText.text = $"{currentMana}/{currentMaxMana}";
        }
    }
}