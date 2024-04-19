using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameLobby resides on the server and will delegate what to do with the players.
/// Clients will get information about the lobby via the LobbyData struct.
/// </summary>
public class GameLobby 
{

    public static readonly float PLAYER_WAIT_TIME = 30;
    public static readonly float MAP_PICK_TIME = 3;

    private LobbyManager manager;
    private string id;

    private LobbyState _state;
    private LobbyState state { 
        get { return _state; }
        set {
            SendDebugMessage($"Setting state to {value}");
            _state = value;
            timeInState = 0;
            if(value == LobbyState.MAP_SELECTION) {
                level = null;
                MovePlayersToLobby();
            }
            manager.UpdateLobby(id, LobbyUpdateReason.STATE_CHANGE);
        }
    }
    private float timeInState;

    private Dictionary<NetworkConnection, PlayerData> players = new(); // Players in lobby

    /* Scene related */
    private SceneLookupData lobbySceneData;
    private SceneLookupData mapSceneData;

    /// <summary>
    /// The time of the last lobby request, used to request a new lobby every 5 seconds if we're missing one.
    /// </summary>
    private float lastLobbyRequestTime;    
    /// <summary>
    /// A list of connections waiting to join lobby, used for when the first player 
    ///   creates lobby and the lobby scene isn't created yet.
    /// </summary>
    private List<NetworkConnection> lobbyJoinQueue = new();

    /* Game related */
    private KartLevel? level;
    private GameplayManager gameplayManager;

    public GameLobby(LobbyManager manager, string id) 
    {
        this.manager = manager;
        this.id = id;

        SceneDelegate.Instance.SceneRegisteredEvent += SceneDelegate_SceneRegistered;
        SceneDelegate.Instance.SceneWillDeregisterEvent += SceneDelegate_SceneWillDeregister;
        SceneDelegate.Instance.SceneDeregisteredEvent += SceneDelegate_SceneDeregistered;

        state = LobbyState.WAITING_FOR_PLAYERS;
        level = null;
    }

    public void Update() 
    {
        // State management
        timeInState += Time.deltaTime;

        switch(state) {
            case LobbyState.WAITING_FOR_PLAYERS:
                bool timePassed = CoreManager.DevSettings.ManualLobbyPlayerWaitSwitch && timeInState >= PLAYER_WAIT_TIME;
                if(Input.GetKeyDown(KeyCode.F4) || (!CoreManager.DevSettings.ManualLobbyPlayerWaitSwitch && OpenSlots == 0) || timePassed)
                    state = LobbyState.MAP_SELECTION;
                break;
            case LobbyState.MAP_SELECTION:
                if(level == null && timeInState >= MAP_PICK_TIME) {
                    if(CoreManager.DevSettings.OverrideMapPick)
                        level = CoreManager.DevSettings.map;
                    else 
                        level = PickKartLevel();

                    SceneLookupData newMapLookupData = new(SceneDelegate.Instance.LevelAtlas.RetrieveData(level.Value).sceneName);
                    SceneDelegate.Instance.LoadSceneAsServer(newMapLookupData);

                    SendDebugMessage($"Picked level {level} and requesting map scene.");
                } else if(level != null && MapScene != null) {
                    foreach(NetworkConnection conn in players.Keys) {
                        SceneDelegate.Instance.AddClientToScene(conn, mapSceneData);
                    }
                    
                    state = LobbyState.RACING;
                }
                break;
            case LobbyState.RACING:
                break;
        }

        if(LobbyScene == null && Time.time - lastLobbyRequestTime >= 5f) {
            SceneDelegate.Instance.LoadSceneAsServer(new(SceneNames.MENU_LOBBY));
            lastLobbyRequestTime = Time.time;
        } else if(LobbyScene != null && lobbyJoinQueue.Count > 0) {
            // Handle lobby join queue
            lobbyJoinQueue.ForEach(conn => {
                SceneDelegate.Instance.AddClientToScene(conn, lobbySceneData);
            });
            lobbyJoinQueue.Clear();
        }

    }  

#region Player Management
    public void AddPlayer(NetworkConnection conn, PlayerData data) 
    {
        players.Add(conn, data);
        manager.UpdateLobby(id, LobbyUpdateReason.PLAYER_JOIN);
 
        SendDebugMessage("Adding player " + data.Summary + " to " + (LobbyScene != null ? "scene." : "queue."));
        if(LobbyScene != null)
            SceneDelegate.Instance.AddClientToScene(conn, lobbySceneData);
        else
            lobbyJoinQueue.Add(conn);
    }

    /// <summary>
    /// Moves all players in map scene back to lobby.
    /// </summary>
    public void MovePlayersToLobby() 
    {
        if(mapSceneData == null)
            return;
        if(!SceneDelegate.Instance.IsSceneRegistered(mapSceneData))
            return;

        SceneElements elements = SceneDelegate.Instance.GetSceneElements(mapSceneData);
        foreach(NetworkConnection client in elements.Clients) {
            SceneDelegate.Instance.AddClientToScene(client, lobbySceneData);
        }
    }
#endregion

#region Scene Management
    public void SceneDelegate_SceneRegistered(SceneLookupData lookupData) 
    {
        SceneElements elements = SceneDelegate.Instance.GetSceneElements(lookupData);
        if(elements.HasOwner) {
            Debug.LogError($"Can't claim newly registered scene \"{lookupData}\" because it already has an owner.");
            return;
        }

        if(SceneNames.IsLobbyScene(lookupData.Name)) {
            lobbySceneData = lookupData;
        } else if(SceneNames.IsMapScene(lookupData.Name)) {
            mapSceneData = lookupData;

            GameplayManager gameplayManager = elements.GameplayManager;
            if(gameplayManager != null) {
                RegisterGameplayManager(gameplayManager);
            } else {
                Debug.LogError("Can't register gameplay manager, it's null.");
                return;
            }
        } else 
            return;

        elements.Owner = this;
        elements.DeleteOnLastClientRemove = SceneNames.IsMapScene(lookupData.Name);
        SceneDelegate.Instance.SetSceneElements(lookupData, elements);

        SendDebugMessage($"Claimed scene \"{lookupData}\"");
    }

