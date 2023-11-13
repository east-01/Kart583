using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{

	private List<PlayerInput> players = new List<PlayerInput>();

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

	void PlayerJoined(PlayerInput player)
	{

		if(players.Count == 0) {
			Camera.main.GetComponent<AudioListener>().enabled = false;
			Camera.main.enabled = false; 
		} else { // Player one's camera always gets the audio listener
			player.camera.GetComponent<AudioListener>().enabled = false;
		}

		players.Add(player);
	}

	void PlayerLeft(PlayerInput player) 
	{ 
		
	}

}
