using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class SettingPreset
{
    public SettingPreset(string nm)
    {
        name = nm;
        settings = new Dictionary<string, object>();
    }

    public SettingPreset(string nm, Dictionary<string, object> values)
    {
        name = nm;
        settings = new Dictionary<string, object>(values); // FIX: clone
        canBeSaved = false;
    }

    public string name;
    public Dictionary<string, object> settings = new Dictionary<string, object>();
    public bool canBeSaved = true;
}

public enum NoteColorMethod
{
    BwColor = 0,
    ChannelRGB = 1,
    RainbowColor = 2,
    RandomHue = 3,
    TrackChannelRandomHue = 4,
    TrackPFARandom = 5,
    TrackPFALooped = 6
}

public enum PlaybackMode
{
    Realtime = 0,
    PNGRender = 1,
    FFmpegRender = 2
}

public enum FFmpegCodec
{
    libx264 = 0,
    libx265 = 1,
    h264_nvenc = 2,
    hevc_nvenc = 3,
    av1_nvenc = 4
}

public enum FFmpegPreset
{
    ultrafast = 0,
    superfast = 1,
    veryfast = 2,
    faster = 3,
    fast = 4,
    medium = 5,
    slow = 6,
    slower = 7,
    veryslow = 8,
    placebo = 9
}

[Serializable]
public class ConfigValue
{
    public string type;
    public string value;
}

[Serializable]
public class SaveData
{
    public Dictionary<string, ConfigValue> values = new();
}

public static class MasterConfig
{
    private static List<Action<string, object>> configCallbacks = new();

    private static List<string> configIgnoreKeys = new()
    {
        "CUDAAccel"
    };

    private static string ConfigFolder = "Presets";

    private static Dictionary<string, object> defaults = new()
    {
        ["PlaybackMode"] = PlaybackMode.Realtime,

        ["FFmpegCodec"] = FFmpegCodec.libx264,
        ["FFmpegCRF"] = 21,
        ["FFmpegPreset"] = FFmpegPreset.veryfast,
        ["FFmpegBitrate"] = 25000,

        ["AutoCloseSeconds"] = 15,
        ["CUDAAccel"] = false,
        ["SolverIterations"] = 16,
        ["BlockSizeX"] = 0.1f,
        ["BlockSizeY"] = 1f,
        ["BlockSizeZ"] = 1f,
        ["Gravity"] = -9.81f,
        ["NoteColoring"] = 1,
        ["CullHeight"] = -500f,

        ["RenderWidth"] = 1920,
        ["RenderHeight"] = 1080,
        ["BlockLimit"] = 2500,
        ["DoublesReductionControl"] = 1,
        ["SteppingMethod"] = 0,
        ["NoteColorMethod"] = NoteColorMethod.TrackPFARandom,

        ["SpawnVelocityX"] = 0f,
        ["SpawnVelocityY"] = -15f,
        ["SpawnVelocityZ"] = 0f,

        ["SkyboxColor"] = new Color32(220, 254, 254, 255),

        ["LightIntensity"] = 1f,
        ["NoteMetallic"] = 0f,
        ["NoteSmoothness"] = 0f,

        ["FloorColor"] = new Color32(69, 69, 69, 255),
        ["FloorMetallic"] = 0f,
        ["FloorSmoothness"] = 0.5f,

        ["CameraFOV"] = 60f,
        ["BloomEnabled"] = false,
        ["BloomThreshold"] = 0.5f,
        ["BloomIntensity"] = 0.75f,
        ["BloomScatter"] = 0.52f,
        ["TonemappingEnabled"] = false,
        ["ShadowsEnabled"] = true,

        ["DiscordWebhook"] = ""
    };

    public static List<SettingPreset> Presets = new()
    {
        new SettingPreset("Default", defaults)
    };

    public static SettingPreset currentPreset;

    // =========================
    // Serialization
    // =========================

    private static object ConvertValue(object obj)
    {
        if (obj is JValue jv)
            return jv.Value;

        if (obj is JObject jo)
        {
            // Color32 support
            if (jo.ContainsKey("r") && jo.ContainsKey("g") && jo.ContainsKey("b"))
            {
                byte r = jo["r"].Value<byte>();
                byte g = jo["g"].Value<byte>();
                byte b1 = jo["b"].Value<byte>();

                byte a = jo.ContainsKey("a") ? jo["a"].Value<byte>() : (byte)255;

                return new Color32(r, g, b1, a);
            }

            return jo.ToObject<Dictionary<string, object>>();
        }

