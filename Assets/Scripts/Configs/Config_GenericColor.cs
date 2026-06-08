using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;

public class Config_GenericColor : MonoBehaviour
{
    public string RelatedConfig;

    TMP_InputField input;
    TextMeshProUGUI textLabel;

    bool suppressUIEvents;
    bool isTyping;

    static readonly Regex hexRegex = new Regex(
        "^#?([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$"
    );

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
        Color32 value = (Color32)MasterConfig.GetValue(RelatedConfig);
        ApplyValueToUI(value);

        input.onSelect.AddListener(_ => isTyping = true);
        input.onDeselect.AddListener(_ => isTyping = false);
        input.onValueChanged.AddListener(OnTextChanged);
    }

    void OnTextChanged(string text)
    {
        if (suppressUIEvents)
            return;

        if (TryParseHex(text, out Color32 color))
        {
            MasterConfig.WriteValue(RelatedConfig, color);
            SetLabelColor(true);
        }
        else
        {
            SetLabelColor(false);
        }
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != RelatedConfig)
            return;

        if (isTyping)
            return;

        ApplyValueToUI((Color32)value);
    }

    void ApplyValueToUI(Color32 value)
    {
        suppressUIEvents = true;

        input.SetTextWithoutNotify(ColorToHex(value));

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

    // =========================
    // Hex parsing
    // =========================

    bool TryParseHex(string hex, out Color32 color)
    {
        color = new Color32(255, 255, 255, 255);

        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.Trim();

        if (!hexRegex.IsMatch(hex))
            return false;

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        byte a = 255;
        if (hex.Length == 8)
            a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

        color = new Color32(r, g, b, a);
        return true;
    }

    string ColorToHex(Color32 c)
    {
        return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
    }
}