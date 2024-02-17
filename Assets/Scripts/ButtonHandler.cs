using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public class ButtonHandler : MonoBehaviour
{
    //public int buttonId;
    public static bool busy = false;
    public static string midiPath = "N/A";
    public GameObject[] Menus;

    void Start()
    {
        //gameObject.GetComponent<Button>().onClick.AddListener(() => OnClick(buttonId));
    }

    public void OnClick(int id)
    {
        switch (id)
        {
            case 0:
                if (!busy)
                {
                    FileBrowser.SetFilters(false, new FileBrowser.Filter("MIDI Files", ".mid", ".midi"));
                    FileBrowser.SetDefaultFilter(".mid");
                    busy = true;
                    StartCoroutine(ShowLoadDialogCoroutine());
                }
                //midiPath = EditorUtility.OpenFilePanel("Open MIDI file", "", "mid");
                break;
            case 1:
                Sound.Close();
                break;
        }
    }

    public void TabClick(int id)
    {
        foreach(GameObject i in Menus)
        {
            i.SetActive(false);
        }
        Menus[id-1].SetActive(true);
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Open MIDI file", "Open");
        if (FileBrowser.Success)
        {
            midiPath = FileBrowser.Result[0];
        }
        busy = false;
    }
}
