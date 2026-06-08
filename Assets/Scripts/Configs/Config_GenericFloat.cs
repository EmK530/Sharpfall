using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Config_GenericFloat : MonoBehaviour
{
    public string RelatedConfig;

    TMP_InputField input;
    Slider slider;
    TextMeshProUGUI textLabel;

    bool suppressUIEvents;
    bool isTyping;

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
        float value = (float)MasterConfig.GetValue(RelatedConfig);
        ApplyValueToUI(value);

        if (input != null)
        {
            input.onSelect.AddListener(_ => isTyping = true);
            input.onDeselect.AddListener(_ => {
                isTyping = false;
            });
            input.onValueChanged.AddListener(OnTextChanged);
            Transform tryFind = transform.Find("Text Area/Placeholder");
            if(tryFind != null)
            {
                TextMeshProUGUI placeholder = tryFind.GetComponent<TextMeshProUGUI>();
                if(placeholder != null)
                {
                    placeholder.text = "(float)";
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

        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
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
        if (isTyping)
            return;
        float val = (float)value;
        ApplyValueToUI(val);
    }

    void ApplyValueToUI(float value)
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