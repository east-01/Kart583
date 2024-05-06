using System;
using System.Collections.Generic;
using System.Data.Common;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The LobbyManager is the SERVER SIDE of the lobby system, it takes in player connections 
///   and distributes them (using the scene manager) into games.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [SyncObject]
    private readonly SyncDictionary<NetworkConnection, string> connectionLobbyPair = new();

    /// <summary>
    /// Server side ONLY, clients should access data via lobbyData SyncDictionary
    /// </summary>
    private Dictionary<string, GameLobby> lobbies = new();

    public delegate void LobbyUpdateHandler(LobbyData newData, LobbyUpdateReason reason);
    public event LobbyUpdateHandler LobbyUpdated;

    private bool waitingForInput;

    private void Awake() 
    {
        if(GameLobby.PLAYER_WAIT_TIME <= 0)
            Debug.LogWarning("GameLobby's PLAYER_WAIT_TIME is <= 0, this is not recommended.");
    }

    private void Update () 
    {
        foreach(GameLobby lobby in lobbies.Values) { lobby.Update(); }
    }

    /// <summary>
    /// Creates and adds a lobby to the server.
    /// </summary>
    /// <returns></returns>
    [Server]
    public GameLobby CreateLobby() 
    {
        GameLobby newLobby = new(this, GenerateLobbyID());
        lobbies.Add(newLobby.ID, newLobby);
        BLog.Log($"Created lobby \"{newLobby.ID}\"", LogChannel.LobbyManager, 0);
        return newLobby;
    }

