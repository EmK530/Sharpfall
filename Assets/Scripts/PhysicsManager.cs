using TMPro;
using UnityEngine;
using System.IO;
using System.Text;

using UnityEngine.Networking;
using Newtonsoft.Json;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class PhysicsManager : MonoBehaviour
{
    public Mesh cubeMesh;
    public Material cubeMaterial;
    public int batchSize = 1023;

    private Matrix4x4[] matrices = new Matrix4x4[0];
    private Vector4[] colors = new Vector4[0];

    // persistent batch buffers (no per-frame allocs)
    private Matrix4x4[] batchMatrices;
    private Vector4[] batchColors;
    private MaterialPropertyBlock mpb;

    static float fixedAccumulator = 0f;
    const int MAX_FIXED_STEPS = 8;

    static void AccumulatorFixedStep(float fixedDt, float frameDt)
    {
        fixedAccumulator += frameDt;

        int steps = 0;
        while (fixedAccumulator >= fixedDt && steps < MAX_FIXED_STEPS)
        {
            LibSharpfall.PXU_StepPhysics(fixedDt);
            fixedAccumulator -= fixedDt;
            steps++;
        }

        if (steps == MAX_FIXED_STEPS)
        {
            fixedAccumulator = 0f;
        }
    }

    bool doUpdate = false;
    int frameCount = 0;
    bool lastState = false;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        LibSharpfall.Init();
        LibSharpfall.PXU_InitPhysics();
        LibSharpfall.PXU_DeleteAllObjects();

        lastState = LibSharpfall.PXU_GetCUDAStatus();
        MasterConfig.WriteValue("CUDAAccel", lastState);
        MasterConfig.ConfigSubscribe((key, value) =>
        {
            if (key == "CUDAAccel")
            {
                libInfo.needsUpdate = true;
                if (lastState != (bool)value)
                {
                    LibSharpfall.PXU_SetCUDAState((bool)value);
                    lastState = LibSharpfall.PXU_GetCUDAStatus();
                    doUpdate = true;
                }
            } else if(key == "SolverIterations")
            {
                LibSharpfall.PXU_SetSolverIterations((int)value);
            }
        });

        batchMatrices = new Matrix4x4[batchSize];
        batchColors = new Vector4[batchSize];
        mpb = new MaterialPropertyBlock();

#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state) {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
            LibSharpfall.CM_ReloadSynth();
            LibSharpfall.PXU_DeleteAllObjects();
            LibSharpfall.CM_Dispose();
        }
    }
#endif

    private static float ShutdownTimer = 15f;
    private static bool WaitingToExit = false;
    void Update()
    {
        if(doUpdate)
        {
            frameCount++;
            if(frameCount >= 2)
            {
                MasterConfig.WriteValue("CUDAAccel", lastState);
                doUpdate = false;
                frameCount = 0;
            }
        }

        // Step MIDI in fixed slices (still CPU-bound, but independent of physics)
        PlaybackMode playMode = (PlaybackMode)MasterConfig.GetValue("PlaybackMode");

        bool played = true;

        float dt = (playMode != PlaybackMode.Realtime || !UIMain.playingMIDI) ? Time.fixedDeltaTime : Time.unscaledDeltaTime;
        float curDt = dt;
        if (dt < 1)
        {
            while (dt > 0.001f)
            {
                float dec = Mathf.Min(dt, 1f / 120f);
                int play = LibSharpfall.CM_StepPlayer(dec);
                if (played && play == 0)
                    played = false;
                dt -= dec;
            }
        }

        if (playMode != PlaybackMode.Realtime)
        {
            if (played || !UIMain.playingMIDI)
            {
                ShutdownTimer = (float)(int)MasterConfig.GetValue("AutoCloseSeconds");
                UIMain._instance.MIDIEndingCont.SetActive(false);
            } else
            {
                ShutdownTimer -= curDt;
                UIMain._instance.MIDIEndingCont.SetActive(true);
                UIMain._instance.MIDIEndingText.text = "Program will exit in " + ShutdownTimer.ToString("F2") + "s";
                if (ShutdownTimer <= 0f)
                {
                    if (playMode == PlaybackMode.FFmpegRender)
                    {
                        if (LibSharpfall.ctx != null)
                            LibSharpfall.ffmpeg_close_stdin(LibSharpfall.ctx);
                        LibSharpfall.ctx = null;
                    }
                    if (!WaitingToExit)
                    {
                        WaitingToExit = true;
                        string url = (string)MasterConfig.GetValue("DiscordWebhook");
                        if (url.StartsWith("https://discord.com/api/webhooks/"))
                        {
                            var payload = new
                            {
                                content = $"Render job finished on: {UIMain.WebhookMIDIFile}\n-# Sharpfall {InternalConfig.ClientVersion}"
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
                                Application.Quit();
                            };
                        } else
                        {
                            Application.Quit();
                        }
                    }
                }
            }
        }

        // Update FPS
        // FPSLabel.text = "FPS: " + (1f / Time.unscaledDeltaTime).ToString("F1");
    }

    void LateUpdate()
    {
        int objectCount = LibSharpfall.PXU_GetObjectCount();

        if (matrices.Length < objectCount) matrices = new Matrix4x4[objectCount];
        if (colors.Length < objectCount) colors = new Vector4[objectCount];

        // Fetch data from native DLL
        LibSharpfall.PXU_GetAllObjectMatrices(matrices, objectCount);
        LibSharpfall.OM_GetAllColorData(colors, objectCount);

        // Draw in batches — WITHOUT allocations or Array.Copy
        for (int i = 0; i < objectCount; i += batchSize)
        {
            int c = Mathf.Min(batchSize, objectCount - i);

            System.Array.Copy(matrices, i, batchMatrices, 0, c);
            System.Array.Copy(colors, i, batchColors, 0, c);

            mpb.SetVectorArray("_BaseColor", batchColors);

            Graphics.DrawMeshInstanced(
                cubeMesh,
                0,
                cubeMaterial,
                batchMatrices,
                c,
                mpb
            );
        }

        // Start physics step
        LibSharpfall.PXU_SignalNewFrame();

        PlaybackMode playMode = (PlaybackMode)MasterConfig.GetValue("PlaybackMode");
        if (playMode == PlaybackMode.Realtime || !UIMain.playingMIDI)
        {
            int mode = (int)MasterConfig.GetValue("SteppingMethod");
            switch (mode)
            {
                // Adaptive Fixed Rate
                case 0:
                    {
                        float deltaTime = Time.unscaledDeltaTime;
                        float subStepTarget = Time.fixedDeltaTime;
                        int subSteps = (int)(Mathf.Max(deltaTime / subStepTarget, 1f));
                        for (int i = 0; i < subSteps; i++)
                        {
                            LibSharpfall.PXU_StepPhysics(deltaTime / (float)subSteps);
                        }
                        break;
                    }

                // 60Hz Accumulator Fixed
                case 1:
                    {
                        AccumulatorFixedStep(Time.fixedDeltaTime, Time.unscaledDeltaTime);
                        break;
                    }

                // Per-Frame Clamped
                case 2:
                    {
                        LibSharpfall.PXU_StepPhysics(Time.fixedDeltaTime);
                        break;
                    }

                // Per-Frame Clamped
                case 3:
                    {
                        LibSharpfall.PXU_StepPhysics(Mathf.Clamp(Time.unscaledDeltaTime, 0f, 1f / 30f));
                        break;
                    }
            }
        } else
        {
            LibSharpfall.PXU_StepPhysics(Time.fixedDeltaTime);
        }
    }
}