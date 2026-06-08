using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_FFmpegCodec : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void WriteToConfig(int id)
    {
        MasterConfig.WriteValue("FFmpegCodec", id);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != "FFmpegCodec")
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
