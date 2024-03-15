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
/// The LobbyManager will take in player connections and distribute them, using the scene
///   manager, into games.
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    [SyncObject]
    private readonly SyncDictionary<NetworkConnection, string> connectionLobbyPair = new();

    /// <summary>
    /// Server side ONLY, clients should access data via lobbyData SyncDictionary
    /// </summary>
    private Dictionary<string, GameLobby> lobbies = new();

    [SyncObject]
    private readonly SyncDictionary<string, LobbyData> lobbyData = new();

    public delegate void LobbyUpdateHandler(LobbyData newData);
    public event LobbyUpdateHandler LobbyUpdated;

    private bool waitingForInput;

    private void Update () 
    {
        foreach(GameLobby lobby in lobbies.Values) { lobby.Update(); }

        if(waitingForInput && PlayerObjectManager.Instance != null && PlayerObjectManager.Instance.GetPlayerObjects().Count > 0) {
            waitingForInput = false;
            ServerRpcJoinLobby(base.LocalConnection, PlayerObjectManager.Instance.GetPlayerObjects()[0].data);
        }
    }

    public override void OnStartClient() 
    {
        if(PlayerObjectManager.Instance != null && PlayerObjectManager.Instance.GetPlayerObjects().Count > 0) {
            ServerRpcJoinLobby(base.LocalConnection, PlayerObjectManager.Instance.GetPlayerObjects()[0].data);
        } else {
            waitingForInput = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcJoinLobby(NetworkConnection newClient, PlayerData data) 
    {
        if(connectionLobbyPair.ContainsKey(newClient)) {
            Debug.LogWarning("Already in a lobby");
            return;
        }

        GameLobby lobbyToJoin = null;
        foreach(string id in lobbies.Keys) {
            GameLobby lobby = lobbies[id];
            // TODO: Add other determining factors like game state
            if(lobby.OpenSlots > 0) {
                lobbyToJoin = lobby;
                break;
            }
        }

        // No lobbies to join, create a new one
        if(lobbyToJoin == null) {
            lobbyToJoin = new GameLobby(this, GenerateLobbyID());
            lobbies.Add(lobbyToJoin.ID, lobbyToJoin);
            // SceneDelegate.Instance.RequestLobbyScene(lobbyToJoin.ID);
        }

        connectionLobbyPair.Add(newClient, lobbyToJoin.ID); // This step must precede SceneDelegate#MoveToLobby which is in GameLobby#AddPlayer
        lobbyToJoin.AddPlayer(newClient, data);

        TargetRpcJoinedLobby(newClient, lobbyToJoin.Data);
    }

    [TargetRpc]
    public void TargetRpcJoinedLobby(NetworkConnection conn, LobbyData joinedLobbyData) {
        LobbyUpdated?.Invoke(joinedLobbyData);
    }

    /*[ServerRpc]
    public void ServerRpcGetLobbyData(NetworkConnection client, out LobbyData? data) 
    {
        GameLobby lobby = GetLobby(client);
        if(lobby == null) {
            data = null;
            return;
        }
        data = lobby.Data;
    }

    [Client]
    public void RU() 
    {
        SRPCRU(base.LocalConnection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SRPCRU(NetworkConnection client) 
    {
        GameLobby lobby = GetLobby(client);
        if(lobby == null) {
            Debug.LogError("fuck");
            return;
        }

        UpdateLobby(lobby.ID, lobby);
    }*/

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

    [Client]
    public LobbyData? GetLobbyData() { return GetLobbyData(GetLobbyID(base.LocalConnection)); }
    public LobbyData? GetLobbyData(string id) 
    {
        if(!lobbyData.ContainsKey(id))
            return null;
        return lobbyData[id];
    }

    /// <summary>
    /// As specified in updating fields section of https://fish-networking.gitbook.io/docs/manual/guides/synchronizing/syncdictionary,
    ///   we must update the lobbies like this in order to ensure the latest data is in the SyncDictionary.
    /// </summary>
    public void UpdateLobby(string id, GameLobby updatedInstance) 
    {
        LobbyData newData = updatedInstance.Data;
        lobbyData[id] = newData;
        LobbyUpdated?.Invoke(newData);
    }

    [Server]
    private string GenerateLobbyID() 
    {
		for(int attempt = 0; attempt < KartSpawner.rlBotNames.Length; attempt++) {
			string selection = KartSpawner.rlBotNames[UnityEngine.Random.Range(0, KartSpawner.rlBotNames.Length)];
			if(GetLobby(selection) == null)
				return selection;
		}
        Debug.LogWarning("Ran out of new lobby ids!");
		return "Lobby";
    }
}