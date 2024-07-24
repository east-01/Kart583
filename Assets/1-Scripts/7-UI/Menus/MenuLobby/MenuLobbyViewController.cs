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
    public float playerWaitTimeLeft;

    private void Start() 
    {
        _controller = GetComponent<MenuLobbyController>();       
        BLog.Log("MenuLobbyViewController#Start: Script started, scene handle is: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle, LogChannel.SceneDelegate, 0); 

        UpdateView();
    }

    private void OnEnable() 
    {
        CoreManager.LobbyCommunicator.LobbyUpdatedEvent += LobbyCommunicator_LobbyUpdatedEvent; 
        UpdateView();
    }

    private void OnDisable() => CoreManager.LobbyCommunicator.LobbyUpdatedEvent -= LobbyCommunicator_LobbyUpdatedEvent;

    private void Update() 
    {
        if(!CoreManager.LobbyCommunicator.LobbyData.HasValue)
            return;

        LobbyData currentData = CoreManager.LobbyCommunicator.LobbyData.Value;

        // Update player timeout
        if(currentData.state == LobbyState.WAITING_FOR_PLAYERS) {
            if(playerWaitTimeLeft > 0) {
                playerWaitTimeLeft -= Time.deltaTime;
                lobbyStatusText.text = $"Waiting for players ({Mathf.FloorToInt(playerWaitTimeLeft)})";
            } else if((GameLobby.PLAYER_WAIT_TIME - currentData.timeInState) > 0) {
                playerWaitTimeLeft = GameLobby.PLAYER_WAIT_TIME - currentData.timeInState;
            } else {
                playerWaitTimeLeft = -1;
            }
        }
    }

#region Updating view
    public void UpdateView() 
    {
        if(_controller == null || _controller.ConnectedNetworkManager == null)
            return;

        NetworkStateManager nsm = _controller.ConnectedNetworkManager.GetComponent<NetworkStateManager>();
        bool isConnected = NetSceneController.LobbyManager != null && nsm != null && nsm.ClientConnectionState == LocalConnectionState.Started && CoreManager.LobbyCommunicator.LobbyData.HasValue;
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

                if(CoreManager.IsLocal)
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
        else if(!CoreManager.LobbyCommunicator.LobbyData.HasValue)
            disconnectedStatusText.text = "No lobby data";
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

    public void LobbyCommunicator_LobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason) 
    {
        BLog.Log("MenuLobbyViewController#LobbyManager_LobbyUpdated: Recieved update event", LogChannel.SceneDelegate, 0); 

        if(newData.state != LobbyState.WAITING_FOR_PLAYERS)
            playerWaitTimeLeft = -1;

        UpdateView();
    }

}
