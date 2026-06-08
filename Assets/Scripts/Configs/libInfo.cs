using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class libInfo : MonoBehaviour
{
    public static bool needsUpdate = true;

    void UpdateInfo()
    {
        needsUpdate = false;
        TextMeshProUGUI txt = GetComponent<TextMeshProUGUI>();
        if (txt == null)
            return;

        string cudaDevice = LibSharpfall.IntPtrToString(LibSharpfall.PXU_GetCUDADevice());
        string cudaError = LibSharpfall.IntPtrToString(LibSharpfall.PXU_GetCUDAError());

        //if (cudaDevice == "N/A" && cudaError == "N/A")
            //return;

        string IncompatibleWarning = " (May not be compatible with this Sharpfall version)";
        string LibSharpfallTarget = LibSharpfall.IntPtrToString(LibSharpfall.LS_GetTarget_libSharpfall());
        bool compatible = Regex.Match("^" + LibSharpfallTarget, InternalConfig.FeatureLevel).Success;
        bool cuda = LibSharpfall.PXU_GetCUDAStatus();

        string redBegin = "<color=#ff4444>";
        string greenBegin = "<color=#88ff88>";
        string colorEnd = "</color>";

        txt.text = $"Client information:\n\n"
            + $"Version: {InternalConfig.ClientVersion} ({InternalConfig.FeatureLevel})\n"
            + $"libSharpfall Version: {(compatible?"":redBegin)}{LibSharpfall.IntPtrToString(LibSharpfall.LS_GetVer_libSharpfall())}{(compatible?"":colorEnd+IncompatibleWarning)}\n"
            + $"ConMIDI Version: {LibSharpfall.IntPtrToString(LibSharpfall.LS_GetVer_ConMIDI())}\n\n"
            + $"Powered by PhysX (107.3-physx-5.6.1)\n\n"
            + $"CUDA Status: {(cuda?greenBegin:redBegin)}{(cuda?"Enabled":"Disabled")}{colorEnd} ({(cuda?cudaDevice:cudaError)})";
    }

    void Awake()
    {
        if (needsUpdate)
            UpdateInfo();
    }

    void Update()
    {
        if (needsUpdate)
            UpdateInfo();
    }
}
