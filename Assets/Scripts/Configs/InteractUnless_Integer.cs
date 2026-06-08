using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractUnless_Integer : MonoBehaviour
{
    public string targetConfig;
    public int targetValue;
    public bool reverse = false;

    private Selectable selectable;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        if (selectable == null)
        {
            Debug.LogError($"{nameof(InteractUnless_Integer)} requires a Selectable component.");
            enabled = false;
        }
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        UpdateState((int)MasterConfig.GetValue(targetConfig));
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != targetConfig)
            return;

        UpdateState((int)value);
    }

    void UpdateState(int val)
    {
        bool interactable = (val == targetValue) == !reverse;
        selectable.interactable = interactable;
    }
}
