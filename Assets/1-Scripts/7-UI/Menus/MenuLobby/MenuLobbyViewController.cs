using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FishNet.Transporting;
using GameKit.Utilities;
using FishNet.Managing.Scened;
using FishNet;
using System;

/// <summary>
/// Communicates with the MenuLobbyController to display whats going on
/// </summary>
public class MenuLobbyViewController : MonoBehaviour
{

    private MenuLobbyController _controller;
    private LobbyManager _lobbyManager;

    [SerializeField]
    private GameObject disconnectedViewContainer;
    [SerializeField]
    private TMP_Text disconnectedStatusText;

    [SerializeField]
    private GameObject connectedViewContainer;
    [SerializeField]
    private GameObject playerNamePlatePrefab;
    [SerializeField]
    private GameObject connectionButtons;
    [SerializeField]
    private TMP_Text lobbyStatusText;  
    [SerializeField]
    private Transform playerListGroup;

    private LobbyData? _currentData;
    /// <summary>
    /// Set when the GameLobby switches to state WAITING_FOR_PLAYER, indicates when the player wait timer will run out.
    /// </summary>
    public float playerWaitTimeout;

    private void Start() 
    {
        _controller = GetComponent<MenuLobbyController>();       
        SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#Start: Script started, scene handle is: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle); 

        UpdateView();
    }

    private void OnDisable() 
    {
        if(_lobbyManager != null)
            _lobbyManager.LobbyUpdated -= LobbyManager_LobbyUpdated;
    }

    private void Update() 
    {
        // Waiting for SceneDelegate/LobbyManager to spawn
        if(_lobbyManager == null && SceneDelegate.Instance != null && SceneDelegate.LobbyManager != null) {
            _lobbyManager = SceneDelegate.LobbyManager;
            SceneDelegate.LobbyManager.LobbyUpdated += LobbyManager_LobbyUpdated;    
            UpdateView();

            SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#Update: Attached lobby manager"); 
        }

        if(_currentData == null)
            return;

        LobbyData currentData = _currentData.Value;

        // Update player timeout text
        if(currentData.state == LobbyState.WAITING_FOR_PLAYERS && playerWaitTimeout != -1) {
            lobbyStatusText.text = $"Waiting for players ({Mathf.RoundToInt(playerWaitTimeout-Time.time)})";
        }
    }

    public void UpdateView() 
    {
        NetworkStateManager nsm = _controller.ConnectedNetworkManager.GetComponent<NetworkStateManager>();
        bool isConnected = _lobbyManager != null && nsm != null && nsm.ClientConnectionState == LocalConnectionState.Started;
        if(isConnected)
            UpdateConnectedView(nsm);
        else
            UpdateDisconnectedView(nsm);
    }

    public void UpdateConnectedView(NetworkStateManager nsm) 
    {
        disconnectedViewContainer.SetActive(false);
        connectedViewContainer.SetActive(true);

        SceneDelegate.SceneDelegateDebug($"MenuLobbyViewController#UpdateView: Updating view (current data has value: {_currentData.HasValue})"); 

        // Menu reset
        lobbyStatusText.text = "-";
        playerListGroup.DestroyChildren();

        if(!_currentData.HasValue)
            return;

        SceneDelegate.SceneDelegateDebug($"MenuLobbyViewController#UpdateView: Player name count {_currentData.Value.players.Count}"); 

        LobbyData lobbyData = _currentData.Value;
        List<PlayerData> players = lobbyData.players;

        // Status text
        switch(lobbyData.state) {
            case LobbyState.WAITING_FOR_PLAYERS:
                lobbyStatusText.text = $"Waiting for players";
                break;
            case LobbyState.MAP_SELECTION:
                lobbyStatusText.text = $"Picking map";
                break;
            case LobbyState.RACING:
                lobbyStatusText.text = "At the track";
                break;
        }

        // Player list
        players.ForEach(playerData => {
            GameObject newNamePlate = Instantiate(playerNamePlatePrefab, playerListGroup);
            newNamePlate.GetComponent<LobbyPlayerNamePlateController>().ShowPlayerData(playerData);
        });

        RectTransform playerListTransform = playerListGroup.gameObject.GetComponent<RectTransform>();
        playerListTransform.sizeDelta = new(playerListTransform.sizeDelta.x, players.Count*118.75f);
    }

    public void UpdateDisconnectedView(NetworkStateManager nsm) 
    {
        disconnectedViewContainer.SetActive(true);
        connectedViewContainer.SetActive(false);

        // Status text
        if(nsm == null)
            disconnectedStatusText.text = "Initializing";
        else
            switch(nsm.ClientConnectionState) {
                case LocalConnectionState.Stopped:
                    disconnectedStatusText.text = "Client not started (Shift+F1)";
                    break;
                case LocalConnectionState.Starting:
                    disconnectedStatusText.text = "Starting connection";
                    break;
                case LocalConnectionState.Started:
                    disconnectedStatusText.text = "Connected";
                    break;
            }
            

    }

    public void LobbyManager_LobbyUpdated(LobbyData newData, LobbyUpdateReason reason) 
    {
        _currentData = newData;
        SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#LobbyManager_LobbyUpdated: Recieved update event"); 

        if(newData.state == LobbyState.WAITING_FOR_PLAYERS)
            playerWaitTimeout = Time.time + (GameLobby.PLAYER_WAIT_TIME-newData.timeInState);
        else
            playerWaitTimeout = -1;

        UpdateView();
    }

}
