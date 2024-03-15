using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class KartSpawner : NetworkBehaviour
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

    public delegate void KartSpawnHandler(NetworkConnection owner, PlayerData data);
    public event KartSpawnHandler KartSpawnedEvent;

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;
    private KartsIRManager playerManager;

    void Awake()
    {
        playerManager = GetComponent<KartsIRManager>();
        gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;
    }

    void Update()
    {
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcSpawnKart(NetworkConnection owner, PlayerData data) 
    {
        SpawnKart(owner, data);
    }

    /// <summary>
	/// Spawns a kart and add it to the game. Returns the KartManager from the new kart.
	/// </summary>
	[Server]
	KartManager SpawnKart(NetworkConnection owner, PlayerData data) 
	{	
		if(playerManager.KartCount >= 8) {
			Debug.LogError("Tried to add a new kart even though there is already 8 (or more) karts.");
            return null;
        }

		if(data.kartType == KartType.NONE) {
			data.kartType = SelectRandomKartType();
			Debug.LogWarning($"Tried to spawn a kart without a kartType included. Random type {data.kartType} selected.");
		}

		GameObject newKart = Instantiate(kartPrefab);
		KartManager newKartManager = KartBehavior.LocateManager(newKart);

        // GameObject position management
		Vector3 spawnPos = kartLevelManager.SpawnPositions.transform.GetChild(playerManager.kartObjects.Count).position;
		Vector3 spawnForward = kartLevelManager.SpawnPositions != null ? kartLevelManager.SpawnPositions.spawnForward : new Vector3(1, 0, 0);

		newKart.transform.forward = spawnForward;
		newKart.transform.position = spawnPos;
		newKart.name = KartNamePrefix + data.name;

		// Spawn for server
		base.ServerManager.Spawn(newKart, owner, gameplayManager.GameLobby.MapScene.Value);
		newKart.GetComponent<NetworkObject>().SetParent(kartLevelManager.KartContainer.GetComponent<EmptyNetworkBehaviour>());

        // PlayerData management
		data.ready = false;
		if(!IsNameUnique(data.name))
			data.name = MakeNameUnique(data.name);

		newKartManager.SetPlayerData(data);

        // Register new kart with playerManager
		playerManager.kartObjects.Add(newKart);
		playerManager.playerPositions.Add(newKartManager.GetPositionTracker());

		// Run event
		ObserversRpcCallSpawnEvent(owner, data);

		return newKartManager;
	}

    [ObserversRpc(RunLocally = true)]
    public void ObserversRpcCallSpawnEvent(NetworkConnection client, PlayerData data) 
    {
        KartSpawnedEvent?.Invoke(client, data);
    }

	[Server]
	public void SpawnBot() 
	{		
        PlayerData bdata = new() {
            name = SelectRandomBotName(),
			kartType = SelectRandomKartType()
        };
		KartManager bkm = SpawnKart(null, bdata);
		bkm.UseBotDriver();
	}

	[Server]
    public void SpawnBots() 
    {
		RaceSettings settings = gameplayManager.RaceManager.settings;
        if(settings.bots) {
            int botsToSpawn = Math.Min(settings.botLimit, KartsIRManager.PlayerLimit-playerManager.KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                SpawnBot();
            }
        }
    }

    /// <summary>
	/// Check if a name is unique among karts
	/// </summary>
	public bool IsNameUnique(string name) {
		foreach(GameObject go in playerManager.kartObjects) {
			if(KartBehavior.LocateManager(go).GetPlayerData().name == name)
				return false;		
		}
		return true;
	}

	/// <summary>
	/// Makes a name unique by appending integers to the end of the input
	/// </summary>
	public string MakeNameUnique(string name) {
		int i = 0;
		while(!IsNameUnique(name + i))
			i++;
		return name + i;
	}

    public KartType SelectRandomKartType() 
	{
		Array enumVals = Enum.GetValues(typeof(KartType));
		return (KartType)enumVals.GetValue(new System.Random().Next(1, enumVals.Length));
	}

	public string SelectRandomBotName() 
	{
		for(int attempt = 0; attempt < rlBotNames.Length; attempt++) {
			string selection = rlBotNames[UnityEngine.Random.Range(0, rlBotNames.Length)] + " (Bot)";
			if(IsNameUnique(selection))
				return selection;
		}
		return "Bot";
	}	

}
