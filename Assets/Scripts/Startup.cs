using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Startup : MonoBehaviour
{
    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI TFPSInput;
    public TextMeshProUGUI THRInput;
    public TextMeshProUGUI THRESInput;
    public NoteManager noteman;
    public static int RenderFPS = 60;
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime = 1f / 60f;
        //Time.maximumDeltaTime = Time.fixedDeltaTime;
        Sound.Init(1,null);
        MIDIClock.throttle = true;
        MIDI.PreloadPath("Intro");
    }

    string targ = "60";
    string targ2 = "1";
    string targ3 = "24";

    public void SendFPSTarget(string val)
    {
        int num;
        if (int.TryParse(val, out num))
        {
            targ = val;
        }
    }

    public void ApplyFPS()
    {
        int num;
        if (int.TryParse(targ, out num))
        {
            RenderFPS = num;
            Application.targetFrameRate = num;
            Time.fixedDeltaTime = 1f / num;
            if (!MIDIClock.render)
            {
                Time.maximumDeltaTime = 0.3333333f;
            } else
            {
                Time.maximumDeltaTime = Time.fixedDeltaTime;
            }
        }
        TFPSInput.text = targ;
    }
    public void SubmitThreads(string txt)
    {
        uint num;
        if (uint.TryParse(txt, out num))
        {
            targ2 = txt;
        }
    }
    public void ApplyThreads()
    {
        uint num;
        if (uint.TryParse(targ2, out num))
        {
            if (num > 64)
            {
                num = 64;
            }
            MIDIPlayer.parallelThreads = (int)num;
        }
        THRInput.text = targ2;
    }
    public void SubmitThres(string txt)
    {
        uint num;
        if (uint.TryParse(txt, out num))
        {
            targ3 = txt;
        }
    }
    public void ApplyThres()
    {
        uint num;
        if (uint.TryParse(targ3, out num))
        {
            if (num > 127)
            {
                num = 127;
            }
            Sound.threshold = (int)num;
        }
        THRESInput.text = targ3;
    }
    public void RenderToggle(bool val)
    {
        MIDIClock.render = val;
        if (val)
        {
            Time.maximumDeltaTime = Time.fixedDeltaTime;
        } else
        {
            Time.maximumDeltaTime = 0.3333333f;
        }
    }
}
