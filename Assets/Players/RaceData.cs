using EMullen.PlayerMgmt;
using UnityEngine;

public class RaceData : PlayerDataClass
{
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
    public int points;

    public float? raceFinishTime;

    public RaceData() {}

    public string Summary => $"[RaceData type: {kartType} ready: {ready}]";
}

public static class RaceDataExtensions 
{
    public static bool IsReady(this PlayerData pd) 
    {
        if(!pd.HasData<RaceData>()) {
            Debug.LogWarning("Player does not have RaceData but we're trying to check if they're ready.");
            return false;
        }
        return pd.GetData<RaceData>().ready;
    }
}