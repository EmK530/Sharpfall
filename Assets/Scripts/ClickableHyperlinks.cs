using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ClickableHyperlinks : MonoBehaviour, IPointerClickHandler
{
    TMP_Text text;
    Camera cam;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        cam = Camera.main;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, null);

        if (linkIndex != -1)
        {
            var info = text.textInfo.linkInfo[linkIndex];
            Application.OpenURL(info.GetLinkID());
        }
    }
}
