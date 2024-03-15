using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The player object manager is mainly responsible for LOCAL player input management.
/// We don't want to network this because, if we did, we'd be doing splitscreen for all
///   players on server when splitscreen should only be for players on the same machine.
/// </summary>
public class PlayerObjectManager : MonoBehaviour
{

    public static PlayerObjectManager Instance { get; private set; }

    public delegate void PlayerObjectJoinHandler(PlayerObject newPlayer);
    public event PlayerObjectJoinHandler PlayerObjectJoinedEvent;

    [SerializeField]
    private GameObject inputPromptCanvas;

    private PlayerInputManager playerInputManager;
    private List<PlayerObject> playerObjects;

    private void Awake()
    {
        // We'll use singleton pattern for this
        if(Instance != null) {
            Destroy(gameObject);
            Debug.Log($"Destroyed newly spawned PlayerObjectManager since singleton Instance already exists.");
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        PlayerObjectJoinedEvent?.Invoke(obj);
    }

    public void PlayerLeft(PlayerInput input) 
    {

    }

    public void PromptForInput() 
    { 
        inputPromptCanvas.SetActive(true); 
        playerInputManager.EnableJoining();
    }

    public void ClearInputPrompt() 
    { 
        inputPromptCanvas.SetActive(false); 
        playerInputManager.DisableJoining();
    }

    public bool InputPromptActive { get { return inputPromptCanvas.activeSelf; } }

    public PlayerInputManager GetPlayerInputManager() { return playerInputManager; }
    public List<PlayerObject> GetPlayerObjects() { return playerObjects; }

    public int PlayerObjectCount { get { return playerObjects.Count; } }

}

public class PlayerObject 
{
    public PlayerInput input;
    public PlayerData data;

    public int PlayerIndex { get { return input.playerIndex; } }
}