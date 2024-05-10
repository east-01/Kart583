using System;
using System.Collections.Generic;
using FishNet;
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

    public static readonly float PLAYER_WAIT_TIME = 20;
    public static readonly float ROUND_END_TIME = 15;
    public static readonly float MAP_PICK_TIME = 3;
    public static readonly KeyCode FORCE_MAP_PICK_KEY = KeyCode.F4;

    private LobbyManager manager;
    private string id;

    private LobbyState _state;
    public LobbyState state { 
        get { return _state; }
        set {
            BLog.Log($"{MessagePrefix}Setting state to {value}", LogChannel.GameLobby, 1);
            LobbyState prevState = _state;
            _state = value;
            timeInState = 0;
            LobbyStateChanged(prevState, _state);
            if(manager.HasLobby(id))
                manager.UpdateLobby(id, LobbyUpdateReason.STATE_CHANGE);
        }
    }
    private float timeInState;

    private Dictionary<NetworkConnection, PlayerData> players = new(); // Players in lobby

    /* Scene related */
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
    private bool CanAutoSelectLevel { get { return CoreManager.IsMultiplayer && !CoreManager.DevSettings.ManualLobbyPlayerWaitSwitch; } }
    public bool forceMapPick = false;
    private GameplayManager gameplayManager;

    public GameLobby(LobbyManager manager, string id) 
    {
        this.manager = manager;
        this.id = id;

        BLog.Log("Initialized lobby");
        SceneController.Instance.SceneRegisteredEvent += SceneDelegate_SceneRegistered;
        SceneController.Instance.SceneWillDeregisterEvent += SceneDelegate_SceneWillDeregister;
        SceneController.Instance.SceneDeregisteredEvent += SceneDelegate_SceneDeregistered;

        state = LobbyState.WAITING_FOR_PLAYERS;
        level = null;

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if(CoreManager.IsLocal && SceneNames.IsMapScene(sceneName)) {
            state = LobbyState.RACING;
            level = CoreManager.LevelAtlas.SearchEnumBySceneName(sceneName);
        }
    }

    public void Update() 
    {
        // State management
        timeInState += Time.deltaTime;

        if(Input.GetKeyDown(FORCE_MAP_PICK_KEY))
            forceMapPick = true;

        CheckState();
    }  

#region State Management
    /// <summary>
    /// Checks the current state and attempts to escalate to the next state.
    /// Can only escalate state once per frame.
    /// </summary>
    public void CheckState() 
    {
        switch(state) {
            case LobbyState.WAITING_FOR_PLAYERS:
                bool timePassed = CanAutoSelectLevel && timeInState >= PLAYER_WAIT_TIME;
                bool noAvailableSpace = CanAutoSelectLevel && !CoreManager.DevSettings.ManualLobbyPlayerWaitSwitch && OpenSlots == 0;
                if(Input.GetKeyDown(FORCE_MAP_PICK_KEY) || 
                   noAvailableSpace || 
                   timePassed || 
                   CoreManager.IsLocal)
                    state = LobbyState.MAP_SELECTION;
                break;
            case LobbyState.MAP_SELECTION:
                bool autoSelectValid = CanAutoSelectLevel && timeInState >= MAP_PICK_TIME;
                if(level == null && (autoSelectValid || forceMapPick)) {
                    forceMapPick = false;
                    KartLevel? selectedLevel;
                    if(CoreManager.DevSettings.OverrideMapPick)
                        selectedLevel = CoreManager.DevSettings.map;
                    else 
                        selectedLevel = LevelAtlas.PickRandomLevel();

                    SetLevel(selectedLevel.Value);
                } else if(level != null && MapScene != null/* && gameplayManager != null*/) {
                    MovePlayersToMap();                    
                    state = LobbyState.RACING;
                }
                break;
            case LobbyState.RACING:
                if(gameplayManager.RaceManager.Phase == RacePhase.FINISHED) {
                    AwardPoints();
                    state = LobbyState.POST_RACE;
                }
                break;
            case LobbyState.POST_RACE:
                if(mapSceneData == null || !NetSceneController.Instance.IsSceneRegistered(mapSceneData)) {
                    MovePlayersToLobby();
                    state = LobbyState.WAITING_FOR_PLAYERS;
                } else if(NetSceneController.Instance.GetSceneElements(mapSceneData).Clients.Count == 0) {
                    state = LobbyState.WAITING_FOR_PLAYERS;
                } else if(CoreManager.IsMultiplayer && timeInState >= ROUND_END_TIME) {
                    MovePlayersToLobby();
                    state = LobbyState.WAITING_FOR_PLAYERS;
                }
                break;
        }
    }

    private void LobbyStateChanged(LobbyState prev, LobbyState current) 
    {
        if(current == LobbyState.MAP_SELECTION) {
            level = null;
        }
    }
#endregion

