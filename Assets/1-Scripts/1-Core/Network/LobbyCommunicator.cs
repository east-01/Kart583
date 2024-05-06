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
public class LobbyCommunicator : NetworkBehaviour
{

    public static float LOBBY_JOIN_REQUEST_TIMEOUT = 10;

    // Things that need to be done:
    // 1. x Communication is started when passing through MenuPlayerController & on Dev settings load
    // 2. x Start/Stop communication Lobby data retainment
    // 3. GameplayManager should create a lobby if loaded into

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
        if(!SceneDelegate.IsReady)
            return;

        if(waitingToStartCommunication) {
            waitingToStartCommunication = false;
            StartCommunication(retryUntilConnected);
        }

        bool timeCondition = Time.time - lastLobbyJoinRequestTime > LOBBY_JOIN_REQUEST_TIMEOUT;
        if(timeCondition && PlayerObjectManager.Instance.PlayerObjectCount > 0 && !InLobby && base.LocalConnection.IsValid) {
            lastLobbyJoinRequestTime = Time.time;

            SceneDelegate.LobbyManager.JoinLobby(base.LocalConnection, PlayerObjectManager.Instance.PlayerOne.data);
        }
    }

    public void StartCommunication(bool retryUntilConnected = true) 
    {
        if(SceneDelegate.Instance == null) {
            waitingToStartCommunication = true;
            this.retryUntilConnected = retryUntilConnected;
            return;
        }

        lobbyData = null;
        initialized = false;

        // Transport configurement and server starting
        if(GameVersion.IsDevelopment || CoreManager.IsLocal)
            CoreManager.NetworkStateManager.UseLocalTransport();
        else
            CoreManager.NetworkStateManager.UseGlobalTransport();

        InstanceFinder.ClientManager.OnRemoteConnectionState += ClientManager_OnClientRemoteConnectionState;
        SceneDelegate.LobbyManager.LobbyUpdated += LobbyManager_LobbyUpdated;

        if(CoreManager.IsLocal)
            CoreManager.NetworkStateManager.StartHost();
        else
            CoreManager.NetworkStateManager.StartClient();
    }

    public void StopCommunication() 
    {
        InstanceFinder.ClientManager.OnRemoteConnectionState -= ClientManager_OnClientRemoteConnectionState;
        SceneDelegate.LobbyManager.LobbyUpdated -= LobbyManager_LobbyUpdated;

        lobbyData = null;
    }

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
