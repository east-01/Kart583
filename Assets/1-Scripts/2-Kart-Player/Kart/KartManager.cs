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

	[SerializeField, SyncVar(OnChange = nameof(PlayerDataChanged))] 
	private PlayerData data;
	[SyncVar] 
	private bool isHuman;

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
		kartCtrl.enabled = base.IsOwner || base.IsServer;
		// kartItemManager: Stays enabled so we can sync item wielding between players
		// posTracker: Stays enabled, updates server on race position (TODO: Make this a server-side calculation it will be exploited)
		// kartEffectManager: Stays enabled

		// Bot/Human driver scripts are determined in UseHumanDriver and UseBotDriver

		GetComponent<Rigidbody>().isKinematic = !(base.IsOwner || base.IsServer);
    }

	/** Connects the PlayerInput to the HumanDriver script in the kart's brain. */
	public void UseHumanDriver(PlayerInput input) 
	{
		if(base.IsOwner || CoreManager.IsLocal) {
			botPath.enabled = false;
			botDriver.enabled = false;
			botItemManager.enabled = false;
			humanDriver.enabled = true;
			humanDriver.ConnectPlayerInput(input);
		} else {
			Debug.LogError($"Failed to use human driver on kart. Not owner, owner is: \"{base.Owner}\".");
			return;
		}

		if(base.IsClient && !base.IsHost) {
			ServerRpcSetIsHuman(true);
			ServerRpcSetReady(true);
		} else if(base.IsServer) {
			isHuman = true;
			data.ready = true;
		} else
			throw new InvalidOperationException("Tried to ready human driver without being a client.");
	}

	public void UseBotDriver() 
	{
		botPath.enabled = true;
		botDriver.enabled = true;
		botItemManager.enabled = true;
		humanDriver.enabled = false;

		if(!base.IsServer) { // Used when the player finishes race and switches to bot controller
			ServerRpcSetIsHuman(false);
			ServerRpcSetReady(true);
		} else {
			isHuman = false;
			data.ready = true;
		}
	}

	public void SetPlayerData(PlayerData data) 
	{
		this.data = data;
	}

	private void PlayerDataChanged(PlayerData prev, PlayerData current, bool asServer) 
	{
		gameObject.name = KartsIRManager.KartNamePrefix + data.name;
	}

	[ServerRpc]
	public void ServerRpcSetReady(bool readyStatus) { data.ready = readyStatus; }
	[ServerRpc]
	public void ServerRpcSetIsHuman(bool isHuman) { this.isHuman = isHuman; }

	public PlayerData GetPlayerData() { return data; }
	public bool IsHuman { get { return isHuman; } }
	public bool IsBot { get { return !isHuman; } }

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