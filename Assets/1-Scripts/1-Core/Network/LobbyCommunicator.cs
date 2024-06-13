using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// The LobbyCommunicator performs CLIENT SIDE tasks relating to connecting to
///   the GameLobby.
/// It will interface with the NetworkStateManager to start connections.
/// </summary>
public class LobbyCommunicator : MonoBehaviour
{

    public static float LOBBY_JOIN_REQUEST_TIMEOUT = 10;

    private NetworkStateManager networkStateManager;

    private LobbyData? lobbyData;
    private bool initialized = false;

    private bool waitingToStartCommunication = false;
    private bool retryUntilConnected = false;

    private float lastLobbyJoinRequestTime = float.MinValue;

    private void Awake() 
    {
        networkStateManager = CoreManager.NetworkStateManager;
    }

    private void Update() 
    {
        if(!NetSceneController.IsReady)
            return;
        if(!CoreManager.HasLocalConnection)
            return;

        if(waitingToStartCommunication) {
            waitingToStartCommunication = false;
            StartCommunication(retryUntilConnected);
        }

        bool timeCondition = Time.time - lastLobbyJoinRequestTime > LOBBY_JOIN_REQUEST_TIMEOUT;
        if(timeCondition && PlayerObjectManager.Instance.PlayerObjectCount > 0 && !InLobby && CoreManager.LocalConnection.IsValid) {
            lastLobbyJoinRequestTime = Time.time;

            NetSceneController.LobbyManager.JoinLobby(CoreManager.LocalConnection, PlayerObjectManager.Instance.Players);
        }
    }

    public void StartCommunication(bool retryUntilConnected = true) 
    {
        if(SceneController.Instance == null) {
            waitingToStartCommunication = true;
            this.retryUntilConnected = retryUntilConnected;
            return;
        }

        lobbyData = null;
        initialized = false;

        // Transport configurement and server starting
        if(DevSettings.IsDevelopment() || CoreManager.IsLocal)
            CoreManager.NetworkStateManager.UseLocalTransport();
        else
            CoreManager.NetworkStateManager.UseGlobalTransport();

        InstanceFinder.ClientManager.OnRemoteConnectionState += ClientManager_OnClientRemoteConnectionState;

        if(CoreManager.IsLocal)
            CoreManager.NetworkStateManager.StartHost();
        else
            CoreManager.NetworkStateManager.StartClient();
    }

    public void StopCommunication() 
    {
        if(CoreManager.HasLocalConnection)
            NetSceneController.LobbyManager.LeaveLobby(CoreManager.LocalConnection);
        else
            Debug.LogWarning("Stopping communication without a local connection. This shouldn't happen.");

        InstanceFinder.ClientManager.OnRemoteConnectionState -= ClientManager_OnClientRemoteConnectionState;

        if(CoreManager.IsLocal)
            CoreManager.NetworkStateManager.StopHost();
        else
            CoreManager.NetworkStateManager.StopClient();

        lobbyData = null;
    }

    public void RegisterLobbyManager() { NetSceneController.LobbyManager.LobbyUpdated += LobbyManager_LobbyUpdated; }
    public void DeregisterLobbyManager() { NetSceneController.LobbyManager.LobbyUpdated -= LobbyManager_LobbyUpdated; }

    private void ClientManager_OnClientRemoteConnectionState(RemoteConnectionStateArgs args) 
    {
        if(args.ConnectionState == RemoteConnectionState.Stopped) {
            StopCommunication();
        }
    }

    private void LobbyManager_LobbyUpdated(LobbyData newData, LobbyUpdateReason reason)
    {
        if(!initialized) {
            initialized = true;
        }

        lobbyData = newData;
    }

    public LobbyData? LobbyData { get { return lobbyData; } }
    public bool InLobby { get { return initialized && lobbyData.HasValue; } }

}
