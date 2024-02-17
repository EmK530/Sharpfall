using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using UnityEngine;

public unsafe class MIDIPlayer : MonoBehaviour
{
    public NoteManager NoteMan;
    public static bool playing = false;
    public static bool loop = true;
    public static bool dead = false;
    public static float deadtimer = 0f;
    static double clock = 0d;
    static bool[] aliveTracks;
    static uint[] trackPositions;
    static long[] trackTimes;
    static byte[] prevEvent;
    static byte[] eventType;
    static int[] currentEvent;
    static bool[] tempstep;
    static int[] pushback;
    public static int parallelThreads = 1;
    static IEnumerator<byte>[] trackReads;
    static uint aliveCount = 0;
    public static void Play()
    {
        dead = false;
        aliveCount = MIDI.realTracks;
        aliveTracks = new bool[MIDI.realTracks];
        trackPositions = new uint[MIDI.realTracks];
        currentEvent = new int[MIDI.realTracks];
        trackTimes = new long[MIDI.realTracks];
        prevEvent = new byte[MIDI.realTracks];
        eventType = new byte[MIDI.realTracks];
        tempstep = new bool[MIDI.realTracks];
        pushback = new int[MIDI.realTracks];
        trackReads = new IEnumerator<byte>[MIDI.realTracks];
        for (int i = 0; i < MIDI.realTracks; i++)
        {
            aliveTracks[i] = true;
            trackPositions[i] = 0;
            trackTimes[i] = 0;
            currentEvent[i] = 0;
            eventType[i] = 0;
            tempstep[i] = true;
            pushback[i] = -1;
            trackReads[i] = MIDI.tracks[i].Cast<byte>().GetEnumerator();
        }
        playing = true;
        MIDIClock.Start();
    }
    [BurstCompile]
    void Update()
    {
        if (playing && NoteManager.ready)
        {
            double newclock = MIDIClock.GetTick();
            if (newclock != clock)
            {
                clock = newclock;
                long clockUInt64 = (long)clock;
                Parallel.For(0, MIDI.realTracks, new ParallelOptions { MaxDegreeOfParallelism = parallelThreads }, i => {
                    //for (ushort i = 0; i < MIDI.realTracks; i++)
                    //{
                    if (aliveTracks[i])
                    {
                        byte eT = eventType[i];
                        int cEv = currentEvent[i];
                        IEnumerator<byte> tR = trackReads[i];
                        long time = trackTimes[i];
                        bool doloop = true;
                        bool doloop2 = true;
                        bool step = tempstep[i];
                        int psh = pushback[i];
                        byte read()
                        {
                            if (psh == -1)
                            {
                                tR.MoveNext();
                                return tR.Current;
                            } else
                            {
                                byte old = (byte)psh;
                                psh = -1;
                                return old;
                            }
                        }
                        void skip(long count)
                        {
                            for (long x = 0; x < count; x++)
                            {
                                tR.MoveNext();
                            }
                        }
                        while (doloop2)
                        {
                            if (step)
                            {
                                byte prev = prevEvent[i];
                                int ev = 0;
                                while (doloop)
                                {
                                    long val = 0;
                                    byte temp = 0;
                                    for (int a = 0; a < 4; a++)
                                    {
                                        temp = read();
                                        if (temp > 0x7F)
                                        {
                                            val = ((val << 7) | (long)(temp & 0x7F));
                                        }
                                        else
                                        {
                                            val = val << 7 | temp;
                                            break;
                                        }
                                    }
                                    time += val;
                                    byte readEvent = read();
                                    if (readEvent < 0x80)
                                    {
                                        psh = readEvent;
                                        readEvent = prev;
                                    }
                                    prev = readEvent;
                                    byte trackEvent = (byte)(readEvent & 0b11110000);
                                    if (trackEvent == 0x90 || trackEvent == 0x80 || trackEvent == 0xA0 || trackEvent == 0xE0 || trackEvent == 0xB0)
                                    {
                                        byte note = read();
                                        byte vel = read();
                                        if (time <= clockUInt64)
                                        {
                                            Sound.Submit((readEvent | (note << 8) | (vel << 16)));
                                            if (trackEvent == 0x90)
                                            {
                                                byte ch = (byte)(readEvent & 0b00001111);
                                                NoteMan.MakeNote((ushort)i, note, ch, (clockUInt64-time)*MIDIClock.ticklen);
                                            }
                                        }
                                        else
                                        {
                                            cEv = (readEvent | (note << 8) | (vel << 16));
                                            doloop = false;
                                            step = false;
                                            eT = 0;
                                            break;
                                        }
                                    }
                                    else if (trackEvent == 0xC0 || trackEvent == 0xD0)
                                    {
                                        byte v1 = read();
                                        if (time <= clockUInt64)
                                        {
                                            Sound.Submit((readEvent | (v1 << 8)));
                                        }
                                        else
                                        {
                                            cEv = (readEvent | (v1 << 8));
                                            doloop = false;
                                            step = false;
                                            eT = 0;
                                            break;
                                        }
                                    }
                                    else if (readEvent == 0)
                                    {
                                        doloop = false;
                                        break;
                                    }
                                    else
                                    {
                                        switch (readEvent)
                                        {
                                            case 0b11110000:
                                                {
                                                    while (read() != 0b11110111) ;
                                                    break;
                                                }
                                            case 0b11110010:
                                                {
                                                    skip(2);
                                                    break;
                                                }
                                            case 0b11110011:
                                                {
                                                    tR.MoveNext();
                                                    break;
                                                }
                                            case 0xFF:
                                                {
                                                    readEvent = read();
                                                    switch (readEvent)
                                                    {
                                                        case 0x51:
                                                            {
                                                                tR.MoveNext();
                                                                ev = 0;
                                                                for (int l = 0; l != 3; l++)
                                                                {
                                                                    byte tmp = read();
                                                                    ev = (ev << 8) | tmp;
                                                                }
                                                                if (time <= clockUInt64)
                                                                {
                                                                    MIDIClock.SubmitBPM(time, ev);
                                                                }
                                                                else
                                                                {
                                                                    cEv = ev;
                                                                    doloop = false;
                                                                    doloop2 = false;
                                                                    step = false;
                                                                    eT = 1;
                                                                }
                                                                break;
                                                            }
                                                        case 0x2F:
                                                            {
                                                                doloop = false;
                                                                aliveTracks[i] = false;
                                                                aliveCount--;
                                                                break;
                                                            }
                                                        default:
                                                            {
                                                                skip(read());
                                                                break;
                                                            }
                                                    }
                                                    break;
                                                }
                                        }
                                    }
                                }
                                trackTimes[i] = time;
                                prevEvent[i] = prev;
                                //trackPositions[i] = tpos;
                                if (!doloop)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (time <= clockUInt64)
                                {
                                    step = true;
                                    switch (eT)
                                    {
                                        case 0:
                                            {
                                                byte readEvent = (byte)(cEv & 0xFF);
                                                byte key = (byte)((cEv >> 8) & 0xFF);
                                                byte ch = (byte)(readEvent & 0b00001111);
                                                Sound.Submit(cEv);
                                                if (((cEv & 0xFF) & 0b11110000) == 0x90)
                                                {
                                                    NoteMan.MakeNote((ushort)i, key, ch, (clockUInt64 - time)*MIDIClock.ticklen);
                                                }
                                                break;
                                            }
                                        case 1:
                                            {
                                                MIDIClock.SubmitBPM(time, cEv);
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        trackTimes[i] = time;
                        currentEvent[i] = cEv;
                        eventType[i] = eT;
                        //trackPositions[i] = tpos;
                        tempstep[i] = step;
                        pushback[i] = psh;
                    }
                });
            }
            if (aliveCount == 0)
            {
                print("Ran out of tracks.");
                playing = false;
                if (loop)
                {
                    for (int i = 0; i < MIDI.realTracks; i++)
                    {
                        aliveTracks[i] = true;
                        trackPositions[i] = 0;
                        trackTimes[i] = 0;
                        currentEvent[i] = 0;
                        eventType[i] = 0;
                        tempstep[i] = true;
                        pushback[i] = -1;
                        trackReads[i].Reset();
                    }
                    playing = true;
                    aliveCount = MIDI.realTracks;
                    MIDIClock.Reset();
                } else
                {
                    dead = true;
                }
            }
        }
        if (dead)
        {
            if (MIDIClock.render)
            {
                deadtimer += Time.fixedDeltaTime;
            }
            else
            {
                deadtimer += Time.deltaTime;
            }
            if(deadtimer > 10f)
            {
                Application.Quit();
            }
        }
        NoteMan.SpawnUpdate();
    }
}