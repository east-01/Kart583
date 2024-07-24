using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;

public class BLog : MonoBehaviour
{

    public static readonly string FILE_PATH = Application.streamingAssetsPath + "/BetterLoggerSettings.json";
    public static int MAX_VERBOSITY = 5;

    private static BetterLoggerSettings settings;
    public static BetterLoggerSettings Settings { 
        get {
            if(settings == null)
                LoadSettings();
            if(settings == null)
                Debug.LogError("Failed to load BetterLoggerSettings");
            return settings;
        }
        set { settings = value; } 
    }

    public static void Log(string message, LogChannel channel = LogChannel.Default, int verbosity = 0) 
    {            
        if(verbosity < 0 || verbosity > MAX_VERBOSITY) {
            Debug.LogError($"Can't BLog. Provided verbosity is out of range. Provided {verbosity}, range [0, {MAX_VERBOSITY}]");
        }
        // If this messages verbosity is greater than the limit, don't print
        if(verbosity > Settings.verbosity)
            return;
        string color = "#c9c9c9";
        if(Settings.channelDatas.ContainsKey(channel)) {
            LogChannelData channelData = Settings.channelDatas[channel];
            if(!channelData.enable)
                return;
            color = channelData.color.ToHexString();
        }

        Debug.Log($"<color=#{color}>{message}</color>");
    }

    public static void Highlight(string message) 
    {
        Debug.Log($"<color=#FFD700><b>{message}</b></color>");        
    }

    public static void SaveSettings() 
    {
        string json = JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings{PreserveReferencesHandling = PreserveReferencesHandling.Objects});
        File.WriteAllText(FILE_PATH, json);
    }

    public static void LoadSettings() 
    {
        if(File.Exists(FILE_PATH))
            settings = JsonConvert.DeserializeObject<BetterLoggerSettings>(File.ReadAllText(FILE_PATH));
        else
            settings = new() {
                verbosity = 0,
                channelDatas = new()
            };
    }
}

[Serializable]
public enum LogChannel 
{
    Default, SceneDelegate, GameLobby, GameplayManager, LobbyManager, DevSettings, KartManager, MenuController, NetworkManager, LobbyCommunicator
}

[Serializable]
public struct LogChannelData 
{
    public bool enable;
    [JsonConverter(typeof(ColorHandler))] public Color color;

    public static LogChannelData DefaultData { get {
        return new() {
            enable = true,
            color = Color.white
        };
    } }
}

[Serializable]
public class BetterLoggerSettings 
{
    public int verbosity;
    public Dictionary<LogChannel, LogChannelData> channelDatas;
}

// Swiped from https://medium.com/@altaf.navalur/serialize-deserialize-color-objects-in-unity-1731e580af94
public class ColorHandler : JsonConverter
{
    public ColorHandler() {}

    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        try
        {
            UnityEngine.ColorUtility.TryParseHtmlString("#" + reader.Value, out Color loadedColor);
            return loadedColor;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse color {objectType} : {ex.Message}");
            return null;
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        string val = UnityEngine.ColorUtility.ToHtmlStringRGB((Color)value);
        writer.WriteValue(val);
    }
}