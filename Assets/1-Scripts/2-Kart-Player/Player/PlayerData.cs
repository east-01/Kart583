using System;
using System.Collections;
using System.Collections.Generic;
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

    public readonly string Summary { get { return $"[PlayerData{{{uuid[..3]}}} name: \"{name}\" type: {kartType} ready: {ready}]"; } }
}
