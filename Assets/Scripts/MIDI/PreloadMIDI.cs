using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.Entities.UniversalDelegates;
using UnityEngine;

public class MIDI : MonoBehaviour
{
    public static ushort format = 0;
    public static ushort fakeTracks = 0;
    public static ushort realTracks = 0;
    public static ushort ppq = 0;
    public static string loadedMIDI = "";
    public static List<byte[]> tracks = new List<byte[]>();
    static bool TextSearch(string comp)
    {
        uint len = (uint)Encoding.UTF8.GetByteCount(comp);
        string cmp = Encoding.UTF8.GetString(ConBuffer.ReadRange(len));
        return comp == cmp;
    }
    public static void Cleanup()
    {
        MIDIClock.bpm = 120d;
        tracks.Clear();
        fakeTracks = 0;
        realTracks = 0;
        ppq = 0;
    }
    public static void PreloadPath(string path)
    {
        loadedMIDI = path;
        if (path == "Intro")
        {
            path = Application.streamingAssetsPath + "/Intro.mid";
        }
        if (!File.Exists(path))
        {
            Debug.LogError("MIDI file not found");
            return;
        }
        ConBuffer.Init(path, 0, 64);
        if (!TextSearch("MThd"))
        {
            Debug.LogError("MIDI header not found");
            return;
        }
        ConBuffer.Skip(4);
        format = (ushort)(ConBuffer.ReadFast() * 256 + ConBuffer.ReadFast());
        fakeTracks = (ushort)(ConBuffer.ReadFast() * 256 + ConBuffer.ReadFast());
        ppq = (ushort)(ConBuffer.ReadFast() * 256 + ConBuffer.ReadFast());
        ConBuffer.ResizeBuffer(100000000);
        MIDIClock.cppq = ppq;
        print("Copying tracks to memory...");
        while(realTracks < fakeTracks)
        {
            realTracks++;
            if (TextSearch("MTrk"))
            {
                try
                {
                    uint size = (uint)((ConBuffer.ReadFast() * 16777216) + (ConBuffer.ReadFast() * 65536) + (ConBuffer.ReadFast() * 256) + ConBuffer.ReadFast());
                    tracks.Add(new byte[size]);
                    //print("Track " + realTracks + " / " + fakeTracks + " | Size " + size);
                    uint offset = 0;
                    while (size > 0)
                    {
                        uint use = (uint)ConBuffer.bufSize;
                        if (size < use)
                        {
                            use = size;
                        }
                        ConBuffer.Copy(tracks[realTracks - 1], offset, use);
                        offset += use;
                        size -= use;
                    }
                } catch (IndexOutOfRangeException)
                {
                    realTracks--;
                    break;
                }
            } else
            {
                break;
            }
        }
        ConBuffer.Clean();
        NoteManager.LoadMats();
        MIDIPlayer.Play();
    }
}
