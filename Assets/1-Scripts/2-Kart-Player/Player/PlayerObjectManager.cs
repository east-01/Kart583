using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using TMPro;
using Unity.VisualScripting;
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

#region Editor fields
    [SerializeField]
    private GameObject inputPromptCanvas;
    [SerializeField]
    private GameObject inputPromptPanel;
    [SerializeField]
    private GameObject deviceMissingPanel;

    [SerializeField]
    private List<string> autoControlSchemeSwitchBlockingScenes;
    [SerializeField]
    private List<string> inputPromptBlockingScenes;
#endregion

#region Runtime fields
    public PlayerInputManager PlayerInputManager { get; private set; }
    public List<PlayerObject> PlayerObjects { get; private set; }
    private List<PlayerObject> playerObjectsMissingDevices = new();

    public int PlayerObjectCount => PlayerObjects.Count;
    public PlayerObject PlayerOne => PlayerObjects.Count > 0 ? PlayerObjects[0] : null;
    public bool CanPlayerOneAutoSwitch => PlayerObjectCount == 1 && !autoControlSchemeSwitchBlockingScenes.Contains(SceneManager.GetActiveScene().name);
#endregion

#region Events
    public delegate void PlayerObjectJoinHandler(PlayerObject newPlayer);
    public event PlayerObjectJoinHandler PlayerObjectJoinedEvent;

    public delegate void PlayerObjectLeftHandler(PlayerObject player);
    public event PlayerObjectLeftHandler PlayerObjectLeftEvent;
#endregion

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

        PlayerInputManager = GetComponent<PlayerInputManager>();
        PlayerObjects = new();
    }

	private void OnEnable()
    {
		PlayerInputManager.onPlayerJoined += AddPlayer;
        PlayerInputManager.onPlayerLeft += RemovePlayer;
    }

	private void OnDisable()
	{
		PlayerInputManager.onPlayerJoined -= AddPlayer;
        PlayerInputManager.onPlayerLeft -= RemovePlayer;		
	}

    private void Update() 
    {
        // BLog.Highlight($"pim allowed joining: {PlayerInputManager.joiningEnabled}");

        // We always need an input for player 1
        if(PlayerObjects.Count == 0 && !InputPromptActive) {
            PromptForInput();
        } else if(PlayerObjects.Count > 0 && InputPromptActive) {
            ClearInputPrompt();
        }

        if(PlayerOne != null) {
            // BLog.Highlight($"P1 can auto switch: {CanPlayerOneAutoSwitch}");
            PlayerOne.input.neverAutoSwitchControlSchemes = !CanPlayerOneAutoSwitch;
        }
    }

#region Add/Remove player
    public void AddPlayer(PlayerInput input) 
    {
        input.gameObject.transform.SetParent(transform);

        PlayerObject obj = new();
        obj.input = input;
        
        input.onDeviceLost += PlayerInput_DeviceLost;
        input.onDeviceRegained += PlayerInput_DeviceRegained;

        // Load data
        if(input.playerIndex == 0 && PlayerPrefs.HasKey(PlayerData.PLAYER_1_DATA))
            obj.data = PlayerData.LoadFromPlayerPrefs(PlayerData.PLAYER_1_DATA).Value;
        else
            obj.data = new() {
                uuid = Guid.NewGuid().ToString(),
                name = ""/*"Player " + (obj.PlayerIndex + 1)*/
            };

        PlayerObjects.Add(obj);
        PlayerObjectJoinedEvent?.Invoke(obj);
    }

    /// <summary>
    /// Remove a player from the PlayerObjectManager
    /// </summary>
    /// <param name="obj"></param>
    public void RemovePlayer(PlayerInput input) 
    {
        PlayerObject obj = FindPlayerObject(input);

        input.onDeviceLost += PlayerInput_DeviceLost;
        input.onDeviceRegained += PlayerInput_DeviceRegained;

        PlayerObjects.Remove(obj);
        PlayerObjectLeftEvent?.Invoke(obj);

        Destroy(input.gameObject);
    }
    /// <summary>
    /// Event call for removing player, shortcuts to the original RemovePlayer call 
    ///   using FindPlayerObject(PlayerInput).
    /// </summary>
    /// <param name="input">The PlayerInput to remove, more specifically, the PlayerObject 
    ///   that owns that PlayerInput</param>
    public void RemovePlayer(PlayerObject obj) => RemovePlayer(obj.input);
#endregion

#region Input prompts
    /// <summary>
    /// Prompt the user for input so that we have a player one.
    /// We don't want to show the input prompt canvas because this is the only
    ///   place where there isn't a player one right when the scene opens.
    /// </summary>
    public void PromptForInput() 
    { 
        if(SceneManager.GetActiveScene().name != SceneNames.MENU_TITLE)
            inputPromptPanel.SetActive(true); 
        PlayerInputManager.EnableJoining();
    }

    public void ClearInputPrompt() 
    { 
        inputPromptPanel.SetActive(false); 
        PlayerInputManager.DisableJoining();
    }

    public bool InputPromptActive { get { 
        // Weird logic here because we don't want to show the input prompt on the title screen. Explained in PromptForInput
        if(inputPromptBlockingScenes.Contains(SceneManager.GetActiveScene().name))
            return PlayerInputManager.joiningEnabled;
        else
            return inputPromptCanvas.activeSelf; 
    } }

    private void PlayerInput_DeviceLost(PlayerInput input)
    {
        PlayerObject obj = FindPlayerObject(input);
        if(!playerObjectsMissingDevices.Contains(obj))
            playerObjectsMissingDevices.Add(obj);

        UpdateMissingDevicesPrompt();
    }
    
    private void PlayerInput_DeviceRegained(PlayerInput input)
    {
        PlayerObject obj = FindPlayerObject(input);
        if(playerObjectsMissingDevices.Contains(obj))
            playerObjectsMissingDevices.Remove(obj);

        UpdateMissingDevicesPrompt();
    }

    public void UpdateMissingDevicesPrompt() 
    {
        string nameList = "";
        playerObjectsMissingDevices.ForEach(obj => nameList += obj.PlayerName + ", ");
        nameList = nameList[..^2];

        deviceMissingPanel.GetComponent<TMP_Text>().text = $"Missing input: {nameList}";
        deviceMissingPanel.SetActive(playerObjectsMissingDevices.Count > 0);
    }
#endregion

    public PlayerObject FindPlayerObject(PlayerInput input) 
    {
        foreach(PlayerObject obj in PlayerObjects) {
            if(obj.PlayerIndex == input.playerIndex)
                return obj;
        }
        return null;
    }

    public List<PlayerData> Players { get {
        List<PlayerData> pds = new();
        foreach(PlayerObject po in PlayerObjects) {
            pds.Add(po.data);
        }
        return pds;
    } }


}

public class PlayerObject 
{
    public PlayerInput input;
    public PlayerData data;
    public POIGDelegate poigDelegate;

    public int PlayerIndex => input.playerIndex;
    public string PlayerName => (data.name == null || data.name.Length == 0) ? "Player " + PlayerIndex : data.name;
}