
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// Karts in race manager tracks all kart objects in race
/// </summary>
public class KartsIRManager : NetworkBehaviour
{

    public static readonly string[] rlBotNames = { // luv u rl
		"Armstrong", "Bandit", "Beast", "Boomer", "Buzz", "C-Block", "Casper", "Caveman", "Centice", "Chipper",
		"Cougar", "Dude", "Foamer", "Fury", "Gerwin", "Goose", "Heater", "Hollywood", "Hound", "Iceman", "Imp",
		"Jester", "Junker", "Khan", "Marley", "Maverick", "Merlin", "Middy", "Mountain", "Myrtle", "Outlaw", "Poncho",
		"Rainmaker", "Raja", "Rex", "Roundhouse", "Sabretooth", "Saltie", "Samara", "Scout", "Shepard", "Slider",
		"Squall", "Sticks", "Stinger", "Storm", "Sultan", "Sundown", "Swabbie", "Tex", "Tusk", "Viper", "Wolfman", "Yuri"
	};
	public static readonly string KartNamePrefix = "Kart-";

	[SerializeField]
    private GameObject kartPrefab;
	[SerializeField] 
	private GameObject playerObjectInGamePrefab;

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

	/// <summary>
	/// A list of all Kart GameObjects, populated by ConnectToKart()
	/// </summary>
	public List<GameObject> kartObjects = new(); // This could include bots as well
	public List<PositionTracker> playerPositions = new();
	private Dictionary<string, PlayerObject> playerObjectsWaitingForKarts = new();

#region Initializers
	void Awake() 
	{
		gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;

		PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerJoined;
		if(SceneDelegate.Instance != null)
			SceneDelegate.Instance.ClientAddedToSceneEvent += SceneDelegate_ClientAddedToScene;

		print("TODO: Subscribe to player object spawned event in KartsIRManager, late join purposes.");
	}

    void Start() 
	{
		if(!CoreManager.IsMultiplayer)
			PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
	}

	private void OnDestroy() 
	{
		PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerJoined;
		if(SceneDelegate.Instance != null)
			SceneDelegate.Instance.ClientAddedToSceneEvent -= SceneDelegate_ClientAddedToScene;
	}

#endregion

	void Update()
    {
		if(!base.IsServer)
			return;

		playerPositions = playerPositions.OrderByDescending(o=>o.RaceCompletion).ToList();
		int i = 0;
		playerPositions.ForEach(pt => { 
			if(pt.Owner.IsValid)
				pt.TargetRpcSetRacePosition(pt.Owner, i); 
			else
				pt.racePos = i;
			i++; 
		});
    }

#region Events
	private void PlayerObjectManager_PlayerJoined(PlayerObject newPlayer) 
	{
		SpawnPlayer(newPlayer);
	}

    private void SceneDelegate_ClientAddedToScene(NetworkConnection client, SceneLookupData sceneLookupData)
    {
		if(sceneLookupData.Name == SceneNames.MENU_LOBBY)
			return;

		print("client added to map scene, spawning player objects");
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
    }
#endregion

#region Kart Spawning
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcSpawnKart(NetworkConnection owner, PlayerData data) { SpawnKart(data, owner); }

    /// <summary>
	/// Spawns a kart and add it to the game. Returns the KartManager from the new kart.
	/// </summary>
	public KartManager SpawnKart(PlayerData data, NetworkConnection owner = null) 
	{	
		if(!CoreManager.ServerRequirement) {
			Debug.LogError("Failed to meet server requirement.");
			return null;
		}

		if(KartCount >= 8) {
			Debug.LogError("Tried to add a new kart even though there is already 8 (or more) karts.");
            return null;
        }

		if(data.uuid == "") {
			Debug.LogError("Tried to spawn a kart with empty guid, this is not allowed");
			return null;
		}

		if(data.kartType == KartType.NONE) {
			data.kartType = SelectRandomKartType();
			Debug.LogWarning($"Tried to spawn a kart without a kartType included. Random type {data.kartType} selected.");
		}

		GameObject newKart = Instantiate(kartPrefab);
		KartManager newKartManager = KartBehavior.LocateManager(newKart);

        // GameObject position management
		Vector3 spawnPos = kartLevelManager.SpawnPositions.transform.GetChild(kartObjects.Count).position;
		Vector3 spawnForward = kartLevelManager.SpawnPositions != null ? kartLevelManager.SpawnPositions.spawnForward : new Vector3(1, 0, 0);

		newKart.transform.forward = spawnForward;
		newKart.transform.position = spawnPos;
		newKart.name = KartNamePrefix + data.name;

		// Spawn for server
		base.ServerManager.Spawn(newKart, owner, gameplayManager.GameLobby.MapScene.Value);
		newKart.GetComponent<NetworkObject>().SetParent(kartLevelManager.KartContainer.GetComponent<EmptyNetworkBehaviour>());

        // PlayerData management
		data.ready = false;

		newKartManager.SetPlayerData(data);

		// Run event
		if(CoreManager.IsMultiplayer) {
			ObserversRpcAddKart(owner, data);
		}

		return newKartManager;
	}

