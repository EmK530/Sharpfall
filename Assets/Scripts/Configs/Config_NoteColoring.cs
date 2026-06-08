using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_NoteColoring : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void WriteToConfig(int id)
    {
        MasterConfig.WriteValue("NoteColoring", id);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != "NoteColoring")
            return;
        int val = (int)value;
        dropdown.value = val;
        LibSharpfall.OM_WriteConfig("ColorMode", val);
    }

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        dropdown.onValueChanged.AddListener(WriteToConfig);
    }
}
