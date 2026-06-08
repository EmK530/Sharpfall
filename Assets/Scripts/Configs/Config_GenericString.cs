using TMPro;
using UnityEngine;

public class Config_GenericString : MonoBehaviour
{
    public string RelatedConfig;

    TMP_InputField input;
    TextMeshProUGUI textLabel;

    bool suppressUIEvents;
    bool isTyping;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();

        textLabel = transform.Find("Text Area/Text")
            ?.GetComponent<TextMeshProUGUI>();

        if (input == null)
        {
            Debug.LogError($"{name}: Needs TMP_InputField");
            enabled = false;
            return;
        }

        MasterConfig.ConfigSubscribe(OnConfigUpdate);

        // initial value
        string value = (string)MasterConfig.GetValue(RelatedConfig);
        ApplyValueToUI(value);

        input.onSelect.AddListener(_ => isTyping = true);
        input.onDeselect.AddListener(_ => isTyping = false);
        input.onValueChanged.AddListener(OnTextChanged);
    }

    void OnTextChanged(string text)
    {
        if (suppressUIEvents)
            return;

        MasterConfig.WriteValue(RelatedConfig, text);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != RelatedConfig)
            return;

        if (isTyping)
            return;

        ApplyValueToUI((string)value);
    }

    void ApplyValueToUI(string value)
    {
        suppressUIEvents = true;

        input.SetTextWithoutNotify(value);

        SetLabelColor(true);
        suppressUIEvents = false;
    }

    void SetLabelColor(bool valid)
    {
        if (textLabel == null)
            return;

        textLabel.color = valid
            ? new Color32(50, 50, 50, 255)
            : Color.red;
    }
}