	public void AddKart(KartManager kartManager) 
	{
		kartObjects.Add(kartManager.gameObject);
		playerPositions.Add(kartManager.GetPositionTracker());
	}

	/// <summary>
	/// Tells all observers to add a Kart
	/// </summary>
    [ObserversRpc(RunLocally = true)]
    public void ObserversRpcAddKart(NetworkConnection ownerOfNewKart, PlayerData data) 
    {
		PlayerObject connectingPlayer = null; // Used for the instance that spawned the kart
		if(ownerOfNewKart == base.LocalConnection) {
			if(!playerObjectsWaitingForKarts.ContainsKey(data.uuid)) {
				Debug.LogError($"Couldn't find a PlayerObject waiting for kart with data {data.Summary}");
				return;
			}
			connectingPlayer = playerObjectsWaitingForKarts[data.uuid];
			playerObjectsWaitingForKarts.Remove(data.uuid);
		}

		StartCoroutine(KartSearchCoroutine(data, connectingPlayer));
    }

	/// <summary>
	/// Used for networked instances. Looks for a kart with the associated data.
	/// Has TWO MODES:
	///   1. If only the PlayerData is provided, it will simply add that Kart using AddKart
	///   2. If PlayerData and a PlayerObject is provided, it will add the kart using AddKart and 
	///      then connect it using ConnectToKart.
	/// Search attempts are made every 0.1s
	/// </summary>
	private IEnumerator KartSearchCoroutine(PlayerData data, PlayerObject playerObj = null) 
	{
		if(playerObj != null && data.uuid != playerObj.data.uuid) {
			Debug.LogError("PlayerData provided doesn't match the data on the PlayerObject!");
			yield break;
		}

		bool connected = false;
		for(int attempts = 0; attempts <= 50; attempts++) {
			KartManager pkm = SearchForKartManager(data);
			if(pkm != null) {
				AddKart(pkm);
	
				if(playerObj != null)
					ConnectToKart(playerObj, pkm);
	
				connected = true;
				break;
			} else 
				yield return new WaitForSeconds(0.1f);
		}
		if(!connected)
			Debug.LogError($"Failed to connect player data to kart. Data: {data.Summary}");
	}
#endregion

#region Player Spawning
	/// <summary>
	/// Spawn a Kart using SpawnKart, different behaviors for a networked and local instance:
	/// For network:
	///   - Client calls SpawnKart on server
	///   - PlayerObject is queued in playerObjectsWaitingForKarts
	///   - Once the server spawns kart, client recieves ConnectPlayerToKart
	///   - Client attempts to connect kart
	/// For local:
	///   - SpawnKart is called
	///   - ConnectPlayerToKart is called using the returned KartManager
	/// </summary>
	public void SpawnPlayer(PlayerObject player) 
	{
		if(CoreManager.IsMultiplayer) {
			ServerRpcSpawnKart(base.LocalConnection, player.data);
			playerObjectsWaitingForKarts.Add(player.data.uuid, player);		
		} else {
			KartManager spawned = SpawnKart(player.data);
			AddKart(spawned);
			ConnectToKart(player, spawned);
		}
	}

