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
	private bool waitingForKartType;

	new protected void Awake() 
	{
		base.Awake();
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
	}

	public void GameplayManagerLoaded(GameplayManager gameplayManager) 
	{
		this.gameplayManager = gameplayManager;

		if(kartManager.GetPlayerData().kartType == KartType.NONE) 
			waitingForKartType = true;
		else
			InitializeKartType();
	}

    public override void OnOwnershipClient(NetworkConnection prevOwner)
    {
		// Sync enabled status with our ownership status
		kartCtrl.enabled = base.IsOwner;
		kartStateManager.enabled = base.IsOwner;
		// kartItemManager: Stays enabled so we can sync item wielding between players
		// posTracker: Stays enabled, updates server on race position (TODO: Make this a server-side calculation it will be exploited)
		// kartEffectManager: Stays enabled

		// Bot/Human driver scripts are determined in UseHumanDriver and UseBotDriver
    }

    private void Update() 
	{
		if(waitingForKartType) {
			if(kartManager.GetPlayerData().kartType != KartType.NONE) {
				InitializeKartType();
				waitingForKartType = false;
			} else 
				return;
		}
	}

	public void InitializeKartType() 
	{
		KartDataPackage kdp = gameplayManager.KartAtlas.RetrieveData(kartManager.GetPlayerData().kartType);
		kartCtrl.settings = kdp.settings;
	
		GameObject newKartModel = Instantiate(kdp.model.gameObject, transform);
		newKartModel.GetComponent<KartModel>().SetKartController(kartCtrl);
		kartCtrl.kartModel = newKartModel.transform;

		if(kartCtrl.kartModel != null) 
			kartCtrl.initKartModelY = kartCtrl.kartModel.localPosition.y;
		else
			Debug.LogWarning("KartController on \"" + kartCtrl.gameObject.name + "\" doesn't have a kartModel assigned."); 
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

	public void SetPlayerData(PlayerData data) 
	{
		this.data = data;
	}

	private void PlayerDataChanged(PlayerData prev, PlayerData current, bool asServer) 
	{
		gameObject.name = KartSpawner.KartNamePrefix + data.name;
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