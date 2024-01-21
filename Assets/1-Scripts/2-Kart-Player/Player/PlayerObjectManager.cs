using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerObjectManager : MonoBehaviour
{

    public static PlayerObjectManager Instance { get; private set; }

    private PlayerInputManager playerInputManager;
    private List<PlayerObject> playerObjects;

    private void Awake()
    {
        // We'll use singleton pattern for this
        if(Instance != null) 
            throw new InvalidOperationException("Singleton pattern broken!");
        else {
            Instance = this;
            DontDestroyOnLoad(Instance);
        }

        playerInputManager = GetComponent<PlayerInputManager>();
        playerObjects = new();
    }

	void OnEnable()
    {
		playerInputManager.onPlayerJoined += PlayerJoined;
        playerInputManager.onPlayerLeft += PlayerLeft;
    }

	private void OnDisable()
	{
		playerInputManager.onPlayerJoined -= PlayerJoined;
        playerInputManager.onPlayerLeft -= PlayerLeft;		
	}

    public void PlayerJoined(PlayerInput input) 
    {

        input.gameObject.transform.SetParent(transform);

        PlayerObject obj = new();
        obj.input = input;
        obj.data = new() {
            name = "Player " + (obj.PlayerIndex + 1)
        };

        playerObjects.Add(obj);

        if(SceneManager.GetActiveScene().name == SceneNames.MENU_PLAYER) {
            GameObject canv = GameObject.Find("MenuCanvas");
            if(canv == null) 
                throw new InvalidOperationException("Failed to find MenuCanvas.");

            canv.GetComponent<MenuPlayerController>().HandleJoin(obj);
        } else if(GameplayManager.Instance != null) {
            GameplayManager.PlayerManager.SpawnPlayer(obj);
        } else
            Debug.LogError("Failed to handle a player input");

    }

    public void PlayerLeft(PlayerInput input) 
    {

    }

    public PlayerInputManager GetPlayerInputManager() { return playerInputManager; }
    public List<PlayerObject> GetPlayerObjects() { return playerObjects; }

}

public class PlayerObject 
{
    public PlayerInput input;
    public PlayerData data;

    public int PlayerIndex { get { return input.playerIndex; } }
}