	/// <summary>
	/// Attempts to connect a PlayerObjectInGame object to a specified kart with kartdata.
	/// </summary>
	/// <returns>Success status</returns>
	private void ConnectToKart(PlayerObject playerObj, KartManager kartManager) 
	{		
		// Spawn player object in game prefab
		GameObject poig = Instantiate(playerObjectInGamePrefab, kartLevelManager.KartContainer);
		POIGDelegate poigDelegate = poig.GetComponent<POIGDelegate>();

		poigDelegate.owner = playerObj;
		playerObj.poigDelegate = poigDelegate;
		kartManager.GetKartVisualsManager().LoadNameplate();

		// Make connections for PlayerInput
		Camera pcam = poigDelegate.Camera;
		pcam.enabled = false;
		pcam.GetComponent<AudioListener>().enabled = false;
		pcam.GetComponent<KartControllerFollow>().subject = kartManager.GetKartController();

		poigDelegate.HUD.GetComponent<PlayerHUDCanvas>().subject = kartManager;

		playerObj.input.camera = pcam;
		playerObj.input.uiInputModule = null; // Destroy menu player input module

		// Connect player kart manager to player object
		kartManager.UseHumanDriver(playerObj.input);
		kartManager.POIGDelegate = poigDelegate;
		print("set poigdelegate as " + poigDelegate);

		// Pass late join phase (does nothing if we're not in late join)
		gameplayManager.RaceManager.PassLateJoin();
		return;
	}
#endregion

#region Bot Spawning
	public void SpawnBot() 
	{		
		if(!CoreManager.ServerRequirement) {
			Debug.LogError("Failed to meet server requirement.");
			return;
		}

        PlayerData bdata = new() {
			uuid = Guid.NewGuid().ToString(),
            name = SelectUniqueRandomBotName(),
			kartType = SelectRandomKartType()
        };
		KartManager bkm = SpawnKart(bdata);
		bkm.UseBotDriver();
	}

    public void SpawnBots() 
    {
		if(!CoreManager.ServerRequirement) {
			Debug.LogError("Failed to meet server requirement.");
			return;
		}

		RaceSettings settings = gameplayManager.RaceManager.settings;
        if(settings.Bots) {
            int botsToSpawn = Math.Min(settings.botLimit, CoreManager.Instance.PlayerLimit-KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                SpawnBot();
            }
        }
    }
#endregion

#region Utility Methods
	/// <summary>
	/// Takes a PlayerData object and locates the associated KartManager with it
	/// </summary>
	public KartManager SearchForKartManager(PlayerData data) { return SearchForKartManager(data.uuid); }
	/// <summary>
	/// Takes a PlayerData object and locates the associated KartManager with it
	/// </summary>
	public KartManager SearchForKartManager(string playerUUID) {
		foreach(KartManager km in FindObjectsOfType<KartManager>()) {
			if(km.GetPlayerData().uuid == playerUUID)
				return km;
		}
		return null;
	}

    /// <summary>
	/// Check if a name is unique among karts
	/// </summary>
	public bool IsNameUnique(string name) {
		foreach(GameObject go in kartObjects) {
			if(KartBehavior.LocateManager(go).GetPlayerData().name == name)
				return false;		
		}
		return true;
	}

    public KartType SelectRandomKartType() 
	{
		Array enumVals = Enum.GetValues(typeof(KartType));
		return (KartType)enumVals.GetValue(new System.Random().Next(1, enumVals.Length));
	}

	public string SelectUniqueRandomBotName() 
	{
		for(int attempt = 0; attempt < rlBotNames.Length; attempt++) {
			string selection = SelectRandomBotName() + " (Bot)";
			if(IsNameUnique(selection))
				return selection;
		}
		return "Bot";
	}	

	public static string SelectRandomBotName() 
	{
		return rlBotNames[UnityEngine.Random.Range(0, rlBotNames.Length)];
	}
#endregion

	public bool AllPlayersReady { get {
		bool allPlayersReady = true;
		foreach(GameObject obj in gameplayManager.PlayerManager.kartObjects) {
			if(!KartBehavior.LocateManager(obj).GetPlayerData().ready) {
				allPlayersReady = false;
				break;
			}
		}
		return allPlayersReady;
	} }
	public int KartCount { get { return kartObjects.Count; } }
	public int HumanPlayerCount { get { 
		int counter = 0;
		foreach(GameObject ko in kartObjects) {
			if(KartBehavior.LocateManager(ko).IsHuman)
				counter++;
		}
		return counter;
	 } }
	public int BotPlayerCount { get { return KartCount-HumanPlayerCount; } }

}