#region Player Management
    public void AddPlayer(NetworkConnection conn, PlayerData data) 
    {
        players.Add(conn, data);
        manager.UpdateLobby(id, LobbyUpdateReason.PLAYER_JOIN);
 
        BLog.Log($"{MessagePrefix}Adding player {data.Summary} to lobby.", LogChannel.GameLobby, 0);
        // if(LobbyScene != null)
        //     SceneDelegate.Instance.AddClientToScene(conn, lobbySceneData);
        // else
        //     lobbyJoinQueue.Add(conn);
    }

    /// <summary>
    /// Moves all players in map scene back to lobby.
    /// </summary>
    public void MovePlayersToLobby() 
    {
        foreach(NetworkConnection client in players.Keys) {
            NetSceneController.Instance.TargetRpcLoadScene(client, new(SceneNames.MENU_LOBBY), false);
        }
    }

    public void MovePlayersToMap() 
    {
        if(mapSceneData == null) {
            Debug.LogError("Can't move players to map, map scene data is null.");
            return;
        }
        if(!NetSceneController.Instance.IsSceneRegistered(mapSceneData)) {
            Debug.LogError("Can't move players to map, the scene isn't registered");
            return;
        }

        BLog.Log($"{MessagePrefix}Sending {players.Count} player(s) to map, is server: {InstanceFinder.IsServer} is client: {InstanceFinder.IsClient}", LogChannel.GameLobby, 0);
        foreach(NetworkConnection conn in players.Keys) {
            NetSceneController.Instance.AddClientToScene(conn, mapSceneData);
        }
    }
#endregion

#region Administrative
    /// <summary>
    /// End the current round. Currently does multiple admin things:
    ///   1. Award points
    ///   2. Recall players to lobby
    /// The map will automatically delete once all players are removed.
    /// </summary>
    public void CompleteRound() 
    {
        AwardPoints();

        MovePlayersToLobby();
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
#endregion

#region Scene/Level Management
    /// <summary>
    /// Set the level. Will load the corresponding scene on the server.
    /// </summary>
    public void SetLevel(KartLevel level) 
    {   
        if(this.level != null) {
            Debug.LogError("Can't set level, one already exists");
            return;
        }
        BLog.Log($"{MessagePrefix}Picked level {level} and requesting map scene.", LogChannel.GameLobby, 0);
        this.level = level;

        SceneLookupData newMapLookupData = new(CoreManager.LevelAtlas.RetrieveData(level).sceneName);
        NetSceneController.Instance.LoadSceneAsServer(newMapLookupData);
    }

    public void SceneDelegate_SceneRegistered(SceneLookupData lookupData) 
    {
        SceneElements elements = NetSceneController.Instance.GetSceneElements(lookupData);
        if(elements.HasOwner) {
            Debug.LogError($"Can't claim newly registered scene \"{lookupData}\" because it already has an owner.");
            return;
        }

        if(SceneNames.IsMapScene(lookupData.Name)) {
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
        NetSceneController.Instance.SetSceneElements(lookupData, elements);

        BLog.Log($"{MessagePrefix}Claimed scene \"{lookupData}\"", LogChannel.GameLobby, 0);
    }

    public void SceneDelegate_SceneWillDeregister(SceneLookupData lookupData) 
    {
        if(lookupData == mapSceneData) {
            DeregisterGameplayManager();
        }
    }

    public void SceneDelegate_SceneDeregistered(SceneLookupData lookupData) 
    {
        if(lookupData == mapSceneData) {
            mapSceneData = null;
        }
    }

    private void RegisterGameplayManager(GameplayManager gm) 
    {
        gameplayManager = gm;
        gameplayManager.SetGameLobby(this);
    }

    private void DeregisterGameplayManager() 
    {
        if(gameplayManager == null) {
            Debug.LogError("Can't deregister GameplayManager because it is null.");
            return;
        }
    }
#endregion

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
    public string MessagePrefix { get { return $"({ID})"; } }
    public SceneLookupData MapSceneData { get { return mapSceneData; } }
    public LobbyState State { get { return state; } }
    public Scene? MapScene { get { 
        if(mapSceneData is null || !NetSceneController.Instance.IsSceneRegistered(mapSceneData))
            return null;
        return NetSceneController.Instance.GetSceneElements(mapSceneData).Scene;
    } }

    public GameplayManager GameplayManager { get { return gameplayManager; } }
    public KartLevel? Level { get { return level; } }

    public Dictionary<NetworkConnection, PlayerData> Players { get { return players; } }
    public int PlayerCount { get { return players.Count; } }

    public int OpenSlots { get { return CoreManager.Instance.PlayerLimit - players.Count; } }

    public MenuLobbyController MenuLobbyController { get {
        MenuLobbyController[] lobbyControllers = GameObject.FindObjectsOfType<MenuLobbyController>();
        if(lobbyControllers.Length != 1) {
            Debug.LogError($"Found != 1 MenuLobbyControllers ({lobbyControllers.Length})");
            return null;
        }
        return lobbyControllers[0];
    } }

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
    RACING, // The lobby is in game
    POST_RACE
}

public enum LobbyUpdateReason 
{
    NONE,
    STATE_CHANGE,
    PLAYER_JOIN
}