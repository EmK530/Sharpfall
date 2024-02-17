using System;
using UnityEngine;
using System.Runtime.CompilerServices;

class Sound
{
    public static int engine = 0;
    public static int threshold = 16;
    public static long totalEvents = 0;
    static string lastWinMMDevice = "";
    private static IntPtr? handle;
    static System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
    public static Func<uint, uint> sendTo = stKDMAPI;
    static uint stWinMM(uint ev)
    {
        return WinMM.midiOutShortMsg((IntPtr)handle, ev);
    }
    static uint stXSynth(uint ev)
    {
        return XSynth.SendDirectData(ev);
    }
    static uint stKDMAPI(uint ev)
    {
        return KDMAPI.SendDirectData(ev);
    }
    public static bool Init(int synth, string winMMdev)
    {
        Close();
        switch (synth)
        {
            case 1:
                bool KDMAPIAvailable = false;
                try { KDMAPIAvailable = KDMAPI.IsKDMAPIAvailable(); } catch (DllNotFoundException) { }
                if (KDMAPIAvailable)
                {
                    int loaded = KDMAPI.InitializeKDMAPIStream();
                    if (loaded == 1)
                    {
                        engine = 1;
                        sendTo = stKDMAPI;
                        return true;
                    }
                    else { return false; }
                }
                else { Debug.Log("KDMAPI is not available."); return false; }
            case 2:
                (bool, string, string, IntPtr?, MidiOutCaps?) result = WinMM.Setup(winMMdev);
                if (!result.Item1)
                {
                    Debug.Log(result.Item3);
                    return false;
                }
                else
                {
                    engine = 2;
                    sendTo = stWinMM;
                    handle = result.Item4;
                    lastWinMMDevice = winMMdev;
                    return true;
                }
            case 3:
                bool XSynthAvailable = false;
                try { XSynthAvailable = XSynth.IsKDMAPIAvailable(); } catch (DllNotFoundException) { }
                if (XSynthAvailable)
                {
                    int loaded = XSynth.InitializeKDMAPIStream();
                    if (loaded == 1)
                    {
                        engine = 3;
                        sendTo = stXSynth;
                        return true;
                    }
                    else { Debug.Log("XSynth is not available."); return false; }
                }
                else { return false; }
            default:
                return false;
        }
    }
    static ulong[] noteOffs = new ulong[16];
    public static void Reload()
    {
        for (int i = 0; i < noteOffs.Length; i++)
        {
            noteOffs[i] = 0;
        }
        Close(false);
        switch (engine)
        {
            case 1:
                KDMAPI.InitializeKDMAPIStream();
                return;
            case 2:
                (bool, string, string, IntPtr?, MidiOutCaps?) result = WinMM.Setup(lastWinMMDevice);
                handle = result.Item4;
                return;
            case 3:
                XSynth.InitializeKDMAPIStream();
                return;
        }
    }
    public static void Submit(int ev)
    {
        if (engine != 0)
        {
            int readEvent = ev & 0xFF;
            int type = readEvent & 0b11110000;
            if (type == 0x90)
            {
                int ch = readEvent & 0b00001111;
                int vel = (ev >> 16) & 0xFF;
                if (vel >= threshold)
                {
                    sendTo((uint)ev);
                    noteOffs[ch]++;
                }
            } else if (type == 0x80)
            {
                int ch = readEvent & 0b00001111;
                if (noteOffs[ch] > 0)
                {
                    sendTo((uint)ev);
                    noteOffs[ch]--;
                }
            } else
            {
                sendTo((uint)ev);
            }
            totalEvents++;
        }
    }
    public static void Close(bool clear = true)
    {
        switch (engine)
        {
            case 1:
                KDMAPI.TerminateKDMAPIStream();
                break;
            case 2:
                if (handle != null)
                {
                    WinMM.midiOutClose((IntPtr)handle);
                }
                break;
            case 3:
                XSynth.TerminateKDMAPIStream();
                break;
        }
        if (clear)
        {
            engine = 0;
        }
    }
}