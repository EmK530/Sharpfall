using System;
using System.Diagnostics;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

class MIDIClock
{
    public static Stopwatch test = new Stopwatch();
    public static double timee = -3d;
    public static ushort cppq = 0;
    public static double bpm = 120d;
    public static double ticklen = 0;
    public static double last = 0;
    public static bool render = false;
    public static bool throttle = false;
    public static double timeLost = 0;
    public static double startTime = 0;
    public static double renLast = 0;
    public static void Start()
    {
        test.Start();
        timeLost = 0d;
        last = 0d;
        renLast = 0;
        startTime = test.ElapsedMilliseconds;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        if(MIDI.loadedMIDI != "Intro")
        {
            timee = -3d / ticklen;
        } else
        {
            timee = -0.5d / ticklen;
        }
    }
    public static void Reset()
    {
        startTime = test.ElapsedMilliseconds;
        last = 0;
        timeLost = 0;
        bpm = 120d;
        renLast = 0;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        timee = 0d;
    }
    public static double GetPassedTime()
    {
        return (double)(test.ElapsedMilliseconds - startTime) / 1000d;
    }
    public static double GetElapsed(bool upd)
    {
        double temp = ((double)GetPassedTime());
        if (render)
        {
            if (upd) {
                renLast += Time.fixedDeltaTime;
            }
            return renLast;
        } else
        {
            if (upd)
            {
                renLast += Time.deltaTime;
            }
        }
        if (throttle)
        {
            if (temp - last > (double)1d / 15d)
            {
                timeLost += (temp - last) - (double)1d / 15d;
                last = temp;
                return temp - timeLost;
            }
        }
        last = temp;
        return temp - timeLost;
    }
    public static void SubmitBPM(double pos, int b)
    {
        double remainder = (timee - pos);
        if (!render)
        {
            timee = pos + (GetElapsed(false) / ticklen);
        } else
        {
            timee = timee + (GetElapsed(false) / ticklen);
        }
        bpm = 60000000 / b;
        //printf("\nNew BPM: %f",bpm);
        timeLost = 0;
        ticklen = ((double)1 / (double)cppq) * ((double)60 / bpm);
        if (!render)
        {
            timee += remainder;
        }
        startTime = test.ElapsedMilliseconds;
        renLast = 0d;
    }
    public static double GetTick()
    {
        return timee + (GetElapsed(true) / ticklen);
    }
}