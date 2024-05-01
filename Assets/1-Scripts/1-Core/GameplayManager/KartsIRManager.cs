
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
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

	public delegate void KartSpawnHandler(NetworkConnection owner, PlayerData data);
    public event KartSpawnHandler KartSpawnedEvent;

#region Initializers
	void Awake() 
	{
		gameplayManager = GetComponent<GameplayManager>();
		kartLevelManager = gameplayManager.KartLevelManager;

		KartSpawnedEvent += KartManager_KartSpawned;
		SceneDelegate.Instance.ClientAddedToSceneEvent += SceneDelegate_ClientAddedToScene;
		// PlayerObjectManager.Instance.PlayerObjectJoinedEvent += PlayerObjectManager_PlayerObjectJoined;
	}

    void Start() 
	{
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
	}

	private void OnDestroy() 
	{
		KartSpawnedEvent -= KartManager_KartSpawned;
		SceneDelegate.Instance.ClientAddedToSceneEvent -= SceneDelegate_ClientAddedToScene;
		// PlayerObjectManager.Instance.PlayerObjectJoinedEvent -= PlayerObjectManager_PlayerObjectJoined;
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
    private void SceneDelegate_ClientAddedToScene(NetworkConnection client, SceneLookupData sceneLookupData)
    {
		if(!SceneNames.IsMapScene(sceneLookupData.Name))
			return;

		print("client added to map scene, spawning player objects");
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
    }

	private void PlayerObjectManager_PlayerObjectJoined(PlayerObject newObject) 
	{	
		if(gameplayManager == null || !gameplayManager.HasLobby)
			return;
		SpawnPlayer(newObject);
	}
#endregion

#region Kart Spawning
	[ServerRpc(RequireOwnership = false)]
    public void ServerRpcSpawnKart(NetworkConnection owner, PlayerData data) { print("serverrpc called"); SpawnKart(owner, data); }

    /// <summary>
	/// Spawns a kart and add it to the game. Returns the KartManager from the new kart.
	/// </summary>
	[Server]
	KartManager SpawnKart(NetworkConnection owner, PlayerData data) 
	{	
		print("Spawning kart on server");
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
		print("SPAWNIGN WITH OWNER " + owner);
		base.ServerManager.Spawn(newKart, owner, gameplayManager.GameLobby.MapScene.Value);
		newKart.GetComponent<NetworkObject>().SetParent(kartLevelManager.KartContainer.GetComponent<EmptyNetworkBehaviour>());

        // PlayerData management
		data.ready = false;

		newKartManager.SetPlayerData(data);

		// Run event
		ObserversRpcCallSpawnEvent(owner, data);

		return newKartManager;
	}

    [ObserversRpc(RunLocally = true)]
    public void ObserversRpcCallSpawnEvent(NetworkConnection client, PlayerData data) 
    {
        KartSpawnedEvent?.Invoke(client, data);
    }
#endregion

#region Player Spawning
	/// <summary>
	/// Spawns a kart using SpawnKart, queues up the PlayerObject to wait for when the server spawns the kart.
	/// Once the server spawns the kart, the client recieves the ConnectPlayerToKart call, and spawns a
	///   PlayerObjectInGame object and connects all elements to the newly spawned kart.
	/// </summary>
	[Client]
	public void SpawnPlayer(PlayerObject player)
	{
		print("Spawned player, is Local: " + CoreManager.IsLocal + " is server: " + base.IsServer + " is client: " + base.IsClient);
		ServerRpcSpawnKart(base.LocalConnection, player.data);
		playerObjectsWaitingForKarts.Add(player.data.uuid, player);		
	}

	public void KartManager_KartSpawned(NetworkConnection conn, PlayerData data) 
	{		
		bool shouldAttemptToConnectPlayerObject = conn == base.LocalConnection && playerObjectsWaitingForKarts.ContainsKey(data.uuid);
		StartCoroutine(KartSearchCoroutine(data, shouldAttemptToConnectPlayerObject));
	}

	/// <summary>
	/// Will repeatedly attempt to connect a POIG to a kart every 0.1s until success.
	/// See ConnectToKart for more details
	/// </summary>
	private IEnumerator KartSearchCoroutine(PlayerData data, bool attemptToConnectPlayerObject) 
	{
		bool connected = false;
		for(int attempts = 0; !connected && attempts <= 50; attempts++) {
			connected = ConnectToKart(data, attemptToConnectPlayerObject);
			if(!connected)
				yield return new WaitForSeconds(0.1f);
		}
		if(!connected)
			Debug.LogError($"Failed to connect player data to kart. Data: {data.Summary}");
	}

	/// <summary>
	/// Attempts to connect a PlayerObjectInGame object to a specified kart with kartdata.
	/// </summary>
	/// <returns>Success status</returns>
	private bool ConnectToKart(PlayerData data, bool attemptToConnectPlayerObject) 
	{
		// Find the kart that was spawned add it to the KartObjects array
		KartManager pkm = SearchForKartManager(data);
		if(pkm == null) 
			return false;

		kartObjects.Add(pkm.gameObject);
		playerPositions.Add(pkm.GetPositionTracker());

		// If we're not attempting to connect a player object we can return true since success is only adding to kartObjects array
		if(!attemptToConnectPlayerObject)
			return true;
		
		PlayerObject player = playerObjectsWaitingForKarts[data.uuid];
		
		if(!playerObjectsWaitingForKarts.ContainsKey(data.uuid)) {
			Debug.LogError($"Couldn't find a PlayerObject waiting for kart with data {data.Summary}");
			return false;
		}

		// Spawn player object in game prefab
		GameObject poig = Instantiate(playerObjectInGamePrefab, kartLevelManager.KartContainer);
		POIGDelegate poigDelegate = poig.GetComponent<POIGDelegate>();

		poigDelegate.owner = player;
		player.poigDelegate = poigDelegate;
		pkm.GetKartVisualsManager().LoadNameplate();

		// Make connections for PlayerInput
		Camera pcam = poigDelegate.Camera;
		pcam.enabled = false;
		pcam.GetComponent<AudioListener>().enabled = false;
		pcam.GetComponent<KartControllerFollow>().subject = pkm.GetKartController();

		poigDelegate.HUD.GetComponent<PlayerHUDCanvas>().subject = pkm;

		player.input.camera = pcam;
		player.input.uiInputModule = null; // Destroy menu player input module

		// Connect player kart manager to player object
		pkm.UseHumanDriver(player.input);
		pkm.POIGDelegate = poigDelegate;
		print("set poigdelegate as " + poigDelegate);

		// Pass late join phase (does nothing if we're not in late join)
		gameplayManager.RaceManager.PassLateJoin();
		return true;
	}
#endregion

#region Bot Spawning
	[Server]
	public void SpawnBot() 
	{		
        PlayerData bdata = new() {
			uuid = Guid.NewGuid().ToString(),
            name = SelectUniqueRandomBotName(),
			kartType = SelectRandomKartType()
        };
		KartManager bkm = SpawnKart(null, bdata);
		bkm.UseBotDriver();
	}

	[Server]
    public void SpawnBots() 
    {
		RaceSettings settings = gameplayManager.RaceManager.settings;
        if(settings.Bots) {
            int botsToSpawn = Math.Min(settings.botLimit, CoreManager.Instance.PlayerLimit-KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                SpawnBot();
            }
        }
    }
#endregion

#region Utils
	/// <summary>
	/// Takes a PlayerData object and locates the associated KartManager with it
	/// </summary>
	public KartManager SearchForKartManager(PlayerData data) 
	{
		return SearchForKartManager(data.uuid);
	}

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
