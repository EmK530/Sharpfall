using System.Linq;
using TMPro;
using UnityEngine;

public class Screen_Settings : MonoBehaviour
{
    public static Screen_Settings _instance;

    public GameObject[] pages;

    public void Start()
    {
        _instance = this;
        gameObject.SetActive(false);
    }

    public static void DisplayPage(string name)
    {
        foreach(GameObject page in _instance.pages)
        {
            page.SetActive(page.name.ToLower() == name.ToLower());
        }
    }
}
