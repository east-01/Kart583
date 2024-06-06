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

    /// <summary>
    /// Set when the GameLobby switches to state WAITING_FOR_PLAYER, indicates when the player wait timer will run out.
    /// </summary>
    public float playerWaitTimeout;

    private void Start() 
    {
        _controller = GetComponent<MenuLobbyController>();       
        BLog.Log("MenuLobbyViewController#Start: Script started, scene handle is: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle, LogChannel.SceneDelegate, 0); 

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
        if(_lobbyManager == null && SceneController.Instance != null && NetSceneController.IsReady && NetSceneController.LobbyManager != null) {
            _lobbyManager = NetSceneController.LobbyManager;
            NetSceneController.LobbyManager.LobbyUpdated += LobbyManager_LobbyUpdated;    
            UpdateView();

            BLog.Log("MenuLobbyViewController#Update: Attached lobby manager", LogChannel.SceneDelegate, 0); 
        }

        if(!CoreManager.LobbyCommunicator.LobbyData.HasValue)
            return;
        LobbyData currentData = CoreManager.LobbyCommunicator.LobbyData.Value;

        // Update player timeout text
        if(currentData.state == LobbyState.WAITING_FOR_PLAYERS && playerWaitTimeout != -1) {
            lobbyStatusText.text = $"Waiting for players ({Mathf.RoundToInt(playerWaitTimeout-Time.time)})";
        }
    }

#region Updating view
    public void UpdateView() 
    {
        if(_controller.ConnectedNetworkManager == null)
            return;

        NetworkStateManager nsm = _controller.ConnectedNetworkManager.GetComponent<NetworkStateManager>();
        bool isConnected = _lobbyManager != null && nsm != null && nsm.ClientConnectionState == LocalConnectionState.Started;
        BLog.Highlight("updating view, is connected: " + isConnected);
        if(isConnected)
            UpdateConnectedView(nsm);
        else
            UpdateDisconnectedView(nsm);
    }

    public void UpdateConnectedView(NetworkStateManager nsm) 
    {
        connectedViewContainer.SetActive(true);
        disconnectedViewContainer.SetActive(false);

        BLog.Log($"MenuLobbyViewController#UpdateView: Updating view (current data has value: {CoreManager.LobbyCommunicator.LobbyData.HasValue})", LogChannel.SceneDelegate, 0); 

        // Menu reset
        lobbyStatusText.text = "-";
        playerListGroup.DestroyChildren();

        if(!CoreManager.LobbyCommunicator.LobbyData.HasValue)
            return;
        LobbyData lobbyData = CoreManager.LobbyCommunicator.LobbyData.Value;

        BLog.Log($"MenuLobbyViewController#UpdateView: Player name count {lobbyData.players.Count}", LogChannel.SceneDelegate, 0);

        List<PlayerData> players = lobbyData.players;

        // Status text
        switch(lobbyData.state) {
            case LobbyState.WAITING_FOR_PLAYERS:
                lobbyStatusText.text = $"Waiting for players";
                break;
            case LobbyState.MAP_SELECTION:
                lobbyStatusText.text = $"Picking map";

                _controller.OpenSubMenu(MenuLobbyController.SUB_MENU_MAP_SELECT);
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
        connectedViewContainer.SetActive(false);
        disconnectedViewContainer.SetActive(true);

        // Status text
        if(nsm == null)
            disconnectedStatusText.text = "Initializing";
        else
            switch(nsm.ClientConnectionState) {
                case LocalConnectionState.Stopped:
                    disconnectedStatusText.text = $"No connection. Retrying in {_controller.retryTimer}s";
                    break;
                case LocalConnectionState.Starting:
                    disconnectedStatusText.text = "Starting connection";
                    break;
                case LocalConnectionState.Started:
                    disconnectedStatusText.text = "Connected";
                    break;
            }
    }
#endregion

    public void LobbyManager_LobbyUpdated(LobbyData newData, LobbyUpdateReason reason) 
    {
        BLog.Log("MenuLobbyViewController#LobbyManager_LobbyUpdated: Recieved update event", LogChannel.SceneDelegate, 0); 

        if(newData.state == LobbyState.WAITING_FOR_PLAYERS)
            playerWaitTimeout = Time.time + (GameLobby.PLAYER_WAIT_TIME-newData.timeInState);
        else
            playerWaitTimeout = -1;

        UpdateView();
    }

}
