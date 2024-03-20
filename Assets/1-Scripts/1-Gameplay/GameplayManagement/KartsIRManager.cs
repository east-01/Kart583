
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

	public static readonly int PlayerLimit = 8;

	[SerializeField] 
	private GameObject playerObjectInGamePrefab;

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;
	private KartSpawner kartSpawner;

	/// <summary>
	/// A list of all Kart GameObjects, populated by ConnectToKart()
	/// </summary>
	public List<GameObject> kartObjects = new(); // This could include bots as well
	public List<PositionTracker> playerPositions = new();
	private Dictionary<string, PlayerObject> playerObjectsWaitingForKarts = new();

	void Awake() 
	{
		gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;
		kartSpawner = GetComponent<KartSpawner>();

		kartSpawner.KartSpawnedEvent += KartManager_KartSpawned;
		SceneDelegate.Instance.ClientAddedToSceneEvent += SceneDelegate_ClientAddedToScene;

		print("TODO: Subscribe to player object spawned event in KartsIRManager, late join purposes.");
	}

    void Start() 
	{
		// PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
	}

	private void OnDestroy() 
	{
		kartSpawner.KartSpawnedEvent -= KartManager_KartSpawned;
		SceneDelegate.Instance.ClientAddedToSceneEvent -= SceneDelegate_ClientAddedToScene;
	}

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

    private void SceneDelegate_ClientAddedToScene(NetworkConnection client, SceneLookupData sceneLookupData)
    {
		if(sceneLookupData.Name == SceneNames.MENU_LOBBY)
			return;

		print("client added to map scene, spawning player objects");
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
    }

	/// <summary>
	/// Spawns a kart using SpawnKart, queues up the PlayerObject to wait for when the server spawns the kart.
	/// Once the server spawns the kart, the client recieves the ConnectPlayerToKart call, and spawns a
	///   PlayerObjectInGame object and connects all elements to the newly spawned kart.
	/// </summary>
	[Client]
	public void SpawnPlayer(PlayerObject player)
	{
		kartSpawner.ServerRpcSpawnKart(base.LocalConnection, player.data);
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

		// Pass late join phase (does nothing if we're not in late join)
		gameplayManager.RaceManager.PassLateJoin();
		return true;
	}

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
