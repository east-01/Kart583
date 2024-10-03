using System;
using EMullen.PlayerMgmt;
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
	public POIGDelegate POIGDelegate { 
		get => poigDelegate;
		set => poigDelegate = value; 
	}
	public bool HasPOIGDelegate { get { return poigDelegate != null; } }

	private SyncVar<string> ownerUID = new();
	public string OwnerUID => ownerUID.Value;
	[ServerRpc(RequireOwnership = false)]
	public void ServerRpcSetOwnerUID(string ownerUID) => PlayerData = value;
	[ServerRpc]
	public void ServerRpcSetReady(bool readyStatus) => data.ready = readyStatus;

	[SyncVar] 
	private bool isHuman;
	public bool IsHuman => isHuman;
	public bool IsBot => !isHuman;
	[ServerRpc]
	public void ServerRpcSetIsHuman(bool isHuman) => this.isHuman = isHuman;	

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
		// Sync enabled status with our ownership status
		kartCtrl.enabled = base.IsOwner || base.IsServer;
		// kartItemManager: Stays enabled so we can sync item wielding between players
		// posTracker: Stays enabled, updates server on race position (TODO: Make this a server-side calculation it will be exploited)
		// kartEffectManager: Stays enabled

		// Bot/Human driver scripts are determined in UseHumanDriver and UseBotDriver

		GetComponent<Rigidbody>().isKinematic = !(base.IsOwner || base.IsServer);
    }

	/// <summary>
	/// Connect the PlayerInput to the HumanDriver script in the kart's brain, the PlayerInput
	///   field will be connected to the HumanDriver script for player control.
	/// </summary>
	public void UseHumanDriver(string ownerUID, PlayerInput input) 
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

	/// <summary>
	/// Use the BotDriver script in the kart's brain, it will automatically race normally.
	/// </summary>
	public void UseBotDriver(string ownerUID) 
	{
		this.ownerUID.Value = ownerUID;

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

	private void PlayerDataChanged(PlayerData prev, PlayerData current, bool asServer) 
	{
		gameObject.name = KartsIRManager.KartNamePrefix + data.name;
	}

	/// <summary>
	/// Check if a GameObject is a Kart GameObject, that being the object has a KartManager
	///   component on it.
	/// </summary>
	/// <param name="obj">The object to check</param>
	/// <returns>Success status- if the GameObject has a KartManager component</returns>
	public static bool IsKartGameObject(GameObject obj) 
	{
		return obj.GetComponent<KartManager>() != null;
	}
}