using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_FFmpegPreset : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void WriteToConfig(int id)
    {
        MasterConfig.WriteValue("FFmpegPreset", id);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != "FFmpegPreset")
            return;
        int val = (int)value;
        dropdown.value = val;
    }

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        dropdown.onValueChanged.AddListener(WriteToConfig);
    }
}
