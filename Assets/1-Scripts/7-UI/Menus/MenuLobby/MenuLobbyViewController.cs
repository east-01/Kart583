using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using FishNet.Transporting;
using GameKit.Utilities;
using FishNet.Managing.Scened;
using FishNet;

/// <summary>
/// Communicates with the MenuLobbyController to display whats going on
/// </summary>
public class MenuLobbyViewController : MonoBehaviour
{

    private MenuLobbyController _controller;
    private LobbyManager _lobbyManager;

    [SerializeField]
    private GameObject playerNamePlatePrefab;
    [SerializeField]
    private GameObject connectionButtons;
    [SerializeField]
    private TMP_Text lobbyStatusText;  
    [SerializeField]
    private Transform playerListGroup;

    private LobbyData? _currentData;

    private void Start() 
    {
        _controller = GetComponent<MenuLobbyController>();       
        SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#Start: Script started, scene handle is: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle); 
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
            SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#Update: Attached lobby manager"); 
            
            // if(InstanceFinder.IsClient) {
            //     print("MenuLobbyViewController#Update: Requesting update"); 
            //     // _lobbyManager.RU();
            // }
        }
    }

    public void UpdateView() 
    {
        NetworkStateManager nsm = _controller.ConnectedNetworkManager.GetComponent<NetworkStateManager>();
        if(nsm == null) {
            Debug.LogError("Failed to UpdateView because the NetworkStateManager is null.");
            return;
        }

        SceneDelegate.SceneDelegateDebug($"MenuLobbyViewController#UpdateView: Updating view (current data has value: {_currentData.HasValue})"); 

        // Status text
        string lobbyStatusText = "";
        if(nsm.ServerConnectionState != LocalConnectionState.Stopped)
            lobbyStatusText = "[Server - " + nsm.ServerConnectionState + "]";
        else if(nsm.ClientConnectionState != LocalConnectionState.Stopped)
            lobbyStatusText = "[Client - " + nsm.ClientConnectionState + "]";
        this.lobbyStatusText.text = lobbyStatusText;

        // Menu reset
        playerListGroup.DestroyChildren();

        if(!_currentData.HasValue)
            return;

        SceneDelegate.SceneDelegateDebug($"MenuLobbyViewController#UpdateView: Player name count {_currentData.Value.playerNames.Count}"); 

        LobbyData lobbyData = _currentData.Value;

        // Player list
        lobbyData.playerNames.ForEach(name => {
            GameObject newNamePlate = Instantiate(playerNamePlatePrefab, playerListGroup);
            newNamePlate.GetComponent<TMP_Text>().text = name;
        });
    }

    public void LobbyManager_LobbyUpdated(LobbyData newData) 
    {
        _currentData = newData;
        SceneDelegate.SceneDelegateDebug("MenuLobbyViewController#LobbyManager_LobbyUpdated: Recieved update event"); 
        UpdateView();
    }

}
