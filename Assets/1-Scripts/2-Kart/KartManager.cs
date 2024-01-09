using System;
using UnityEngine;

/** This class will be responsible for one player.
    Manages items currently */
public class KartManager : KartBehavior 
{

	private PlayerData data;

	public void SetPlayerData(PlayerData data) 
	{
		this.data = data;
	}

	public void SwitchToBotBrain() 
	{
		botPath.enabled = true;
		botDriver.enabled = true;
		botItemManager.enabled = true;
		humanDriver.enabled = false;
	}

	public static bool IsKartGameObject(GameObject obj) 
	{
		return obj.GetComponent<KartManager>() != null;
	}

	public PlayerData GetPlayerData() { return data; }

}

public struct PlayerData {
	public String name;
}