        if (obj is JArray ja)
            return ja.ToObject<List<object>>();

        if (obj is long l) return (int)l;
        if (obj is double d) return (float)d;
        if (obj is bool b) return b;
        if (obj is string s) return s;

        return obj;
    }

    // =========================
    // Core API
    // =========================

    public static void ConfigSubscribe(Action<string, object> callback)
    {
        configCallbacks.Add(callback);
    }

    private static void SendToAllCallbacks(string key, object value)
    {
        foreach (var cb in configCallbacks.ToArray())
            cb(key, value);
    }

    public static void WriteValue(string key, object value)
    {
        if (currentPreset == null)
            currentPreset = Presets[0];

        currentPreset.settings[key] = value;
        SendToAllCallbacks(key, value);
    }

    public static object GetValue(string key)
    {
        if (currentPreset == null)
            LoadPreset(Presets[0].name);

        return currentPreset.settings[key];
    }

    // =========================
    // File Handling
    // =========================

    public static void SaveAs(string newName, bool switchToNew = true)
    {
        if (currentPreset == null || string.IsNullOrWhiteSpace(newName))
            return;

        var newSettings = new Dictionary<string, object>(currentPreset.settings);

        var newPreset = new SettingPreset(newName, newSettings)
        {
            canBeSaved = true
        };

        Presets.RemoveAll(p => p.name == newName);
        Presets.Add(newPreset);

        if (switchToNew)
            currentPreset = newPreset;

        Directory.CreateDirectory(ConfigFolder);

        var filtered = new Dictionary<string, object>();

        foreach (var kv in newPreset.settings)
        {
            if (configIgnoreKeys.Contains(kv.Key))
                continue;

            filtered[kv.Key] = kv.Value;
        }

        string json = JsonConvert.SerializeObject(filtered, Formatting.Indented);
        string path = Path.Combine(ConfigFolder, newName + ".json");

        File.WriteAllText(path, json);
    }

    public static bool DeleteCurrentPreset()
    {
        if (currentPreset == null)
            return false;

        // Prevent deleting protected presets
        if (!currentPreset.canBeSaved)
        {
            Debug.LogWarning("Cannot delete a built-in or protected preset.");
            return false;
        }

        string name = currentPreset.name;
        string path = Path.Combine(ConfigFolder, name + ".json");

        // 1. Delete file if it exists
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        // 2. Remove from runtime list
        Presets.RemoveAll(p => p.name == name);

        // 3. Switch to fallback preset
        var fallback = Presets.FirstOrDefault(p => !p.canBeSaved)
                       ?? Presets.FirstOrDefault();

        if (fallback == null)
        {
            Debug.LogError("No fallback preset available!");
            currentPreset = null;
            return false;
        }

        currentPreset = fallback;

        // 4. Fire callbacks to update everything
        foreach (var kv in currentPreset.settings.ToList())
            SendToAllCallbacks(kv.Key, kv.Value);

        return true;
    }

    public static void LoadPreset(string name)
    {
        currentPreset = Presets.FirstOrDefault(p => p.name == name)
                        ?? new SettingPreset(name, defaults);

        string path = Path.Combine(ConfigFolder, name + ".json");

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);

            var loaded = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (loaded != null)
            {
                foreach (var kv in loaded)
                {
                    if (configIgnoreKeys.Contains(kv.Key))
                        continue;

                    currentPreset.settings[kv.Key] = ConvertValue(kv.Value);
                }
            }
        }

        foreach (var kv in currentPreset.settings.ToList())
        {
            //Debug.Log("Sending callback: " + kv.Key);
            SendToAllCallbacks(kv.Key, kv.Value);
        }
    }

    public static List<string> GetAvailablePresetFiles()
    {
        if (!Directory.Exists(ConfigFolder))
            return new List<string>();

        return Directory.GetFiles(ConfigFolder, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();
    }

    public static List<string> GetAvailablePresets()
    {
        var result = new HashSet<string>();

        foreach (var preset in Presets)
            result.Add(preset.name);

        if (Directory.Exists(ConfigFolder))
        {
            foreach (var file in Directory.GetFiles(ConfigFolder, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                result.Add(name);
            }
        }

        return result.ToList();
    }
}