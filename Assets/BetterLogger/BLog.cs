using System;
using System.Collections.Generic;
using UnityEngine;

public class BLog : MonoBehaviour
{

    public static int MAX_VERBOSITY = 5;

    public static BLog Instance;

    // The data & verbosity will be loaded by the DevSettings window
    private Dictionary<LogChannel, LogChannelData> data = new();
    private int verbosity = 0;

    private void Awake() 
    {
        if(Instance != null) {
            Debug.LogError("Can't wake up a new BetterLogger instnace. One already exists.");
            return;
        }

        Instance = this;
    }

    public static void Log(string message, LogChannel channel = LogChannel.Default, int verbosity = 0) 
    {            
        if(Instance == null) {
            Debug.LogError("Can't BLog, no instance exists");
            return;
        }
        if(verbosity < 0 || verbosity > MAX_VERBOSITY) {
            Debug.LogError($"Can't BLog. Provided verbosity is out of range. Provided {verbosity}, range [0, {MAX_VERBOSITY}]");
        }
        // If this messages verbosity is greater than the limit, don't print
        if(verbosity > Instance.verbosity)
            return;
        string color = "#c9c9c9";
        if(Instance.data.ContainsKey(channel))
            color = Instance.data[channel].color.ToString();

        Debug.Log($"<color=#{color}>{message}</color>");
    }

    public static void Highlight(string message) 
    {
        Debug.Log($"<color=#FFD700><b>{message}</b></color>");        
    }

    public int GetVerbosity() { return verbosity;}
    public void SetVerbosity(int verbosity) { this.verbosity = verbosity; }

    public bool HasData(LogChannel channel) { return data.ContainsKey(channel); } 
    public LogChannelData GetData(LogChannel channel) { return data[channel]; }
    public void SetData(LogChannel channel, LogChannelData newData) { data[channel] = newData; }

}

[Serializable]
public struct LogChannelData {
    public LogChannel channel;
    public bool enable;
    public Color color;

    public static LogChannelData DefaultData { get {
        return new() {
            channel = LogChannel.Default,
            enable = true,
            color = Color.white
        };
    } }
}

[Serializable]
public enum LogChannel {
    Default, SceneDelegate, GameLobby, GameplayManager, LobbyManager, DevSettings, KartManager
}