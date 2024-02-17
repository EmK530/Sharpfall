using System;
using System.IO;

class ConBuffer
{
    static FileStream midi;
    static byte[] buffer;
    static int bufRange = 0;
    public static int bufSize = 0;
    static long bufPos = 0;
    static long filePos = 0;
    static long curSeek = 0;
    static string pathSave;
    static bool fileEnded = false;

    public static void Init(string path, long seek, int bufSizee)
    {
        bufSize = bufSizee;
        pathSave = path;
        filePos = 0;
        curSeek = 0;
        bufPos = 0;
        midi = new FileStream(path, FileMode.Open, FileAccess.Read);
        buffer = new byte[bufSize];
        bufRange = midi.Read(buffer, 0, bufSize);
        curSeek += filePos + bufRange;
        fileEnded = (bufRange != bufSize);
    }

    public static void UpdateBuffer()
    {
        if (!fileEnded)
        {
            filePos += bufPos;
            midi.Seek(filePos - curSeek, SeekOrigin.Current);
            bufRange = midi.Read(buffer, 0, bufSize);
            curSeek = filePos + bufRange;
            fileEnded = (bufRange != bufSize);
            bufPos = 0;
        }
    }

    public static void Seek(long pos)
    {
        bool cond = pos - filePos >= bufRange;
        bufPos = pos - filePos;
        if (cond)
        {
            UpdateBuffer();
        }
    }

    public static int Pushback = -1;

    public static void Skip(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Pushback != -1)
            {
                Pushback = -1;
            }
            if (bufPos >= bufRange)
            {
                UpdateBuffer();
            }
            bufPos++;
        }
    }

    public static void ResizeBuffer(int size)
    {
        bufSize = size;
        buffer = new byte[size];
        UpdateBuffer();
    }

    public static byte ReadFast()
    {
        if (bufPos >= bufRange)
        {
            UpdateBuffer();
        }
        bufPos++;
        return buffer[bufPos - 1];
    }

    public static byte[] ReadRange(uint size)
    {
        if (bufPos + size >= bufRange)
        {
            UpdateBuffer();
        }
        byte[] range = new byte[size];
        Array.Copy(buffer, (int)bufPos, range, 0, size);
        bufPos += size;
        return range;
    }

    public static void Copy(byte[] target, uint offset, uint size)
    {
        if (bufPos + size >= bufRange)
        {
            UpdateBuffer();
        }
        if (size == 0)
        {
            size = (uint)bufSize;
        }
        Array.Copy(buffer, bufPos, target, offset, size);
        bufPos += size;
    }

    public static void Clean()
    {
        buffer = null;
        midi.Close();
    }
}