    public void SceneDelegate_SceneWillDeregister(SceneLookupData lookupData) 
    {
        if(lookupData == mapSceneData) {
            DeregisterGameplayManager();
        }
    }

    public void SceneDelegate_SceneDeregistered(SceneLookupData lookupData) 
    {
        if(lookupData == lobbySceneData) {
            lobbySceneData = null;
            Debug.LogWarning("Lobby was unloaded. Not sure what to do about it honestly but");
        } else if(lookupData == mapSceneData) {
            mapSceneData = null;
        }
    }

    private void RegisterGameplayManager(GameplayManager gm) 
    {
        gameplayManager = gm;
        gameplayManager.SetGameLobby(this);

        gameplayManager.RaceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;
    }

    private void DeregisterGameplayManager() 
    {
        if(gameplayManager == null) {
            Debug.LogError("Can't deregister GameplayManager because it is null.");
            return;
        }

        gameplayManager.RaceManager.RacePhaseChanged -= RaceManager_RacePhaseChanged;
    }
#endregion

    private void RaceManager_RacePhaseChanged(RacePhase prev, RacePhase current) 
    {
        SendDebugMessage($"RaceManager phase changed to {current}");
        if(current == RacePhase.FINISHED) {
            state = LobbyState.WAITING_FOR_PLAYERS;
            AwardPoints();
        }
    }

    /// <summary>
    /// Gets the placements dictionary from the RaceManager and adds the points awarded to each PlayerData.
    /// </summary>
    public void AwardPoints() 
    {
        if(gameplayManager == null) {
            Debug.LogError("Can't award points, the gameplay manager is null.");
            return;
        }
        if(gameplayManager.RaceManager.Phase != RacePhase.FINISHED) {
            Debug.LogError("Can't award points, the RaceManager's phase isn't FINISHED");
            return;
        }

        SyncDictionary<string, RacePlacementData> placements = gameplayManager.RaceManager.GetPlacements();
        List<NetworkConnection> playerKeys = new List<NetworkConnection>(players.Keys);
        foreach(NetworkConnection client in playerKeys) {
            PlayerData data = players[client];
            if(!placements.ContainsKey(data.uuid)) {
                Debug.LogWarning($"Tried to award points to \"{data.Summary}\" but they aren't in the placements dictionary.");
                continue;
            }
            data.points += placements[data.uuid].pointsAwarded;
            players[client] = data;
        }
    }

    public static KartLevel PickKartLevel() 
    {
        Array values = Enum.GetValues(typeof(KartLevel));
        return (KartLevel)values.GetValue(new System.Random().Next(values.Length));
    }

    private bool sendLobbyDebugMessages = false;
    public void SendDebugMessage(string message) 
    {
        if(sendLobbyDebugMessages)
            Debug.Log($"({id}) {message}");
    }

    public PlayerData? GetPlayerData(NetworkConnection client) 
    {
        if(!players.ContainsKey(client))
            return null;
        return players[client];
    }

    public LobbyData Data { get {
        List<PlayerData> players = new();
        foreach(PlayerData data in this.players.Values) { players.Add(data); }

        return new() {
            players = players,
            state = this.state,
            timeInState = this.timeInState
        };
    } }

    public string ID { get { return id; } }
    public SceneLookupData LobbySceneData { get { return lobbySceneData; } }
    public SceneLookupData MapSceneData { get { return mapSceneData; } }
    public LobbyState State { get { return state; } }
    public Scene? LobbyScene { get { 
        if(lobbySceneData is null || !SceneDelegate.Instance.IsSceneRegistered(lobbySceneData))
            return null;
        return SceneDelegate.Instance.GetSceneElements(lobbySceneData).Scene; 
    } }
    public Scene? MapScene { get { 
        if(mapSceneData is null || !SceneDelegate.Instance.IsSceneRegistered(mapSceneData))
            return null;
        return SceneDelegate.Instance.GetSceneElements(mapSceneData).Scene;
    } }

    public GameplayManager GameplayManager { get { return gameplayManager; } }
    public KartLevel? Level { get { return level; } }

    public Dictionary<NetworkConnection, PlayerData> Players { get { return players; } }
    public int PlayerCount { get { return players.Count; } }

    public int OpenSlots { get { return CoreManager.Instance.PlayerLimit - players.Count; } }

}

[Serializable]
public struct LobbyData 
{
    public List<PlayerData> players;
    public LobbyState state;
    public float timeInState;
    public float playerWaitTimeout;
}

[Serializable]
public enum LobbyState 
{
    WAITING_FOR_PLAYERS, 
    MAP_SELECTION, 
    RACING // The lobby is in game
}

public enum LobbyUpdateReason 
{
    NONE,
    STATE_CHANGE,
    PLAYER_JOIN
}