#region Client Movement
    public void JoinLobby(NetworkConnection newClient, PlayerData data) 
    {
        if(!base.IsServer) {
            ServerRpcJoinLobby(newClient, data);
            return;
        }
        if(connectionLobbyPair.ContainsKey(newClient)) {
            Debug.LogWarning("Already in a lobby");
            return;
        }

        BLog.Log($"Searching for a lobby for client {newClient}:", LogChannel.LobbyManager, 2);
        GameLobby lobbyToJoin = null;
        foreach(string id in lobbies.Keys) {
            GameLobby lobby = lobbies[id];
            // TODO: Add other determining factors like game state
            bool joinable = lobby.OpenSlots > 0/* && lobby.State == LobbyState.WAITING_FOR_PLAYERS*/;
            BLog.Log($"  Found \"{id}\" with {lobby.OpenSlots} open slots in state {lobby.State}. Joinable: {joinable}", LogChannel.LobbyManager, 2);
            if(joinable) {
                lobbyToJoin = lobby;
                break;
            }
        }

        // No lobbies to join, create a new one
        if(lobbyToJoin == null)
            lobbyToJoin = CreateLobby();

        connectionLobbyPair.Add(newClient, lobbyToJoin.ID); // This step must precede SceneDelegate#MoveToLobby which is in GameLobby#AddPlayer
        lobbyToJoin.AddPlayer(newClient, data);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcJoinLobby(NetworkConnection newClient, PlayerData data) 
    {
        JoinLobby(newClient, data);
    }

    /// <summary>
    /// Request that the server moves the provided client NetworkConnection to the lobby scene
    /// </summary>
    [Client]
    public void RequestLobbyMove() 
    {
        BLog.Log("Requesting lobby move.", LogChannel.LobbyManager, 0);
        ServerRpcRequestLobbyMove(base.LocalConnection);
    }

    /// <summary>
    /// Request that the server moves the provided client NetworkConnection to the lobby scene
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcRequestLobbyMove(NetworkConnection client) 
    {
        MoveClientToLobby(client);
    }

    /// <summary>
    /// Move the specified client to their lobby scene.
    /// </summary>
    [Server]
    public void MoveClientToLobby(NetworkConnection client) 
    {
        GameLobby lobby = GetLobby(client);
        if(lobby == null) {
            Debug.LogError("Can't move client to lobby, they are not in one.");
            return;
        }
        BLog.Log($"Client \"{client}\" requested to move to lobby", LogChannel.LobbyManager, 0);
        SceneDelegate.Instance.AddClientToScene(client, new(SceneNames.MENU_LOBBY));
    }
#endregion

#region Lobby Updating
    /// <summary>
    /// Fires the TargetRpcLobbyUpdatedEvent for all clients connected to a specific lobby
    /// </summary>
    [Server]
    public void UpdateLobby(string lobbyID, LobbyUpdateReason reason) 
    {
        if(lobbyID == null) {
            Debug.LogError("Can't update lobby because lobbyID is null.");
            return;
        }
        if(!lobbies.ContainsKey(lobbyID)) {
            Debug.LogError($"Couldn't update lobby \"{lobbyID}\" because it's not registered in the LobbyManager.");
            return;
        }
        GameLobby lobby = lobbies[lobbyID];
        LobbyData lobbyData = lobby.Data;

        // Invoke event on server
        LobbyUpdated?.Invoke(lobbyData, reason);

        // Invoke event for clients to said lobby
        foreach(NetworkConnection client in lobby.Players.Keys) {
            if(!Observers.Contains(client))
                continue;
            TargetRpcLobbyUpdatedEvent(client, lobbyData, reason);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcUpdateLobby(string lobbyID) 
    {
        UpdateLobby(lobbyID, LobbyUpdateReason.NONE);
    }

    [Client]
    public void RequestLobbyUpdate() 
    {
        print("Requesting lobby update and is client: " + base.IsClient);
        if(GetLobbyID() == null) {
            Debug.LogWarning("Can't request lobby update since current lobby id is null.");
            return;
        }
        ServerRpcUpdateLobby(GetLobbyID());
    }

    [TargetRpc]
    public void TargetRpcLobbyUpdatedEvent(NetworkConnection conn, LobbyData lobbyData, LobbyUpdateReason reason) {
        LobbyUpdated?.Invoke(lobbyData, reason);
    }
#endregion

#region Getters
    public bool HasLobby(String lobbyID) 
    {
        return lobbies.ContainsKey(lobbyID);
    }

    /// <summary>
    /// Gets the lobby id for the LocalConnection. Is a shortcut for:
    /// </summary>
    /// <code> GetLobbyID(base.LocalConnection) </code>
    [Client]
    public string GetLobbyID() { return GetLobbyID(base.LocalConnection); }

    /// <summary>
    /// Gets the lobby id for a specified NetworkConnection
    /// </summary>
    public string GetLobbyID(NetworkConnection conn) 
    {
        if(!connectionLobbyPair.ContainsKey(conn))
            return null;
        return connectionLobbyPair[conn];
    }

    /// <summary>
    /// Gets the lobby that the NetworkConnection is currently in.
    /// </summary>
    [Server]
    public GameLobby GetLobby(NetworkConnection conn) 
    {
        string id = GetLobbyID(conn);
        if(id == null)
            return null;
        return GetLobby(id);
    }

    /// <summary>
    /// Gets the lobby with the specified id.
    /// </summary>
    [Server]
    public GameLobby GetLobby(string id) 
    {
        if(!lobbies.ContainsKey(id))
            return null;
        return lobbies[id];
    }
#endregion

    /// <summary>
    /// Is the same thing as the server instance pressing GameLobby#FORCE_MAP_PICK_KEY.
    /// Only works for development builds.
    /// </summary>
    [Client]
    public void RequestForceMapPick() 
    {
        ServerRpcRequestForceMapPick(base.LocalConnection);        
    }

    /// <summary>
    /// Is the same thing as the server instance pressing GameLobby#FORCE_MAP_PICK_KEY.
    /// Only works for development builds.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcRequestForceMapPick(NetworkConnection client) 
    {
        if(!GameVersion.IsDevelopment) {
            Debug.LogWarning("Can't force map pick. We're not in a development build.");
            return;
        }
        GameLobby lobby = GetLobby(client);
        if(lobby == null) {
            Debug.LogError("Can't force map pick, client is not in a lobby.");
            return;
        }

        lobby.state = LobbyState.MAP_SELECTION;
    }

    [Server]
    private string GenerateLobbyID() 
    {
		for(int attempt = 0; attempt < KartsIRManager.rlBotNames.Length; attempt++) {
			string selection = KartsIRManager.rlBotNames[UnityEngine.Random.Range(0, KartsIRManager.rlBotNames.Length)];
			if(GetLobby(selection) == null)
				return selection;
		}
        Debug.LogWarning("Ran out of new lobby ids!");
		return "Lobby";
    }

    public int LobbyCount { get { return lobbies.Count; } }

}