using System.IO;
using UnityEngine;

public class RenderingSystem : MonoBehaviour
{
    private static RenderingSystem _instance;
    private static readonly string dir = Path.Combine(Directory.GetCurrentDirectory(), "Render");

    void Start()
    {
        _instance = this;
    }

    public static void CoroutineStart()
    {
        _instance.StartCoroutine(CaptureLoop());
    }

    public static System.Collections.IEnumerator CaptureLoop()
    {
        while(true)
        {
            yield return new WaitForEndOfFrame();

            if (UIMain.frameCount == 0)
            {
                UIMain.frameCount++;
                continue;
            }
            if (!UIMain.playingMIDI)
            {
                if(LibSharpfall.ctx != null)
                {
                    LibSharpfall.ffmpeg_close_stdin(LibSharpfall.ctx);
                    LibSharpfall.ctx = null;
                }
                continue;
            }
            PlaybackMode playback = (PlaybackMode)MasterConfig.GetValue("PlaybackMode");
            if (playback == PlaybackMode.Realtime)
                continue;

            int width = (int)MasterConfig.GetValue("RenderWidth");
            int height = (int)MasterConfig.GetValue("RenderHeight");

            if(UIMain.tex == null || UIMain.tex2 == null)
                continue;

            RenderTexture.active = UIMain.tex2;
            UIMain.tex.ReadPixels(new Rect(0, 0, UIMain.tex2.width, UIMain.tex2.height), 0, 0);
            UIMain.tex.Apply(false, false);
            RenderTexture.active = null;

            if (playback == PlaybackMode.PNGRender)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                byte[] bytes = UIMain.tex.EncodeToPNG();
                File.WriteAllBytes(Path.Combine(dir, "frame" + UIMain.frameCount++ + ".png"), bytes);
            } else
            {
                if (LibSharpfall.ctx == null)
                    continue;

                byte[] raw = UIMain.tex.GetRawTextureData();
                if(!LibSharpfall.ffmpeg_write_frame(LibSharpfall.ctx, raw, raw.Length))
                {
                    UIMain.SimulateExitPress();
                }
            }
        }
    }
}