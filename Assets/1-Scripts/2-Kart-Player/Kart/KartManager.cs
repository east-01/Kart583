using System;
using UnityEngine;
using UnityEngine.InputSystem;

/** This class will be responsible for one player.
    Manages items currently */
public class KartManager : KartBehavior 
{

	private PlayerData data;
	private bool isHuman;

	public void SetPlayerData(PlayerData data) 
	{
		this.data = data;
	}

	/** Connects the PlayerInput to the HumanDriver script in the kart's brain. */
	public void UseHumanDriver(PlayerInput input) 
	{
		isHuman = true;

		botPath.enabled = false;
		botDriver.enabled = false;
		botItemManager.enabled = false;
		humanDriver.enabled = true;
		humanDriver.ConnectPlayerInput(input);
	}

	public void UseBotDriver() 
	{
		isHuman = false;

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
	public bool IsHuman { get { return isHuman; } }
	public bool IsBot { get { return !isHuman; } }

}

public struct PlayerData {
	public String name;
	public String hexColor;
	public bool ready;
}