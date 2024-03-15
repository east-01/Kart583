
using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

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

	public List<GameObject> kartObjects = new(); // This could include bots as well
	public List<PositionTracker> playerPositions = new();
	private Dictionary<string, PlayerObject> playerObjectsWaitingForKarts = new();

	void Awake() 
	{
		gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;
		kartSpawner = GetComponent<KartSpawner>();

		kartSpawner.KartSpawnedEvent += KartManager_KartSpawned;

		print("TODO: Subscribe to player object spawned event in KartsIRManager, late join purposes.");
	}

	void Start() 
	{
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
	}

	private void OnDestroy() 
	{
		kartSpawner.KartSpawnedEvent -= KartManager_KartSpawned;
	}

	void Update()
    {
		playerPositions = playerPositions.OrderByDescending(o=>o.raceCompletion).ToList();
		int i = 0;
		playerPositions.ForEach(pt => { pt.racePos = i; i++; });
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
		playerObjectsWaitingForKarts.Add(player.data.name, player);		
	}

	public void KartManager_KartSpawned(NetworkConnection conn, PlayerData data) 
	{
		if(conn != base.LocalConnection)
			return;

		if(!playerObjectsWaitingForKarts.ContainsKey(data.name)) {
			Debug.LogError($"Couldn't find a PlayerObject waiting for kart with data name \"{data.name}\"");
			return;
		}

		PlayerObject player = playerObjectsWaitingForKarts[data.name];
		
		// Find the kart that was spawned
		KartManager pkm = SearchForKartManager(data);
		if(pkm == null) {
			Debug.LogError($"Failed to find spawned kart. Player data name is \"{data.name}\"");
			return;
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

		// Pass late join phase (does nothing if we're not in late join)
		gameplayManager.RaceManager.PassLateJoin();

	}

	/// <summary>
	/// Takes a PlayerData object and locates the associated KartManager with it
	/// </summary>
	public KartManager SearchForKartManager(PlayerData data) 
	{
		foreach(KartManager km in FindObjectsOfType<KartManager>()) {
			if(km.GetPlayerData().name == data.name)
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
