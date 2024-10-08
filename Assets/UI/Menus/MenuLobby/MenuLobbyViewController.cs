using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FishNet.Transporting;
using FishNet.Managing.Scened;
using FishNet;
using System;
using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using GameKit.Dependencies.Utilities;

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

        UpdateView();
    }

    private void OnEnable() 
    {
        LobbyCommunicator.Instance.LobbyUpdatedEvent += LobbyCommunicator_LobbyUpdatedEvent; 
        UpdateView();
    }

    private void OnDisable() => LobbyCommunicator.Instance.LobbyUpdatedEvent -= LobbyCommunicator_LobbyUpdatedEvent;

    private void Update() 
    {
        if(!LobbyCommunicator.Instance.LobbyData.HasValue)
            return;

        LobbyData currentData = LobbyCommunicator.Instance.LobbyData.Value;

        // Update player timeout
        if(currentData.stateTypeString == nameof(WaitingForPlayersState)) {
            if(playerWaitTimeLeft > 0) {
                playerWaitTimeLeft -= Time.deltaTime;
                lobbyStatusText.text = $"Waiting for players ({Mathf.FloorToInt(playerWaitTimeLeft)})";
            } else if((KartLobby.PLAYER_WAIT_TIME - currentData.timeInState) > 0) {
                playerWaitTimeLeft = KartLobby.PLAYER_WAIT_TIME - currentData.timeInState;
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

        NetworkController nc = NetworkController.Instance;
        bool isConnected = nc != null && LobbyManager.Instance != null && nc.ClientConnectionState == LocalConnectionState.Started && LobbyCommunicator.Instance.LobbyData.HasValue;
        if(isConnected)
            UpdateConnectedView(nc);
        else
            UpdateDisconnectedView(nc);
    }

    public void UpdateConnectedView(NetworkController nc) 
    {
        connectedViewContainer.SetActive(true);
        disconnectedViewContainer.SetActive(false);

        BLog.Log($"MenuLobbyViewController#UpdateView: Updating view (current data has value: {LobbyCommunicator.Instance.LobbyData.HasValue})", SceneController.Instance.logSettings, 0); 

        // Menu reset
        lobbyStatusText.text = "-";
        playerListGroup.DestroyChildren();

        if(!LobbyCommunicator.Instance.LobbyData.HasValue)
            return;
        LobbyData lobbyData = LobbyCommunicator.Instance.LobbyData.Value;

        BLog.Log($"MenuLobbyViewController#UpdateView: Player name count {lobbyData.playerUIDs.Count}", SceneController.Instance.logSettings, 0);

        List<string> playerUIDs = lobbyData.playerUIDs;

        // Status text
        switch(lobbyData.stateTypeString) {
            case nameof(WaitingForPlayersState):
                lobbyStatusText.text = $"Waiting for players";
                break;
            case nameof(MapSelectionState):
                lobbyStatusText.text = $"Picking map";

                if(CoreManager.IsLocal)
                    _controller.OpenSubMenu(MenuLobbyController.SUB_MENU_MAP_SELECT);
                break;
            case nameof(RacingState):
                lobbyStatusText.text = "At the track";
                break;
        }

        // Player list
        playerUIDs.ForEach(playerUID => {
            GameObject newNamePlate = Instantiate(playerNamePlatePrefab, playerListGroup);
            newNamePlate.GetComponent<LobbyPlayerNamePlateController>().ShowPlayerData(playerUID);
        });

        RectTransform playerListTransform = playerListGroup.gameObject.GetComponent<RectTransform>();
        playerListTransform.sizeDelta = new(playerListTransform.sizeDelta.x, playerUIDs.Count*118.75f);
    }

    public void UpdateDisconnectedView(NetworkController nc) 
    {
        connectedViewContainer.SetActive(false);
        disconnectedViewContainer.SetActive(true);

        // Status text
        if(nc == null)
            disconnectedStatusText.text = "Initializing";
        else
            switch(nc.ClientConnectionState) {
                case LocalConnectionState.Stopped:
                    disconnectedStatusText.text = $"No connection.";
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
        BLog.Log("MenuLobbyViewController#LobbyManager_LobbyUpdated: Recieved update event", SceneController.Instance.logSettings, 0); 

        if(newData.stateTypeString != nameof(WaitingForPlayersState))
            playerWaitTimeLeft = -1;

        UpdateView();
    }

}
