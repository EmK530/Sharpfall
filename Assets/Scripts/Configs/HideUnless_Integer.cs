using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HideUnless_Integer : MonoBehaviour
{
    public string targetConfig;
    public int targetValue;
    public bool reverse = false;

    void Awake()
    {
        MasterConfig.ConfigSubscribe(OnConfigUpdate);
        gameObject.SetActive(((int)MasterConfig.GetValue(targetConfig) == targetValue) == !reverse);
    }

    void OnConfigUpdate(string key, object value)
    {
        if (key != targetConfig)
            return;
        int val = (int)value;
        gameObject.SetActive((val == targetValue) == !reverse);
    }
}
