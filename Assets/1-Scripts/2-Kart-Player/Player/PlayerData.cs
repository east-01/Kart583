using System;
using FishNet.Connection;
using UnityEngine;

/// <summary>
/// This is data relating to the in game player.
/// </summary>
[Serializable]
public struct PlayerData {
    /// <summary>
    /// A unique identifier for this player
    /// </summary>
    public string uuid;
    public NetworkConnection connection;
    /// <summary>
    /// The display name of the player
    /// </summary>
	public string name;
	public KartType kartType;
    /// <summary>
    /// The ready status of the player. Used in two contexts:
    /// 1. Player select menu: Will be set to true when the player has 
    ///   finished customizing their data in the menu 
    /// 2. In game: Set to false initially by the server on player spawn,
    ///   once the player gets connected to it's input and camera it will
    ///   be set to true. Bots will automatically be set to true.
    /// </summary>
	public bool ready;
    /// <summary>
    /// The hex color that the player picked in the player select menu.
    /// </summary>
	public string hexColor;
    public int points;

    public float? raceFinishTime;

    public readonly string Summary { get { return $"[PlayerData{{{uuid[..3]}}} name: \"{name}\" type: {kartType} ready: {ready}]"; } }

    public static string PLAYER_1_DATA = "PLAYER_1_DATA";

    public readonly void SaveToPlayerPrefs(string key) 
    {
        Debug.Log(this.Summary);
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static PlayerData? LoadFromPlayerPrefs(string key) 
    {
        if(!PlayerPrefs.HasKey(key))
            return null;

        string json = PlayerPrefs.GetString(key);
        return JsonUtility.FromJson<PlayerData>(json);
    }
}
