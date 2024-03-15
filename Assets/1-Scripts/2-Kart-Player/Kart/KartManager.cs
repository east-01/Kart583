using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

/** Responsible for managing kart operations. */
public class KartManager : KartBehavior 
{

	[SerializeField] private POIGDelegate poigDelegate;

	[SerializeField] 
	private PlayerData data;
	[SyncVar] private bool isHuman;

	[ObserversRpc(RunLocally = true, BufferLast = true)]
	public void SetPlayerData(PlayerData data) 
	{
		this.data = data;
		gameObject.name = KartSpawner.KartNamePrefix + data.name;
	}

	/** Connects the PlayerInput to the HumanDriver script in the kart's brain. */
	public void UseHumanDriver(PlayerInput input) 
	{
		botPath.enabled = false;
		botDriver.enabled = false;
		botItemManager.enabled = false;
		humanDriver.enabled = true;
		humanDriver.ConnectPlayerInput(input);

		if(base.IsClient) {
			ServerRpcSetIsHuman(true);
			ServerRpcSetReady(true);
		} else
			throw new InvalidOperationException("Tried to ready human driver without being a client.");
	}

	public void UseBotDriver() 
	{
		botPath.enabled = true;
		botDriver.enabled = true;
		botItemManager.enabled = true;
		humanDriver.enabled = false;

		if(base.IsClient) { // Used when the player finishes race and switches to bot controller
			ServerRpcSetIsHuman(false);
			ServerRpcSetReady(true);
		} else if(base.IsServer) {
			isHuman = false;
			data.ready = true;
		}
	}

	public static bool IsKartGameObject(GameObject obj) 
	{
		return obj.GetComponent<KartManager>() != null;
	}

	[ServerRpc]
	public void ServerRpcSetReady(bool readyStatus) { data.ready = readyStatus; }
	[ServerRpc]
	public void ServerRpcSetIsHuman(bool isHuman) { this.isHuman = isHuman; }

	public PlayerData GetPlayerData() { return data; }
	public bool IsHuman { get { return isHuman; } }
	public bool IsBot { get { return !isHuman; } }

	public bool HasPOIGDelegate { get { return poigDelegate != null; } }
	public POIGDelegate POIGDelegate { get { return poigDelegate; } }

}