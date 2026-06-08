using System.Linq;
using TMPro;
using UnityEngine;

public class Screen_LibError : MonoBehaviour
{
    public static Screen_LibError _instance;

    public TextMeshProUGUI innerErrorText;

    public void Start()
    {
        _instance = this;
        gameObject.SetActive(false);
    }

    public static void DisplayError(string main, string inner)
    {
        string fullString = $"<color=white>Exception:</color> {main}\n\n<color=white>Inner exception:</color>\n{inner}";
        int lines = fullString.Count(f => f == '\n');
        _instance.innerErrorText.rectTransform.anchorMin = new Vector2(0, 0.6f - 0.04f * lines);
        _instance.innerErrorText.text = fullString;
    }
}
