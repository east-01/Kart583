using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        obj.data = new();
        obj.data.name = "Player " + (obj.PlayerIndex+1);

        playerObjects.Add(obj);

        GameObject canv = GameObject.Find("PlayerMenuCanvas");
        if(canv == null) throw new InvalidOperationException("Failed to find player menu canvas.");

        canv.GetComponent<PlayerMenuController>().HandleJoin(obj);

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