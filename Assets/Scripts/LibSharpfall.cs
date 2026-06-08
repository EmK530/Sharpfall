using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;

public static class LibSharpfall
{
    private const string DLL_NAME = "libSharpfall.dll";
    private const string UPDATE_API = "https://api.github.com/repos/EmK530/libSharpfall/releases/latest";

    public static FFmpegContext ctx = null;

    public static void Init()
    {
        /*
        string dllPath = "";
        if(UnityEngine.Application.isEditor)
        {
            dllPath = "Assets/Plugins/x86_64/" + DLL_NAME;
        } else
        {
            dllPath = UnityEngine.Application.dataPath+"/Plugins/x86_64/"+DLL_NAME;
        }

        if (!File.Exists(dllPath))
        {
            UnityEngine.Debug.Log("[LibSharpfall.Bindings] DLL not found! Cannot check for updates.");
            return;
        }

        var info = FileVersionInfo.GetVersionInfo(dllPath);
        string fileVersion = info.FileVersion;
        */
        // TODO: Check for updates.
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectData
    {
        public float x, y, z;
        public float qx, qy, qz, qw;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class FFmpegContext
    {
        public IntPtr pipe;
        public int width;
        public int height;
    }

    public static bool SuccessfulInit = false;

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_InitPhysics();

    //[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    //public static extern int CreateObject(float x, float y, float z, float vx, float vy, float vz);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_DeleteAllObjects();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_BeginStep(float deltaTime);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool PXU_IsStepDone();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_CompleteStep();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SignalNewFrame();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_StepPhysics(float deltaTime);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SetCullHeight(float h);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int PXU_GetObjectCount();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_GetAllObjectMatrices([Out] UnityEngine.Matrix4x4[] buffer, int bufferSize);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SetNoteSize(float x, float y, float z);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SetGravity(float y);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OM_GetAllColorData([Out] UnityEngine.Vector4[] buffer, int bufferSize);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OM_WriteSpawnVelocity(float x, float y, float z);

    //[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    //public static extern void SetObjectTransform(int index, float x, float y, float z, float qx, float qy, float qz, float qw, float vx, float vy, float vz);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_ShutdownPhysics();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SetCUDAState(bool state);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void PXU_SetSolverIterations(int value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LS_GetTarget_libSharpfall();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LS_GetVer_libSharpfall();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LS_GetVer_ConMIDI();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LS_GetValidation();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool PXU_GetCUDAStatus();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PXU_GetCUDADevice();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PXU_GetCUDAError();



    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CM_InitSynth();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CM_ReloadSynth();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CM_LoadMIDIPath([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CM_StepPlayer(double deltaTime);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CM_Dispose();



    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void OM_WriteConfig(string target, int value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern FFmpegContext ffmpeg_start(
        string outputFile,
        int width,
        int height,
        int fps,
        string codec,
        int useCRF,
        int quality,
        string preset
    );

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool ffmpeg_write_frame(FFmpegContext ctx, byte[] data, int size);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ffmpeg_close_stdin(FFmpegContext ctx);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ffmpeg_destroy(FFmpegContext ctx);

    public static string IntPtrToString(IntPtr ptr)
    {
        return Marshal.PtrToStringUTF8(ptr);
    }
}