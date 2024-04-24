using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

/** Responsible for managing kart operations. */
public class KartManager : KartBehavior, GameplayManagerBehavior 
{

	private GameplayManager gameplayManager;

	[SerializeField] 
	private POIGDelegate poigDelegate;

	[SyncVar(OnChange = nameof(PlayerDataChanged))] 
	private PlayerData syncData;
	private PlayerData data;
	public PlayerData Data {
		get { return CoreManager.IsMultiplayer ? syncData : data; }
		set {
			if(CoreManager.IsMultiplayer) {
				if(base.IsServer)
					syncData = value; 
				else 
					ServerRpcSetPlayerData(value);
			} else {
				PlayerData prevData = data;
				PlayerDataChanged(prevData, value, false);
				data = value;
			}
		}
	}

	[SyncVar] 
	private bool syncIsHuman;
	private bool isHuman;
	public bool IsHuman {
		get { return CoreManager.IsMultiplayer ? syncIsHuman : isHuman;}
		set {
			if(CoreManager.IsMultiplayer) {
				if(base.IsServer)
					syncIsHuman = value;
				else
					ServerRpcSetIsHuman(value);
			} else
				isHuman = value;
		}
	}
	public bool IsBot { get { return !IsHuman; } }

	public bool ownershipChanged = false;

	new protected void Awake() 
	{
		base.Awake();
		CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
	}

	public void GameplayManagerLoaded(GameplayManager gameplayManager) 
	{
		this.gameplayManager = gameplayManager;
	}

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
		ownershipChanged = true;

		// Sync enabled status with our ownership status
		kartCtrl.enabled = base.IsOwner;
		kartStateManager.enabled = base.IsOwner;
		// kartItemManager: Stays enabled so we can sync item wielding between players
		// posTracker: Stays enabled, updates server on race position (TODO: Make this a server-side calculation it will be exploited)
		// kartEffectManager: Stays enabled

		// Bot/Human driver scripts are determined in UseHumanDriver and UseBotDriver
    }

	/** Connects the PlayerInput to the HumanDriver script in the kart's brain. */
	public void UseHumanDriver(PlayerInput input) 
	{

		if(base.IsOwner) {
			botPath.enabled = false;
			botDriver.enabled = false;
			botItemManager.enabled = false;
			humanDriver.enabled = true;
			humanDriver.ConnectPlayerInput(input);
		}

		IsHuman = true;
		ReadyUp();
	}

	public void UseBotDriver() 
	{
		botPath.enabled = true;
		botDriver.enabled = true;
		botItemManager.enabled = true;
		humanDriver.enabled = false;

		IsHuman = false;
		ReadyUp();
	}

	public void ReadyUp() {
		PlayerData data = Data;
		data.ready = true;
		Data = data;
	}

	[ServerRpc]
	public void ServerRpcSetPlayerData(PlayerData data) { SetPlayerData(data); }
	public void SetPlayerData(PlayerData data) { this.Data = data; }

	[ServerRpc]
	public void ServerRpcSetIsHuman(bool isHuman) { this.IsHuman = isHuman; }

	private void PlayerDataChanged(PlayerData prev, PlayerData current, bool asServer) 
	{
		gameObject.name = KartsIRManager.KartNamePrefix + Data.name;
	}

	public PlayerData GetPlayerData() { return Data; }

	public bool HasPOIGDelegate { get { return poigDelegate != null; } }
	public POIGDelegate POIGDelegate { 
		get { return poigDelegate; } 
		set { poigDelegate = value; } 
	}

	public static bool IsKartGameObject(GameObject obj) 
	{
		return obj.GetComponent<KartManager>() != null;
	}
}