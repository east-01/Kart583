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

	private void OnEnable()
    {
		playerInputManager.onPlayerJoined += PlayerJoined;
        playerInputManager.onPlayerLeft += PlayerLeft;
    }

	private void OnDisable()
	{
		playerInputManager.onPlayerJoined -= PlayerJoined;
        playerInputManager.onPlayerLeft -= PlayerLeft;		
	}

    private void Update() 
    {
        // We always need an input for player 1
        if(playerObjects.Count == 0 && !InputPromptActive) {
            PromptForInput();
        } else if(playerObjects.Count > 0 && InputPromptActive) {
            ClearInputPrompt();
        }
    }

    public void PlayerJoined(PlayerInput input) 
    {
        input.gameObject.transform.SetParent(transform);

        PlayerObject obj = new();
        obj.input = input;
        
        if(input.playerIndex == 0 && PlayerPrefs.HasKey(PlayerData.PLAYER_1_DATA))
            obj.data = PlayerData.LoadFromPlayerPrefs(PlayerData.PLAYER_1_DATA).Value;
        else
            obj.data = new() {
                uuid = Guid.NewGuid().ToString(),
                name = ""/*"Player " + (obj.PlayerIndex + 1)*/
            };

        playerObjects.Add(obj);
        PlayerObjectJoinedEvent?.Invoke(obj);

        PlayerOne.input.neverAutoSwitchControlSchemes = PlayerObjectCount > 1;
    }

    public void PlayerLeft(PlayerInput input) 
    {

    }

    public void RemovePlayer(PlayerObject obj) 
    {
        Debug.LogWarning("TODO: Implement PlayerObjectManager#RemovePlayer");
    }

    /// <summary>
    /// Prompt the user for input so that we have a player one.
    /// We don't want to show the input prompt canvas because this is the only
    ///   place where there isn't a player one right when the scene opens.
    /// </summary>
    public void PromptForInput() 
    { 
        if(SceneManager.GetActiveScene().name != SceneNames.MENU_TITLE)
            inputPromptCanvas.SetActive(true); 
        playerInputManager.EnableJoining();
    }

    public void ClearInputPrompt() 
    { 
        inputPromptCanvas.SetActive(false); 
        playerInputManager.DisableJoining();
    }

    public bool InputPromptActive { get { 
        // Weird logic here because we don't want to show the input prompt on the title screen. Explained in PromptForInput
        if(SceneManager.GetActiveScene().name == SceneNames.MENU_TITLE)
            return playerInputManager.joiningEnabled;
        else
            return inputPromptCanvas.activeSelf; 
    } }

    public PlayerInputManager GetPlayerInputManager() { return playerInputManager; }
    public List<PlayerObject> GetPlayerObjects() { return playerObjects; }
    public PlayerObject PlayerOne { get { 
        if(playerObjects.Count == 0)
            return null;
        return playerObjects[0]; 
    } }
    public List<PlayerData> Players { get {
        List<PlayerData> pds = new();
        foreach(PlayerObject po in playerObjects) {
            pds.Add(po.data);
        }
        return pds;
    } }

    public int PlayerObjectCount { get { return playerObjects.Count; } }

}

public class PlayerObject 
{
    public PlayerInput input;
    public PlayerData data;
    public POIGDelegate poigDelegate;

    public int PlayerIndex { get { return input.playerIndex; } }
}