
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
	public static readonly string[] rlBotNames = { // luv u rl
		"Armstrong", "Bandit", "Beast", "Boomer", "Buzz", "C-Block", "Casper", "Caveman", "Centice", "Chipper",
		"Cougar", "Dude", "Foamer", "Fury", "Gerwin", "Goose", "Heater", "Hollywood", "Hound", "Iceman", "Imp",
		"Jester", "Junker", "Khan", "Marley", "Maverick", "Merlin", "Middy", "Mountain", "Myrtle", "Outlaw", "Poncho",
		"Rainmaker", "Raja", "Rex", "Roundhouse", "Sabretooth", "Saltie", "Samara", "Scout", "Shepard", "Slider",
		"Squall", "Sticks", "Stinger", "Storm", "Sultan", "Sundown", "Swabbie", "Tex", "Tusk", "Viper", "Wolfman", "Yuri"
	};
	private static readonly string KartNamePrefix = "Kart-";

	[SerializeField] private GameObject kartPrefab;
	[SerializeField] private GameObject playerObjectInGamePrefab;

	public List<GameObject> kartObjects = new(); // This could include bots as well
	public List<PositionTracker> playerPositions = new();
	private Dictionary<string, PlayerObject> playerObjectsWaitingForKarts = new();

	void Start() 
	{
		PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => SpawnPlayer(po));
	}

	void Update()
    {
		playerPositions = playerPositions.OrderByDescending(o=>o.raceCompletion).ToList();
		int i = 0;
		playerPositions.ForEach(pt => { pt.racePos = i; i++; });
    }

	/// <summary>
	/// Spawns a kart and add it to the game. Returns the KartManager from the new kart.
	/// </summary>
	KartManager SpawnKart(NetworkConnection owner, PlayerData data) 
	{	
		if(kartObjects.Count >= 8)
			throw new InvalidOperationException("Tried to add a new kart even though there is already 8 (or more) karts.");
		if(!base.IsServer)
			throw new InvalidOperationException("Tried to spawn a new kart on a client.");

		GameObject newKart = Instantiate(kartPrefab, GameplayManager.KartContainer);
		base.ServerManager.Spawn(newKart.GetComponent<NetworkObject>(), owner);

		// Connects the owner to the scene that the GameplayManager is on
		// TODO: This should be more robust
		// base.SceneManager.AddOwnerToDefaultScene(newKart.GetComponent<NetworkObject>());

        print("spawned " + newKart.name);

		KartManager newKartManager = KartBehavior.LocateManager(newKart);
		newKartManager.SetPlayerData(data);

		Vector3 spawnPos = GameplayManager.SpawnPositions.transform.GetChild(kartObjects.Count).position;
		Vector3 spawnForward = GameplayManager.SpawnPositions != null ? GameplayManager.SpawnPositions.spawnForward : new Vector3(1, 0, 0);

		newKart.transform.forward = spawnForward;
		newKart.transform.position = spawnPos;
		newKart.name = KartNamePrefix + data.name;

		kartObjects.Add(newKart);
		playerPositions.Add(newKartManager.GetPositionTracker());

		return newKartManager;
	}

	/// <summary>
	/// Spawns a kart using SpawnKart, spawns a PlayerObjectInGame object and connects them.
	/// </summary>
	/// <param name="player"></param>
	public void SpawnPlayer(PlayerObject player)
	{
		// Player was spawned via late join, give them a random kart name
		if(player.data.kartName == KartName.NONE) 
			player.data.kartName = SelectRandomKartName();

		print("player kart name " + player.data.kartName);
		SpawnPlayer_Server(player.data, base.LocalConnection);
		playerObjectsWaitingForKarts.Add(player.data.name, player);
		
	}

	[ServerRpc(RequireOwnership = false)]
	public void SpawnPlayer_Server(PlayerData data, NetworkConnection source) {
		print("Spawning player (" + kartObjects.Count + " objects)");
		SpawnKart(source, data);
		ConnectPlayerToKart(source, data);
	}

	[TargetRpc]
	public void ConnectPlayerToKart(NetworkConnection conn, PlayerData data) {

		PlayerObject player = playerObjectsWaitingForKarts[data.name];

		// Find the kart that was spawned
		GameObject spawnedPlayerKart = GameObject.Find(KartNamePrefix+data.name);
		if(spawnedPlayerKart == null) {
			print("FAILED TO FIND SPAWNED KART");
			return;
		}
			// throw new InvalidOperationException("Failed to find spawned player kart.");

		KartManager pkm = KartBehavior.LocateManager(spawnedPlayerKart);

		// Spawn player object in game prefab
		GameObject poig = Instantiate(playerObjectInGamePrefab, GameplayManager.KartContainer);
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
	}

	[ServerRpc(RequireOwnership = false)]
	public void SpawnBot() 
	{		
		print("Spawning bot (" + kartObjects.Count + " objects)");
        PlayerData bdata = new() {
            name = rlBotNames[UnityEngine.Random.Range(0, rlBotNames.Length)] + " (Bot)",
			kartName = SelectRandomKartName()
        };
		KartManager bkm = SpawnKart(null, bdata);
		bkm.UseBotDriver();
	}

	[ServerRpc(RequireOwnership = false)]
    public void SpawnBots() 
    {
		RaceSettings settings = GameplayManager.RaceManager.settings;
        if(settings.bots) {
            int botsToSpawn = Math.Min(settings.botLimit, PlayerLimit-KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                SpawnBot();
            }
        }
    }

	private KartName SelectRandomKartName() 
	{
		Array enumVals = Enum.GetValues(typeof(KartName));
		return (KartName)enumVals.GetValue(new System.Random().Next(1, enumVals.Length));
	}

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
