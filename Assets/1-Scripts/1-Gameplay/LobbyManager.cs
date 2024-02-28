using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private SyncDictionary<NetworkConnection, string> connectionLobbyPair;
    private SyncDictionary<string, GameLobby> lobbies;
}

[Serializable]
public struct GameLobby {
    private string id;
    private List<NetworkConnection> clients;
    private Dictionary<NetworkConnection, PlayerData> playerData;
}