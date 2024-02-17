using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PlayButton : MonoBehaviour
{
    public NoteManager noteman;
    public Transform centerCam;
    public GameObject menu;
    public GameObject title;
    Button bt;
    string last = "N/A";

    void Start()
    {
        bt = gameObject.GetComponent<Button>();
        bt.onClick.AddListener(Play);
    }
    void Update()
    {
        if (ButtonHandler.midiPath != last)
        {
            bt.interactable = !Directory.Exists(ButtonHandler.midiPath);
            last = ButtonHandler.midiPath;
        }
    }

    void Play()
    {
        MIDIPlayer.playing = false;
        MIDIPlayer.loop = false;
        MIDIClock.throttle = true;
        if (MIDIClock.render)
        {
            Time.maximumDeltaTime = Time.fixedDeltaTime;
        }
        MIDI.Cleanup();
        if (Sound.engine != 0)
        {
            Sound.Reload();
        }
        noteman.ClearEntities();
        centerCam.gameObject.GetComponent<RotateCam>().enabled = false;
        centerCam.rotation = Quaternion.Euler(0, 0, 0);
        menu.SetActive(false);
        title.SetActive(false);
        Camera.main.GetComponent<CameraScript>().enabled = true;
        MIDI.PreloadPath(ButtonHandler.midiPath);
    }
}
