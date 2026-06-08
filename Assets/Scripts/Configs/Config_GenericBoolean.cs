using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_GenericBoolean : MonoBehaviour
{
    public string RelatedConfig;
    Toggle input;

    bool suppressUIEvents;

    void Awake()
    {
        input = GetComponent<Toggle>();

        if (input == null)
        {
            Debug.LogError($"{name}: Needs Toggle");
            enabled = false;
            return;
        }


        // Subscribe to config updates
        MasterConfig.ConfigSubscribe(OnConfigUpdate);

        // Initialize UI from config
        bool value = (bool)MasterConfig.GetValue(RelatedConfig);
        ApplyValueToUI(value);

        input.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool value)
    {
        if (suppressUIEvents)
            return;

        MasterConfig.WriteValue(RelatedConfig, value);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != RelatedConfig)
            return;

        bool val = (bool)value;
        ApplyValueToUI(val);
    }

    void ApplyValueToUI(bool value)
    {
        suppressUIEvents = true;

        if (input != null)
            input.isOn = value;

        suppressUIEvents = false;
    }
}