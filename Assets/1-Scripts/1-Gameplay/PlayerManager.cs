using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class PlayerManager : MonoBehaviour
{

	public static readonly int PlayerLimit = 8;

	public GameObject kartBotPrefab;

	public List<GameObject> kartObjects = new List<GameObject>(); // This could include bots as well
	public List<PlayerInput> playerInputs = new List<PlayerInput>();
	public List<PositionTracker> playerPositions = new List<PositionTracker>();

	private PlayerInputManager controls;
	private List<KartManager> joinQueue = new List<KartManager>();

	private void Awake()
	{
		controls = GetComponent<PlayerInputManager>();
	}

	void OnEnable()
    {
		controls.onPlayerJoined += PlayerJoined;
        controls.onPlayerLeft += PlayerLeft;
    }

	private void OnDisable()
	{
		controls.onPlayerJoined -= PlayerJoined;
        controls.onPlayerLeft -= PlayerLeft;		
	}

	void Update()
    {
		playerPositions = playerPositions.OrderByDescending(o=>o.raceCompletion).ToList();
		int i = 0;
		playerPositions.ForEach(pt => { pt.racePos = i; i++; });

		// Use a join queue so that we only add karts that are ready
		// Without this, the kart wouldn't be completely initialized by the time we use AddKart(), so we have to wait.
		List<KartManager> toRemove = new List<KartManager>();
		joinQueue.ForEach(km => {
			if(km.GetPositionTracker() != null) {
				AddKart(km);
				toRemove.Add(km);
			}
		});

		toRemove.ForEach(km => joinQueue.Remove(km));
    }

	void AddKart(KartManager kart) 
	{	
		if(kartObjects.Count == 0) {
			// TODO: Load settings from menu
			GameplayManager.RaceManager.Activate(null);
		}
		
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

	void PlayerJoined(PlayerInput player)
	{

		if(playerInputs.Count == 0) {
			Camera.main.GetComponent<AudioListener>().enabled = false;
			Camera.main.enabled = false; 
		} else { // Player one's camera always gets the audio listener
			player.camera.GetComponent<AudioListener>().enabled = false;
		}
		playerInputs.Add(player);
		joinQueue.Add(player.gameObject.GetComponentInParent<KartManager>());
		// AddKart(player.gameObject.GetComponentInParent<KartManager>());
	}

	void PlayerLeft(PlayerInput player) 
	{ 
		
	}

	public void SpawnBot() 
	{
		GameObject obj = Instantiate(kartBotPrefab);
		AddKart(obj.GetComponent<KartManager>());
	}

	public int KartCount { get { return kartObjects.Count; } }
	public int HumanPlayerCount { get { return playerInputs.Count; } }
	public int BotPlayerCount { get { return KartCount-HumanPlayerCount; } }

}
