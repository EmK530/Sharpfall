using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractUnless_Boolean : MonoBehaviour
{
    public string targetConfig;
    public bool targetValue;
    public bool reverse = false;

    private Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (selectable == null)
        {
            Debug.LogError($"{nameof(InteractUnless_Boolean)} requires a Selectable component.");
            enabled = false;
        }
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        UpdateState((bool)MasterConfig.GetValue(targetConfig));
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != targetConfig)
            return;

        UpdateState((bool)value);
    }

    void UpdateState(bool val)
    {
        bool interactable = (val == targetValue) == !reverse;
        selectable.interactable = interactable;
    }
}
