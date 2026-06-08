using Newtonsoft.Json;
using SimpleFileBrowser;
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    private static bool firstrun = false;

    public static UIMain _instance;

    public Button openFileButton;
    public Button closeFileButton;
    public Button playButton;

    public GameObject ExitToMenuPrompt;
    public Button ExitToMenuButton;
    public Button ClosePromptButton;

    public GameObject MIDIEndingCont;
    public TextMeshProUGUI MIDIEndingText;

    public TextMeshProUGUI fileText;
    public TextMeshProUGUI versionText;
    public GameObject MIDILoadError;

    public Slider threadSlider;

    public GameObject imageObject;
    public GameObject imageBG;
    public RawImage renderImage;

    public GameObject settingsFrame;
    public GameObject[] screens;

    void DisplayScreenID(int id)
    {
        if (fileDialogOpen)
            return;
        int count = 0;
        foreach(GameObject screen in screens)
        {
            count++;
            screen.SetActive(id == count);
        }
    }

    string selectedFilePath = null;
    bool fileDialogOpen = false;
    public static bool playingMIDI = false;
    public static int frameCount = 0;

    IEnumerator ShowLoadDialogCoroutine()
    {
        FileBrowser.SetFilters(false, new FileBrowser.Filter("MIDI Files", ".mid", ".midi"));
        FileBrowser.SetDefaultFilter(".mid");
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Open MIDI file", "Open");
        if(FileBrowser.Success)
        {
            selectedFilePath = FileBrowser.Result[0];
            fileText.text = Path.GetFileName(selectedFilePath);
            playButton.interactable = true;
        } else
        {
            selectedFilePath = null;
            fileText.text = "";
            playButton.interactable = false;
        }
        MIDILoadError.SetActive(false);
        fileDialogOpen = false;
    }

    public static string Md5Short8(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        using (var md5 = MD5.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(bytes);
            var sb = new StringBuilder(8);
            for (int i = 0; i < 4; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }

    public static void SimulateExitPress()
    {
        _instance.ExitToMenuPrompt.SetActive(false);
        playingMIDI = false;
        LibSharpfall.CM_ReloadSynth();
        LibSharpfall.PXU_DeleteAllObjects();
        LibSharpfall.CM_Dispose();
        LibSharpfall.OM_WriteConfig("BlockLimit", 2500);
        LibSharpfall.OM_WriteConfig("LimitX1", 1);
        _instance.selectedFilePath = null;
        _instance.fileText.text = "";
        _instance.playButton.interactable = false;
        _instance.settingsFrame.SetActive(true);

        _instance.imageObject.SetActive(false);
        _instance.imageBG.SetActive(false);
        Camera.main.targetTexture = null;
        RenderTexture.active = null;

        _instance.LoadMenu();
    }

    private void LoadMenu()
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "Menu.bin");
        string tempPath = Path.Combine(Path.GetTempPath(), "SharpfallMenu.mid");
        byte[] key = System.Text.Encoding.ASCII.GetBytes(InternalConfig.FileEncryption);
        byte[] data = File.ReadAllBytes(streamingPath);
        byte[] decrypted = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
            decrypted[i] = (byte)(data[i] ^ key[i % key.Length]);
        frameCount = 0;
        imageObject.SetActive(false);
        imageBG.SetActive(false);
        MIDIEndingCont.SetActive(false);
        File.WriteAllBytes(tempPath, decrypted);
        LibSharpfall.CM_LoadMIDIPath(tempPath);
        File.Delete(tempPath);
    }

    public static Texture2D tex;
    public static RenderTexture tex2;

    public static string WebhookMIDIFile = "";

    void Start()
    {
        // Make screens temporarily visible to jumpstart scripts, they make themselves invisible
        _instance = this;
        Time.fixedDeltaTime = 1f / 60f;
        RenderingSystem.CoroutineStart();
        threadSlider.maxValue = Environment.ProcessorCount;
        //foreach(Transform screen in transform)
        //{
            //screen.gameObject.SetActive(true);
        //}
        firstrun = true;
        versionText.text = $"Sharpfall {InternalConfig.ClientVersion}";
        openFileButton.onClick.AddListener(() =>
        {
            if(!fileDialogOpen)
            {
                fileDialogOpen = true;
                StartCoroutine(ShowLoadDialogCoroutine());
            }
        });
        closeFileButton.onClick.AddListener(() =>
        {
            selectedFilePath = null;
            fileText.text = "";
            playButton.interactable = false;
        });
        ClosePromptButton.onClick.AddListener(() =>
        {
            ExitToMenuPrompt.SetActive(false);
        });
        ExitToMenuButton.onClick.AddListener(() =>
        {
            if (!playingMIDI)
                return;

            SimulateExitPress();
        });
        playButton.onClick.AddListener(() =>
        {
            if (playingMIDI)
                return;
            PlaybackMode playMode = (PlaybackMode)MasterConfig.GetValue("PlaybackMode");
            int width = (int)MasterConfig.GetValue("RenderWidth");
            int height = (int)MasterConfig.GetValue("RenderHeight");
            if (playMode == PlaybackMode.FFmpegRender)
            {
                FFmpegCodec codec = (FFmpegCodec)MasterConfig.GetValue("FFmpegCodec");
                int codecInt = (int)codec;
                string fileName = Path.GetFileNameWithoutExtension(selectedFilePath) + ".mkv";
                if (codecInt < 2)
                {
                    int crf = (int)MasterConfig.GetValue("FFmpegCRF");
                    FFmpegPreset preset = (FFmpegPreset)MasterConfig.GetValue("FFmpegPreset");
                    LibSharpfall.ctx = LibSharpfall.ffmpeg_start(fileName, width, height, 60, codec.ToString(), 1, crf, preset.ToString());
                    if (LibSharpfall.ctx == null)
                        return;
                } else {
                    int bitrate = (int)MasterConfig.GetValue("FFmpegBitrate");
                    LibSharpfall.ctx = LibSharpfall.ffmpeg_start(fileName, width, height, 60, codec.ToString(), 0, bitrate, "");
                    if (LibSharpfall.ctx == null)
                        return;
                }
            }
            LibSharpfall.CM_Dispose();
            if(LibSharpfall.CM_LoadMIDIPath(selectedFilePath) == 0)
            {
                MIDILoadError.SetActive(true);
                return;
            }
            playingMIDI = true;
            settingsFrame.SetActive(false);
            LibSharpfall.CM_ReloadSynth();
            LibSharpfall.PXU_DeleteAllObjects();
            LibSharpfall.OM_WriteConfig("BlockLimit", (int)MasterConfig.GetValue("BlockLimit"));
            LibSharpfall.OM_WriteConfig("LimitX1", (int)MasterConfig.GetValue("DoublesReductionControl"));

            if(playMode != PlaybackMode.Realtime)
            {
                if (tex != null)
                    DestroyImmediate(tex);
                if (tex2 != null)
                    DestroyImmediate(tex2);

                string url = (string)MasterConfig.GetValue("DiscordWebhook");
                if(url.StartsWith("https://discord.com/api/webhooks/"))
                {
                    WebhookMIDIFile = Path.GetFileNameWithoutExtension(selectedFilePath) + ".mid";
                    var payload = new
                    {
                        content = $"Started a render on {WebhookMIDIFile}\n-# Sharpfall {InternalConfig.ClientVersion}"
                    };
                    string json = JsonConvert.SerializeObject(payload);
                    byte[] body = Encoding.UTF8.GetBytes(json);
                    var req = new UnityWebRequest(url, "POST");
                    req.uploadHandler = new UploadHandlerRaw(body);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    var op = req.SendWebRequest();
                    op.completed += _ =>
                    {
                        if (req.result != UnityWebRequest.Result.Success)
                            Debug.LogWarning("Webhook failed: " + req.error);
                        req.Dispose();
                    };
                }

                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex2 = new RenderTexture(width, height, 32);
                tex2.antiAliasing = 8;
                Camera.main.targetTexture = tex2;
                RenderTexture.active = tex2;
                renderImage.texture = tex2;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = (Color32)MasterConfig.GetValue("SkyboxColor");//new Color32(220, 254, 254, 255);
                imageObject.SetActive(true);
                imageBG.SetActive(true);
                imageObject.GetComponent<AspectRatioFitter>().aspectRatio = (float)width / (float)height;
            }
        });
        ExitToMenuPrompt.SetActive(false);
    }

    public void Hide()
    {
        settingsFrame.SetActive(false);
    }

    void LibError(Exception ex)
    {
        DisplayScreenID(1);
        imageObject.SetActive(false);
        imageBG.SetActive(false);
        MIDIEndingCont.SetActive(false);
        Screen_LibError.DisplayError(ex.Message, (ex.InnerException != null ? ex.InnerException.ToString() : "N/A"));
        Camera.main.backgroundColor = new Color(0.1f, 0, 0);
    }

    void LibError(Exception ex, string customMessage)
    {
        DisplayScreenID(1);
        imageObject.SetActive(false);
        imageBG.SetActive(false);
        MIDIEndingCont.SetActive(false);
        Screen_LibError.DisplayError(ex.Message, customMessage);
        Camera.main.backgroundColor = new Color(0.1f, 0, 0);
    }

    void HandleNoteSize(string key, object value)
    {
        if(key == "BlockSizeX" || key == "BlockSizeY" || key == "BlockSizeZ")
            LibSharpfall.PXU_SetNoteSize((float)MasterConfig.GetValue("BlockSizeX"), (float)MasterConfig.GetValue("BlockSizeY"), (float)MasterConfig.GetValue("BlockSizeZ"));
    }

    void HandleGravity(string key, object value)
    {
        if (key == "Gravity")
            LibSharpfall.PXU_SetGravity((float)value);
    }

    void HandleCullHeight(string key, object value)
    {
        if (key == "CullHeight")
            LibSharpfall.PXU_SetCullHeight((float)value);
    }

    void HandleSpawnVelocity(string key, object value)
    {
        if (key == "SpawnVelocityX" || key == "SpawnVelocityY" || key == "SpawnVelocityZ")
            LibSharpfall.OM_WriteSpawnVelocity((float)MasterConfig.GetValue("SpawnVelocityX"), (float)MasterConfig.GetValue("SpawnVelocityY"), (float)MasterConfig.GetValue("SpawnVelocityZ"));
    }

    // Simulator objects
    public static Light MainLight;
    public static Volume GlobalVolume;

    public Material NoteMaterial;
    public Material PlatformMaterial;

    private static UniversalRenderPipelineAsset render;

    void HandleSkybox(string key, object value)
    {
        if (key == "SkyboxColor")
            Camera.main.backgroundColor = (Color32)value;
        if (key == "FloorColor")
            PlatformMaterial.SetColor("_BaseColor", (Color32)value);
    }

    void HandleMiscVisuals(string key, object value)
    {
        switch(key)
        {
            case "LightIntensity":
                MainLight.intensity = (float)value;
                break;
            case "NoteMetallic":
                NoteMaterial.SetFloat("_Metallic", (float)value);
                break;
            case "NoteSmoothness":
                NoteMaterial.SetFloat("_Smoothness", (float)value);
                break;
            case "FloorMetallic":
                PlatformMaterial.SetFloat("_Metallic", (float)value);
                break;
            case "FloorSmoothness":
                PlatformMaterial.SetFloat("_Smoothness", (float)value);
                break;
            case "CameraFOV":
                Camera.main.fieldOfView = (float)value;
                break;
        }
    }

    void HandleMiscGraphics(string key, object value)
    {
        switch (key)
        {
            case "BloomEnabled":
                {
                    var profile = GlobalVolume.profile;
                    if (!profile.TryGet(out Bloom bloom))
                        bloom = profile.Add<Bloom>(true);
                    bloom.active = (bool)value;
                }
                break;
            case "BloomThreshold":
                {
                    var profile = GlobalVolume.profile;
                    if (!profile.TryGet(out Bloom bloom))
                        bloom = profile.Add<Bloom>(true);
                    bloom.threshold.Override((float)value);
                }
                break;
            case "BloomIntensity":
                {
                    var profile = GlobalVolume.profile;
                    if (!profile.TryGet(out Bloom bloom))
                        bloom = profile.Add<Bloom>(true);
                    bloom.intensity.Override((float)value);
                }
                break;
            case "BloomScatter":
                {
                    var profile = GlobalVolume.profile;
                    if (!profile.TryGet(out Bloom bloom))
                        bloom = profile.Add<Bloom>(true);
                    bloom.scatter.Override((float)value);
                }
                break;
            case "TonemappingEnabled":
                {
                    var profile = GlobalVolume.profile;
                    if (!profile.TryGet(out Tonemapping tonemap))
                        tonemap = profile.Add<Tonemapping>(true);
                    tonemap.active = (bool)value;
                }
                break;
            case "ShadowsEnabled":
                MainLight.shadows = (bool)value ? LightShadows.Soft : LightShadows.None;
                break;
        }
    }

    // Connect to libSharpfall on first update, to allow everything to initialize
    void Update()
    {
        if(firstrun)
        {
            firstrun = false;
            try
            {
                IntPtr ptr = LibSharpfall.LS_GetValidation();
                if (ptr == IntPtr.Zero)
                    throw new Exception("Invalid return address by GetValidation");
                string text = LibSharpfall.IntPtrToString(ptr);
                if (text != InternalConfig.Validation)
                    throw new Exception("Incorrect return string by GetValidation");
            }
            catch (Exception ex) { LibError(ex); return; }

            try { LibSharpfall.Init(); } catch (Exception ex) {
                LibError(ex, "Error invoking LibSharpfall.Init()");
                return;
            }

            try { LibSharpfall.PXU_InitPhysics(); } catch (Exception ex) {
                LibError(ex, "Error invoking LibSharpfall.PXU_InitPhysics()");
                return;
            }

            try { if (LibSharpfall.CM_InitSynth() != 1) { throw new Exception("Failed to load KDMAPI, make sure OmniMIDI is installed."); } } catch (Exception ex) {
                LibError(ex, "Error invoking LibSharpfall.CM_InitSynth()");
                return;
            }

            LibSharpfall.SuccessfulInit = true;

            var original = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            render = Instantiate(original);
            GraphicsSettings.defaultRenderPipeline = render;

            MasterConfig.ConfigSubscribe(HandleNoteSize);
            MasterConfig.ConfigSubscribe(HandleGravity);
            MasterConfig.ConfigSubscribe(HandleCullHeight);
            MasterConfig.ConfigSubscribe(HandleSpawnVelocity);
            MasterConfig.ConfigSubscribe(HandleSkybox);
            MasterConfig.ConfigSubscribe(HandleMiscVisuals);
            MasterConfig.ConfigSubscribe(HandleMiscGraphics);

            DisplayScreenID(3);

            SceneManager.LoadScene("Simulator", LoadSceneMode.Additive);

            StartCoroutine(FinalLoadStage());
        }
        if(playingMIDI)
        {
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                ExitToMenuPrompt.SetActive(true);
            }
        }
    }

    IEnumerator FinalLoadStage()
    {
        yield return null;

        MainLight = GameObject.FindWithTag("LightSource")?.GetComponent<Light>();
        GlobalVolume = GameObject.FindWithTag("GlobalVolume")?.GetComponent<Volume>();

        MasterConfig.LoadPreset("Default");

        LoadMenu();
    }
}
