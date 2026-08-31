using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class del : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool ispoint;
    public Transform r;
    public Image image;
    public Sprite himage;
    public Sprite jimage;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ispoint = true;
        r.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        image.sprite = himage;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        ispoint = false;
        r.localScale = Vector3.one;
        image.sprite = jimage;
    }
}