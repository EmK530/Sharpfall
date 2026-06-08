using UnityEngine;
using UnityEngine.UI;

public class Config_PlaybackMode : MonoBehaviour
{
    public Button[] buttons;

    void WriteToConfig(int id)
    {
        MasterConfig.WriteValue("PlaybackMode", id);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != "PlaybackMode")
            return;
        int val = (int)value;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button btn = buttons[i];
            ColorBlock colors = btn.colors;
            colors.normalColor = i == val ? (new Color32(160, 254, 254, 255)) : Color.white;
            colors.selectedColor = i == val ? colors.normalColor : Color.white;
            colors.highlightedColor = i == val ? colors.normalColor : new Color32(222, 254, 254, 255);
            btn.colors = colors;
        }
    }

    void Awake()
    {
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        for (int i = 0; i < buttons.Length; i++)
        {
            int excl = i;
            Button btn = buttons[i];
            btn.onClick.AddListener(() => WriteToConfig(excl));
        }
    }

    void Update()
    {
        
    }
}
