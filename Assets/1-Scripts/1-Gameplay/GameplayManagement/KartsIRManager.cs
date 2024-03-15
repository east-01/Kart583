
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
		playerPositions = playerPositions.OrderByDescending(o=>o.raceCompletion).ToList();
		int i = 0;
		playerPositions.ForEach(pt => { pt.racePos = i; i++; });
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
		playerObjectsWaitingForKarts.Add(player.data.guid, player);		
	}

	public void KartManager_KartSpawned(NetworkConnection conn, PlayerData data) 
	{
		if(conn != base.LocalConnection)
			return;

		if(!playerObjectsWaitingForKarts.ContainsKey(data.guid)) {
			Debug.LogError($"Couldn't find a PlayerObject waiting for kart with data {data.Summary}");
			return;
		}

		StartCoroutine(KartSearchCoroutine(data));
	}

	/// <summary>
	/// Will repeatedly attempt to connect a POIG to a kart every 0.1s until success.
	/// See ConnectToKart for more details
	/// </summary>
	private IEnumerator KartSearchCoroutine(PlayerData data) 
	{
		bool connected = false;
		for(int attempts = 0; !connected && attempts <= 50; attempts++) {
			connected = ConnectToKart(data);
			if(!connected)
				yield return new WaitForSeconds(0.1f);
		}
		if(!connected)
			Debug.LogError($"Failed to connect player data to kart. Data: {data.Summary}");
	}


	/// <summary>
	/// Attempts to connect a PlayerObjectInGame object to a specified kart with kartdata.
	/// Will return true 
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	private bool ConnectToKart(PlayerData data) 
	{
		PlayerObject player = playerObjectsWaitingForKarts[data.guid];
		
		// Find the kart that was spawned
		KartManager pkm = SearchForKartManager(data);
		if(pkm == null) 
			return false;

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
		return true;
	}

	/// <summary>
	/// Takes a PlayerData object and locates the associated KartManager with it
	/// </summary>
	public KartManager SearchForKartManager(PlayerData data) 
	{
		foreach(KartManager km in FindObjectsOfType<KartManager>()) {
			if(km.GetPlayerData().guid == data.guid)
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
