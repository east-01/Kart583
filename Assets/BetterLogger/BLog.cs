using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BLog : MonoBehaviour
{

    [SerializeField]
    private List<LogChannelData> channelDatas;
    [SerializeField]
    private int verbosity = 0;

    private Dictionary<LogChannel, string> colors = new();

    public static void Log(string message, LogChannel channel = LogChannel.Default, int verbosity = 0) 
    {            
        BLog inst = CoreManager.BLog;
        if(inst.colors.Count == 0)
            inst.ParseChannelData();

        if(verbosity > inst.verbosity)
            return;
        string color = "#c9c9c9";
        if(inst.colors.ContainsKey(channel))
            color = inst.colors[channel];

        Debug.Log($"<color=#{color}>{message}</color>");
    }

    public void ParseChannelData() 
    {
        foreach(LogChannelData lcd in channelDatas) {
            colors[lcd.channel] = lcd.color.ToHexString().Substring(0, 6);
        }
    }

}

[Serializable]
public struct LogChannelData {
    public LogChannel channel;
    public Color color;
}

[Serializable]
public enum LogChannel {
    Default, SceneDelegate, GameLobby, GameplayManager, LobbyManager, DevSettings
}