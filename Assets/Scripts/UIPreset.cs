using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIPreset : MonoBehaviour
{
    public GameObject PresetPrompt;
    public Button SaveButton;
    public Button CancelButton;
    public Button SaveButton2;
    public Button DeletePreset;
    public TMP_InputField PresetNameInput;
    public TMP_Dropdown PresetList;

    void SaveListener()
    {
        PresetPrompt.SetActive(true);
    }

    void SaveListener2()
    {
        if (PresetNameInput.text == "")
            return;

        PresetPrompt.SetActive(false);
        MasterConfig.SaveAs(PresetNameInput.text, true);
        UpdateOptions();
    }

    void CancelListener()
    {
        PresetPrompt.SetActive(false);
    }

    void DeleteListener()
    {
        MasterConfig.DeleteCurrentPreset();
        UpdateOptions();
    }

    void UpdateOptions()
    {
        PresetList.ClearOptions();
        List<string> presets = MasterConfig.GetAvailablePresets();
        PresetList.AddOptions(presets);
        int index = 0;
        foreach(string i in presets)
        {
            if(i == MasterConfig.currentPreset.name)
            {
                PresetList.value = index;
                break;
            }
            index++;
        }
    }

    void OnPresetUpdate(int value)
    {
        MasterConfig.LoadPreset(PresetList.options[value].text);
    }

    void Start()
    {
        PresetPrompt.SetActive(false);
        SaveButton.onClick.AddListener(SaveListener);
        CancelButton.onClick.AddListener(CancelListener);
        SaveButton2.onClick.AddListener(SaveListener2);
        PresetList.onValueChanged.AddListener(OnPresetUpdate);
        DeletePreset.onClick.AddListener(DeleteListener);
        UpdateOptions();
    }
}
