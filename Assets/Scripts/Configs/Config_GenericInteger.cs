using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_GenericInteger : MonoBehaviour
{
    public string RelatedConfig;

    TMP_InputField input;
    Slider slider;
    TextMeshProUGUI textLabel;

    bool suppressUIEvents;

    void Awake()
    {
        input = GetComponent<TMP_InputField>();
        slider = GetComponent<Slider>();

        textLabel = transform.Find("Text Area/Text")
            ?.GetComponent<TextMeshProUGUI>();

        if (input == null && slider == null)
        {
            Debug.LogError($"{name}: Needs TMP_InputField or Slider");
            enabled = false;
            return;
        }


        // Subscribe to config updates
        MasterConfig.ConfigSubscribe(OnConfigUpdate);

        // Initialize UI from config
        int value = (int)MasterConfig.GetValue(RelatedConfig);
        ApplyValueToUI(value);

        if (input != null)
        {
            input.onValueChanged.AddListener(OnTextChanged);
            Transform tryFind = transform.Find("Text Area/Placeholder");
            if(tryFind != null)
            {
                TextMeshProUGUI placeholder = tryFind.GetComponent<TextMeshProUGUI>();
                if(placeholder != null)
                {
                    placeholder.text = "(int)";
                }
            }
        }

        if (slider != null)
        {
            slider.wholeNumbers = true;
            slider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void OnTextChanged(string text)
    {
        if (suppressUIEvents)
            return;

        if (int.TryParse(text, out int result))
        {
            MasterConfig.WriteValue(RelatedConfig, result);
            SetLabelColor(true);
        }
        else
        {
            SetLabelColor(false);
        }
    }

    void OnSliderChanged(float value)
    {
        if (suppressUIEvents)
            return;

        int result = Mathf.RoundToInt(value);
        MasterConfig.WriteValue(RelatedConfig, result);
        SetLabelColor(true);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != RelatedConfig)
            return;

        int val = (int)value;
        ApplyValueToUI(val);
    }

    void ApplyValueToUI(int value)
    {
        suppressUIEvents = true;

        if (input != null)
            input.SetTextWithoutNotify(value.ToString());

        if (slider != null)
            slider.SetValueWithoutNotify(value);

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