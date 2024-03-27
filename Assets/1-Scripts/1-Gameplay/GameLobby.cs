using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameLobby resides on the server and will delegate what to do with the players.
/// Clients will get information about the lobby via the LobbyData struct.
/// </summary>
public class GameLobby 
{

    public static readonly float PLAYER_WAIT_TIME = 2;

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
                // if(mapScene != null)
                    // SceneDelegate.Instance.UnloadMapScene(id, mapScene.Value);
            }
            manager.UpdateLobby(id, LobbyUpdateReason.STATE_CHANGE);
        }
    }
    private float timeInState;

    private Dictionary<NetworkConnection, PlayerData> players = new(); // Players in lobby

    /* Scene related */
    private SceneLookupData lobbySceneData;
    private SceneLookupData mapSceneData;

    private Dictionary<SceneLookupData, Scene> loadedScenes = new();

    /* Game related */
    private KartLevel? level;
    private GameplayManager gameplayManager;

    /// <summary>
    /// A list of connections waiting to join lobby, used for when the first player 
    ///   creates lobby and the lobby scene isn't created yet.
    /// </summary>
    private List<NetworkConnection> lobbyJoinQueue = new();
    private List<NetworkConnection> mapJoinQueue = new();

    public GameLobby(LobbyManager manager, string id) 
    {
        this.manager = manager;
        this.id = id;

        state = LobbyState.WAITING_FOR_PLAYERS;
        level = null;
    }

    public void Update() 
    {
        // State management
        timeInState += Time.deltaTime;

        switch(state) {
            case LobbyState.WAITING_FOR_PLAYERS:
                // PLAYER_WAIT_TIME == -1 is manual switch mode
                if(PLAYER_WAIT_TIME != -1 && (timeInState >= PLAYER_WAIT_TIME || OpenSlots == 0))
                    state = LobbyState.MAP_SELECTION;
                break;
            case LobbyState.MAP_SELECTION:
                if(level == null) {
                    // level = PickKartLevel();
                    level = KartLevel.TEST_TRACK;

                    SendDebugMessage("TODO: Delete existing map scene");
                    SceneLookupData newMapLookupData = new(SceneDelegate.Instance.LevelAtlas.RetrieveData(level.Value).sceneName);
                    SceneDelegate.Instance.LoadSceneForGameLobby(id, newMapLookupData);

                    // SceneDelegate.Instance.RequestMapScene(id, level.Value);
                    SendDebugMessage($"Picked level {level} and requesting map scene.");
                } else if(level != null) {
                    // SendDebugMessage($"Waiting for map scene, is map scene null {MapScene == null}; map scene handle: {mapSceneHandle}");
                    if(MapScene != null) {
                        foreach(NetworkConnection conn in players.Keys) {
                            SceneDelegate.Instance.MoveToMap(conn);
                        }
                        
                        state = LobbyState.RACING;
                    }
                }
                break;
            case LobbyState.RACING:
                break;
        }

        // Handle lobby join queue
        if(LobbyScene != null && lobbyJoinQueue.Count > 0) {
            lobbyJoinQueue.ForEach(conn => {
                SceneDelegate.Instance.MoveToLobby(conn);
            });
            lobbyJoinQueue.Clear();
        }

    }  

    public void AddPlayer(NetworkConnection conn, PlayerData data) 
    {
        players.Add(conn, data);
        manager.UpdateLobby(id, LobbyUpdateReason.PLAYER_JOIN);
 
        SendDebugMessage("Adding player " + data.Summary);
        SceneDelegate.Instance.MoveToLobby(conn);
    }

    public KartLevel PickKartLevel() 
    {
        Array values = Enum.GetValues(typeof(KartLevel));
        return (KartLevel)values.GetValue(new System.Random().Next(values.Length));
    }

    public void RegisterLoadedScene(SceneLookupData data, Scene scene) 
    {
        if(loadedScenes.ContainsKey(data)) {
            Debug.LogError("Tried to register a loaded scene with data that is already stored.");
            return;
        }

        SendDebugMessage($"Registered scene named \"{scene.name}\" with handle {data.Handle}");
        if(scene.name == SceneNames.MENU_LOBBY) {
            lobbySceneData = data;
            SendDebugMessage($"Lobby scene handle is now " + lobbySceneData.Handle);
        } else {
            mapSceneData = data;
            SendDebugMessage($"Map scene handle is now " + mapSceneData.Handle);

            // Register GameplayManager
            foreach(GameObject obj in scene.GetRootGameObjects()) {
                GameplayManager testManager = obj.GetComponent<GameplayManager>();
                if(testManager == null)
                    continue;
                RegisterGameplayManager(testManager);
                break;
            }
            if(gameplayManager == null)
                Debug.LogError("GameLobby registered a non-lobby scene, but failed to register an associated GameplayManager.");
        }

        loadedScenes.Add(data, scene);
    }

    public void DeleteLoadedScene(Scene scene) 
    {
        if(!loadedScenes.ContainsValue(scene)) {
            Debug.LogError("Tried to DeleteLoadedScene for a scene that isn't registered to this lobby!");
            return;
        }

        SendDebugMessage($"Deleting scene named \"{scene.name}\" with handle {scene.handle}");
        foreach(NetworkConnection client in players.Keys) {
            SceneDelegate.Instance.MoveToLobby(client);            
        }

        SceneUnloadData sud = new SceneUnloadData(new SceneLookupData(scene.handle, scene.name));
        SceneDelegate.Instance.SceneManager.UnloadConnectionScenes(sud);
    }

    /// <summary>
    /// Deletes the current map scene and deregisteres the current gameplayManager.
    /// </summary>
    public void DeleteMapScene() 
    {
        if(MapScene == null) {
            Debug.LogError("Can't delete map scene because MapScene is null.");
            return;
        }

        if(!IsMapLevelEmpty()) {
            Debug.LogError("Can't delete map scene because MapScene is occupied.");
            return;
        }

        DeregisterGameplayManager();
        DeleteLoadedScene(MapScene.Value);
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

    private void RaceManager_RacePhaseChanged(RacePhase prev, RacePhase current) 
    {
        SendDebugMessage($"RaceManager phase changed to {current}");
        if(current == RacePhase.FINISHED) {
            state = LobbyState.WAITING_FOR_PLAYERS;
        }
    }

    /// <summary>
    /// Get a loaded scene registered in loadedScenes using SceneLookupData.
    /// </summary>
    /// <param name="allowEmptyHandleLookup">Look through stored data and search for scene by name only</param>
    public Scene? GetLoadedScene(SceneLookupData data, bool allowNameLookup) 
    {
        if(data == null) 
            return null;
            
        SceneLookupData dataEmptyHandle = new(0, data.Name);
        if(loadedScenes.ContainsKey(data)) {
            return loadedScenes[data];
        } else if(allowNameLookup) {
            foreach(SceneLookupData testData in loadedScenes.Keys) {
                if(testData.Name == data.Name)
                    return loadedScenes[testData];
            }
        }

        return null;
    }

    /// <summary>
    /// Looks through players in lobby and check if they have the Map Level loaded.
    /// </summary>
    public bool IsMapLevelEmpty() 
    {
        if(MapScene == null)
            return true;

        foreach(NetworkConnection client in players.Keys) {
            if(client.Scenes.Contains(MapScene.Value))
                return false;
        }        
        return true;
    }

    private bool sendLobbyDebugMessages = true;
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
    public Scene? LobbyScene { get { return GetLoadedScene(lobbySceneData, false); } }
    public Scene? MapScene { get { return GetLoadedScene(mapSceneData, false); } }

    public GameplayManager GameplayManager { get { return gameplayManager; } }
    public KartLevel? Level { get { return level; } }

    public Dictionary<NetworkConnection, PlayerData> Players { get { return players; } }
    public int PlayerCount { get { return players.Count; } }

    public int OpenSlots { get { return KartsIRManager.PlayerLimit - players.Count; } }

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