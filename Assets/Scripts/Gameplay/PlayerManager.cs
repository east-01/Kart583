using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{

	public GameObject kartBotPrefab;

	/** This could include bots as well */
	private List<GameObject> playerObjects = new List<GameObject>();
	private List<PlayerInput> playerInputs = new List<PlayerInput>();

	private PlayerInputManager controls;

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

    }

	void AddPlayer(GameObject playerObject) 
	{	
		if(playerObjects.Count >= 8) {
			Debug.LogError("Tried to add a new player even though there is already 8 (or more) players.");
			Destroy(playerObject);
			return;
		}

		playerObject.transform.position = GameObject.Find("SpawnPositions").transform.GetChild(playerObjects.Count).position;

		GameObject sp = GameObject.Find("SpawnPositions");
		Vector3 forward = new Vector3(1, 0, 0);
		if(sp != null && sp.GetComponent<SpawnPositions>() != null) {
			playerObject.transform.forward = sp.GetComponent<SpawnPositions>().spawnForward;
			playerObject.GetComponent<KartController>().KartForward = sp.GetComponent<SpawnPositions>().spawnForward;
		}

		playerObjects.Add(playerObject);
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
		AddPlayer(player.gameObject);
	}

	void PlayerLeft(PlayerInput player) 
	{ 
		
	}

	public void SpawnBot() 
	{
		AddPlayer(GameObject.Instantiate(kartBotPrefab));
	}

	public int PlayerCount { get { return playerObjects.Count; } }

}
