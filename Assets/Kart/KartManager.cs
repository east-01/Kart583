using System;
using EMullen.Core;
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
	private BLogChannel logSettings;
	public BLogChannel LogSettings => logSettings;

	[SerializeField] 
	private POIGDelegate poigDelegate;
	public POIGDelegate POIGDelegate { 
		get => poigDelegate;
		set => poigDelegate = value; 
	}
	public bool HasPOIGDelegate { get { return poigDelegate != null; } }

	private readonly SyncVar<string> ownerUID = new();
	public string OwnerUID => ownerUID.Value;
	[ServerRpc(RequireOwnership = false)]
	public void ServerRpcSetOwnerUID(string ownerUID) => this.ownerUID.Value = ownerUID;

	private readonly SyncVar<bool> isHuman = new();
	public bool IsHuman => isHuman.Value;
	public bool IsBot => !isHuman.Value;
	[ServerRpc]
	public void ServerRpcSetIsHuman(bool isHuman) => this.isHuman.Value = isHuman;	

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
		kartCtrl.enabled = IsOwner || IsServerInitialized;
		// kartItemManager: Stays enabled so we can sync item wielding between players
		// posTracker: Stays enabled, updates server on race position (TODO: Make this a server-side calculation it will be exploited)
		// kartEffectManager: Stays enabled

		// Bot/Human driver scripts are determined in UseHumanDriver and UseBotDriver

		GetComponent<Rigidbody>().isKinematic = !(IsOwner || IsServerInitialized);
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

		if(IsClientInitialized && !IsHostInitialized) {
			ServerRpcSetOwnerUID(ownerUID);
			ServerRpcSetIsHuman(true);
		} else if(base.IsServerInitialized) {
			this.ownerUID.Value = ownerUID;
			isHuman.Value = true;
		} else
			throw new InvalidOperationException("Tried to ready human driver without being a client.");

		PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(ownerUID);
		RaceData raceData = pd.GetData<RaceData>();
		raceData.ready = true;
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

		if(!IsServerInitialized) { // Used when the player finishes race and switches to bot controller
			ServerRpcSetOwnerUID(ownerUID);
			ServerRpcSetIsHuman(false);
		} else {
			this.ownerUID.Value = ownerUID;
			isHuman.Value = false;
		}

		PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(ownerUID);
		RaceData raceData = pd.GetData<RaceData>();
		raceData.ready = true;
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

public static class KartManagerExtensions 
{
	public static PlayerData GetPlayerData(this KartManager manager) 
	{
		if(PlayerDataRegistry.Instance == null) {
			Debug.LogError("Can't get player data, player data registry is null");
			return null;
		}
		if(!PlayerDataRegistry.Instance.Contains(manager.OwnerUID)) {
			Debug.LogError($"Can't get player data, player data registry doesn't contain owner uid \"{manager.OwnerUID}\"");
			return null;
		}
		return PlayerDataRegistry.Instance.GetPlayerData(manager.OwnerUID);
	}
}