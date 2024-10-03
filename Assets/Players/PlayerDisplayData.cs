using System;
using EMullen.PlayerMgmt;

/// <summary>
/// This IdentifierData is used by the PlayerManagement to keep track of the PlayerData.
/// The uid WILL NOT CHANGE once instantiated, this ensures you can safely reference the
///   same PlayerData each time.
/// </summary>
public class PlayerDisplayData : PlayerDataClass
{
    /// <summary>
    /// The display name of the player
    /// </summary>
	public string name;
    /// <summary>
    /// The hex color that the player picked in the player select menu.
    /// </summary>
	public string hexColor;

    public PlayerDisplayData() {}

    public override string ToString() => $"name: {name} hexColor: {hexColor}";
}

public static class IdentifierPlayerDataExtensions 
{
    public static string GetUID(this PlayerData playerData) 
    {
        return playerData.GetData<IdentifierData>().uid;
    }
}