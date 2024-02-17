using System.IO;
using System.Collections;
using System.Drawing;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using UnityEngine.Rendering;
using System.Diagnostics;
using UnityEngine.Playables;
using TMPro;
using UnityEngine.UI;

public class Screenshot : MonoBehaviour
{
    int frames = 0;
    WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
    string dir = Directory.GetCurrentDirectory();
    private Texture2D tex;

    public GameObject[] configs;
    public TMP_Dropdown dropdown;
    public TextMeshProUGUI CRF;
    public TextMeshProUGUI Bitrate;
    public GameObject FFmpegSettings;

    public static bool firstFind = true;
    public static uint ffmpegCRF = 17;
    public static bool useCRF = true;
    public static bool ffmpegLog = false;
    public static string ffmpegCodec = "libx264";
    private static string[] ffmpegCodecs = new string[]
    {
        "libx264",
        "libx265",
        "h264_nvenc",
        "hevc_nvenc"
    };
    public static uint ffmpegBitrateKB = 50000;
    public static string ffmpegPreset = "veryfast";
    private static string[] ffmpegPresets = new string[]
    {
        "ultrafast",
        "superfast",
        "veryfast",
        "faster",
        "fast",
        "medium",
        "slow",
        "slower",
        "veryslow",
        "placebo"
    };
    public static bool ffmpegAvailable = false;
    private static int renderMode = 0;

    public void SetBitrate(string tx)
    {
        if (tx != "")
        {
            uint num;
            if (uint.TryParse(tx, out num))
            {
                ffmpegBitrateKB = num;
            }
            else
            {
                Bitrate.text = ffmpegBitrateKB.ToString();
            }
        }
    }

    public void SetCRF(string tx)
    {
        if (tx != "")
        {
            uint num;
            if (uint.TryParse(tx, out num))
            {
                ffmpegCRF = num;
            }
            else
            {
                CRF.text = ffmpegCRF.ToString();
            }
        }
    }

    public void SetPreset(int mode)
    {
        ffmpegPreset = ffmpegPresets[mode];
    }

    public void SetLog(bool log)
    {
        ffmpegLog = log;
    }

    public void SetCodec(int mode)
    {
        ffmpegCodec = ffmpegCodecs[mode];
        useCRF = mode <= 1;
        configs[useCRF ? 0 : 1].SetActive(true);
        configs[useCRF ? 1 : 0].SetActive(false);
    }

    [BurstCompile]
    IEnumerator SS()
    {
        //wait for frame end
        yield return frameEnd;
        //read display
        if (tex == null || tex.width != Screen.width || tex.height != Screen.height)
        {
            if (tex != null)
                DestroyImmediate(tex);  // Release the previous texture
            tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        }
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        byte[] bytes;
        if (!Directory.Exists(dir + "\\Render"))
        {
            Directory.CreateDirectory(dir + "\\Render");
        }
        if (renderMode == 1)
        {
            bytes = tex.EncodeToJPG(100);
            File.WriteAllBytes(dir + "\\Render\\frame" + frames + ".jpg", bytes);
        } else
        {
            bytes = tex.EncodeToPNG();
            File.WriteAllBytes(dir + "\\Render\\frame" + frames + ".png", bytes);
        }
        bytes = null;
        frames++;
    }

    Process FFmpegProc = null;

    [BurstCompile]
    IEnumerator SS2()
    {
        yield return frameEnd;
        if (FFmpegProc == null)
        {
            if (!File.Exists(Application.streamingAssetsPath + "/ffmpeg.exe"))
            {
                Application.Quit();
            }
            else
            {
                string qualityOptions,dolog = "";
                if (useCRF)
                {
                    qualityOptions = "-crf " + ffmpegCRF + " -preset " + ffmpegPreset;
                } else
                {
                    qualityOptions = "-b:v " + ffmpegBitrateKB + "K -b_ref_mode 0";
                }
                if (ffmpegLog)
                {
                    dolog = "-report";
                }
                FFmpegProc = new Process();
                FFmpegProc.StartInfo.FileName = Application.streamingAssetsPath + "/ffmpeg.exe";
                FFmpegProc.StartInfo.Arguments = $"-y {dolog} -r {Startup.RenderFPS} -f rawvideo -s {Screen.width}x{Screen.height} -pixel_format rgba -i pipe:0 -c:v {ffmpegCodec} -vf vflip -pix_fmt rgb32 {qualityOptions} {dir + "\\Render.mkv"}";
                FFmpegProc.StartInfo.UseShellExecute = false;
                FFmpegProc.StartInfo.RedirectStandardInput = true;
                FFmpegProc.StartInfo.RedirectStandardOutput = true;
                FFmpegProc.StartInfo.CreateNoWindow = true;
                FFmpegProc.Start();
            }
        }
        if (FFmpegProc != null)
        {
            if (tex == null)
            {
                tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            }
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            Color32[] pixels = tex.GetPixels32();
            byte[] bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pix = pixels[i];
                bytes[i * 4] = pix.r;
                bytes[i * 4 + 1] = pix.g;
                bytes[i * 4 + 2] = pix.b;
                bytes[i * 4 + 3] = pix.a;
            }
            if (FFmpegProc.HasExited)
            {
                Application.Quit();
            }
            else
            {
                FFmpegProc.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                FFmpegProc.StandardInput.BaseStream.Flush();
            }
            yield return null;
        } else
        {
            Application.Quit();
        }
    }

    public static void SetRenderMode(int mode)
    {
        renderMode = mode;
    }

    void LateUpdate()
    {
        if (MIDI.loadedMIDI != "Intro")
        {
            if (MIDIClock.render)
            {
                switch (renderMode)
                {
                    case 0:
                    case 1:
                        StartCoroutine(SS());
                        break;
                    case 2:
                        StartCoroutine(SS2());
                        break;
                }
            }
        }
        else
        {
            if (!ffmpegAvailable && File.Exists(Application.streamingAssetsPath + "/ffmpeg.exe"))
            {
                ffmpegAvailable = true;
                dropdown.options[2].text = "FFmpeg Output";
                if (firstFind == true)
                {
                    dropdown.value = 2;
                    firstFind = false;
                }
            }
            else if (!ffmpegAvailable)
            {
                if (renderMode == 2)
                {
                    dropdown.value = 0;
                    renderMode = 0;
                }
                dropdown.options[2].text = "(FFmpeg UNAVAILABLE)";
            }
            FFmpegSettings.SetActive(renderMode == 2 && MIDIClock.render);
        }
    }
}
