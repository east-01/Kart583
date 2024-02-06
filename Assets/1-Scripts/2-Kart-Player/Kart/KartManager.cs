using System;
using UnityEngine;
using UnityEngine.InputSystem;

/** Responsible for managing kart operations. */
public class KartManager : KartBehavior 
{

	[SerializeField] private POIGDelegate poigDelegate;

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

	public bool HasPOIGDelegate { get { return poigDelegate != null; } }
	public POIGDelegate POIGDelegate { get { return poigDelegate; } }

}

public struct PlayerData {
	public string name;
	public string hexColor;
	public KartName? kartName;
	public bool ready;
}