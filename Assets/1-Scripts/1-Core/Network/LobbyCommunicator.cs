using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

/// <summary>
/// The LobbyCommunicator is the client side of the lobby system. It has two 
///   major functions:
/// 1. Initiating the connection to connect to a lobby (via the NetworkStateManager)
/// 2. Acting as a delegate for the LobbyManager, see the reasoning for this in the
///      LobbyManager summary.
/// </summary>
public class LobbyCommunicator : MonoBehaviour
{

    private bool waitingToStartCommunication = false;
    private bool retryUntilConnected = false;

    public string LobbyID { get; private set; }
    public LobbyData? LobbyData { get; private set; }

#region Events
    /// <summary>
    /// Event call for when the client joins a lobby.
    /// Invoked at the end of LobbyManager#AddToLobby
    /// </summary>
    /// <param name="lobbyID">Joined lobby ID</param>
    /// <param name="data">Initial LobbyData</param>
    public delegate void LobbyJoinedHandler(string lobbyID, LobbyData data);
    public event LobbyJoinedHandler LobbyJoinedEvent;
    public void DoNotUse_InvokeLobbyJoinedEvent(string lobbyID, LobbyData data) => LobbyJoinedEvent?.Invoke(lobbyID, data); // please beat me up for this i dont know how to make it better though
    /// <summary>
    /// Event call for when the client leaves a lobby. The lobby leave reason is also provided 
    ///   in case of non-standard lobby exit (ban or server shutdown).
    /// </summary>
    /// <param name="lobbyID">Left lobby ID</param>
    /// <param name="reason">The reason why the client left</param>
    public delegate void LobbyLeftHandler(string lobbyID, string reason);
    public event LobbyLeftHandler LobbyLeftEvent;
    public void DoNotUse_InvokeLobbyLeftEvent(string lobbyID, string reason) => LobbyLeftEvent?.Invoke(lobbyID, reason);
    /// <summary>
    /// Event call for when the server issues a message. 
    /// </summary>
    /// <param name="lobbyID">Message lobby ID</param>
    /// <param name="message">The message that the lobby sent</param>
    public delegate void LobbyMessageHandler(string lobbyID, NetworkConnection sender, LobbyMessageType type, string message);
    public event LobbyMessageHandler LobbyMessageEvent;
    public void DoNotUse_InvokeLobbyMessageEvent(string lobbyID, NetworkConnection sender, LobbyMessageType type, string message) => LobbyMessageEvent?.Invoke(lobbyID, sender, type, message);
    /// <summary>
    /// Event call for when the lobby is updated, reason for update is also provided.
    /// </summary>
    /// <param name="lobbyID">Updated lobby ID</param>
    /// <param name="newData">New LobbyData</param>
    /// <param name="reason">The reason why the lobby updated</param>
    public delegate void LobbyUpdateHandler(string lobbyID, LobbyData newData, LobbyUpdateReason reason);
    public event LobbyUpdateHandler LobbyUpdatedEvent;
    public void DoNotUse_InvokeLobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason) => LobbyUpdatedEvent?.Invoke(lobbyID, newData, reason);
#endregion

    private void OnEnable() 
    { 
        LobbyJoinedEvent += LobbyCommunicator_LobbyJoinedEvent;
        LobbyLeftEvent += LobbyCommunicator_LobbyLeftEvent;
        LobbyMessageEvent += LobbyCommunicator_LobbyMessageEvent;
        LobbyUpdatedEvent += LobbyCommunicator_LobbyUpdatedEvent;
    }

    private void OnDisable() 
    {
        LobbyJoinedEvent -= LobbyCommunicator_LobbyJoinedEvent;
        LobbyLeftEvent -= LobbyCommunicator_LobbyLeftEvent;
        LobbyMessageEvent -= LobbyCommunicator_LobbyMessageEvent;
        LobbyUpdatedEvent -= LobbyCommunicator_LobbyUpdatedEvent;
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
    }

#region Start/Stop communication
    public void StartCommunication(bool retryUntilConnected = true) 
    {
        if(SceneController.Instance == null) {
            waitingToStartCommunication = true;
            this.retryUntilConnected = retryUntilConnected;
            return;
        }

        BLog.Log("Starting communication.", LogChannel.LobbyCommunicator);

        LobbyID = null;
        LobbyData = null;

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
        BLog.Log("Stopping communication.", LogChannel.LobbyCommunicator);
        if(CoreManager.LobbyCommunicator.LobbyID != null)
            NetSceneController.LobbyManager.RemoveFromLobby(CoreManager.LocalConnection, "Client stopped communication.");
        else
            Debug.LogWarning("Stopping communication without a local connection. This shouldn't happen.");

        InstanceFinder.ClientManager.OnRemoteConnectionState -= ClientManager_OnClientRemoteConnectionState;

        if(CoreManager.IsLocal)
            CoreManager.NetworkStateManager.StopHost();
        else
            CoreManager.NetworkStateManager.StopClient();

        LobbyID = null;
        LobbyData = null;
    }
#endregion

#region Event handlers
    private void LobbyCommunicator_LobbyJoinedEvent(string lobbyID, LobbyData initialData) 
    {
        if(LobbyID != null) {
            Debug.LogWarning($"Recieved LobbyJoinEvent when we're already in a lobby (Existing LobbyID is \"{LobbyID}\")");
            return;
        }

        LobbyID = lobbyID;
        LobbyData = initialData;
        BLog.Log($"Joined lobby \"{lobbyID}\"", LogChannel.LobbyCommunicator, 0);
    }

    private void LobbyCommunicator_LobbyLeftEvent(string lobbyID, string reason) 
    {
        if(LobbyID != lobbyID) {
            Debug.LogError($"Recieved LobbyLeftEvent when lobbyID's do not match. Current: \"{LobbyID}\" Incoming: \"{lobbyID}\"");
            return;
        }

        LobbyID = null;
        LobbyData = null;
        BLog.Log($"Left lobby \"{lobbyID}\"", LogChannel.LobbyCommunicator, 0);
    }

    private void LobbyCommunicator_LobbyMessageEvent(string lobbyID, NetworkConnection sender, LobbyMessageType type, string message) 
    {
        // We don't check if ID matches here because LME_CMD_FORCE_DISCONNECT messages do not contain lobbyID or sender.
        if(type == LobbyMessageType.ACTION && message.StartsWith(LobbyManager.LME_CMD_FORCE_DISCONNECT)) {
            StopCommunication();
        }
    }

    private void LobbyCommunicator_LobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason) 
    {
        if(LobbyID != lobbyID) {
            Debug.LogError($"Recieved LobbyUpdatedEvent when lobbyID's do not match. Current: \"{LobbyID}\" Incoming: \"{lobbyID}\"");
            return;
        }
        LobbyData = newData;
    }

    private void ClientManager_OnClientRemoteConnectionState(RemoteConnectionStateArgs args) 
    {
        if(args.ConnectionState == RemoteConnectionState.Started) {
        } else if(args.ConnectionState == RemoteConnectionState.Stopped) {
            StopCommunication();
        }
    }
#endregion

    public bool InLobby { get { return LobbyID != null && LobbyData.HasValue; } }

}
