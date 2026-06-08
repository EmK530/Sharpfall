using UnityEngine;
using UnityEngine.UI;

public class PageButton : MonoBehaviour
{
    public string AssociatedPageName;

    void PageClicked()
    {
        Screen_Settings.DisplayPage(AssociatedPageName);
    }

    void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(PageClicked);
    }
}
