using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_SteppingMethod : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void WriteToConfig(int id)
    {
        MasterConfig.WriteValue("SteppingMethod", id);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != "SteppingMethod")
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
