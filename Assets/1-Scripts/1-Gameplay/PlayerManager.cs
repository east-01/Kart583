using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerManager : MonoBehaviour
{

	public static readonly int PlayerLimit = 8;
	public static readonly string[] rlBotNames = { // luv u rl
		"Armstrong", "Bandit", "Beast", "Boomer", "Buzz", "C-Block", "Casper", "Caveman", "Centice", "Chipper",
		"Cougar", "Dude", "Foamer", "Fury", "Gerwin", "Goose", "Heater", "Hollywood", "Hound", "Iceman", "Imp",
		"Jester", "Junker", "Khan", "Marley", "Maverick", "Merlin", "Middy", "Mountain", "Myrtle", "Outlaw", "Poncho",
		"Rainmaker", "Raja", "Rex", "Roundhouse", "Sabretooth", "Saltie", "Samara", "Scout", "Shepard", "Slider",
		"Squall", "Sticks", "Stinger", "Storm", "Sultan", "Sundown", "Swabbie", "Tex", "Tusk", "Viper", "Wolfman", "Yuri"
	};

	[SerializeField] private GameObject kartPrefab;
	[SerializeField] private GameObject playerObjectInGamePrefab;

	public List<GameObject> kartObjects = new(); // This could include bots as well
	public List<PlayerInput> playerInputs = new();
	public List<PositionTracker> playerPositions = new();

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

	void AddKart(KartManager kart) 
	{	
		if(kartObjects.Count >= 8) {
			Debug.LogError("Tried to add a new player even though there is already 8 (or more) players.");
			Destroy(kart);
			return;
		}

		kart.transform.forward = GameplayManager.SpawnPositions != null ? GameplayManager.SpawnPositions.spawnForward : new Vector3(1, 0, 0);
		Vector3 spawnPos = GameplayManager.SpawnPositions.transform.GetChild(kartObjects.Count).position;
		kart.transform.position = spawnPos;

		kartObjects.Add(kart.gameObject);
		playerPositions.Add(kart.GetPositionTracker());
	}

	void SpawnPlayer(PlayerObject player)
	{
		
		// Spawn player object in game prefab
		GameObject poig = Instantiate(playerObjectInGamePrefab);
		KartManager pkm = KartBehavior.LocateManager(poig.GetComponent<POIGDelegate>().KartObject);

		// Make connections for PlayerInput
		Camera pcam = poig.GetComponent<POIGDelegate>().Camera;
		pcam.enabled = false;
		pcam.GetComponent<AudioListener>().enabled = false;
		player.input.camera = pcam;

		playerInputs.Add(player.input);

		// Connect player kart manager to player object
		pkm.SetPlayerData(player.data);
		pkm.UseHumanDriver(player.input);

		AddKart(pkm);
	}

	public void SpawnBot() 
	{
		GameObject obj = Instantiate(kartPrefab);
		
		KartManager bkm = KartBehavior.LocateManager(obj);
        PlayerData bdata = new() {
            name = rlBotNames[Random.Range(0, rlBotNames.Length)] + " (Bot)"
        };
        bkm.SetPlayerData(bdata);
		bkm.UseBotDriver();

		AddKart(bkm);
	}

	public int KartCount { get { return kartObjects.Count; } }
	public int HumanPlayerCount { get { return playerInputs.Count; } }
	public int BotPlayerCount { get { return KartCount-HumanPlayerCount; } }

}
