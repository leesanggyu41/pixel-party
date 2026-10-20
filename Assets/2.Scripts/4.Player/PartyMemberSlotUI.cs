using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyMemberSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerLabel;

    [SerializeField] private Image HpValueImage;
    [SerializeField] private Slider HpSlider;
    [SerializeField] private Slider ManaSlider;

    [SerializeField] private TextMeshProUGUI ManaText;
    [SerializeField] private TextMeshProUGUI HpText;

    private PlayerData playerData;

    public void Setup(NetworkInfo playerInfo)
    {
        playerInfo.TryGetComponent(out playerData); // NetworkPlayerInfo랑 PlayerData가 같은 오브젝트에 있어야 함!

        playerLabel.text = PlayerColorutility.GetLabel(playerInfo.JoinOrder);
        HpValueImage.color = PlayerColorutility.GetPlayerColor(playerInfo.JoinOrder);

        gameObject.SetActive(true);
    }
    public void Clear()
    {
        playerData = null;
        gameObject.SetActive(false);
    }



    void Update()
    {
        if (playerData != null)
        {
            HpSlider.maxValue = playerData.MaxHp;
            HpSlider.value = playerData.CurrentHp;
            HpText.text = $"{playerData.CurrentHp}/{playerData.MaxHp}";

            ManaSlider.maxValue = playerData.MaxMana;
            ManaSlider.value = playerData.CurrentMana;
            ManaText.text = $"{playerData.CurrentMana}/{playerData.MaxMana}";
        }
    }
}
