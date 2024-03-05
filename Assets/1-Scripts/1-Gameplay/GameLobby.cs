using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.SceneManagement;

/// <summary>
/// The GameLobby resides on the server and will delegate what to do with the players.
/// Clients will get information about the lobby via the LobbyData struct.
/// </summary>
public class GameLobby 
{

    public static readonly float PLAYER_WAIT_TIME = 5;

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
            manager.UpdateLobby(id, this);
        }
    }
    private float timeInState;

    private Dictionary<NetworkConnection, PlayerData> players = new();
    private KartLevel? level;

    private SceneLookupData lobbySceneData;
    private SceneLookupData mapSceneData;

    private Dictionary<SceneLookupData, Scene> loadedScenes = new();

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
            Debug.Log($"Moving {lobbyJoinQueue.Count} players from queue (is server: {InstanceFinder.IsServer})");
            lobbyJoinQueue.ForEach(conn => {
                SceneDelegate.Instance.MoveToLobby(conn);
                Debug.Log("Added player from queue");
            });
            lobbyJoinQueue.Clear();
        }

    }  

    public void AddPlayer(NetworkConnection conn, PlayerData data) 
    {
        // if(lobbyScene != null) {
        //     SceneDelegate.Instance.MoveToLobby(conn);
        //     Debug.Log("added player directly to scene");
        // } else {
        //     lobbyJoinQueue.Add(conn);
        //     Debug.Log("added player to join quee");
        // }

        players.Add(conn, data);
        manager.UpdateLobby(id, this);
 
        SendDebugMessage("Adding player");
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
        }

        loadedScenes.Add(data, scene);
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

    private bool sendLobbyDebugMessages = true;
    public void SendDebugMessage(string message) {
        if(sendLobbyDebugMessages)
            Debug.Log($"({id}) {message}");
    }

    public LobbyData Data { get {
        List<string> pNames = new();
        foreach(PlayerData data in players.Values) { pNames.Add(data.name); }

        return new() {
            playerNames = pNames,
            state = this.state,
            timeInState = this.timeInState
        };
    } }

    public string ID { get { return id; } }
    public SceneLookupData LobbySceneData { get { return lobbySceneData; } }
    public SceneLookupData MapSceneData { get { return mapSceneData; } }
    public LobbyState State { get { return state; } }
    public KartLevel? Level { get { return level; } }
    public Scene? LobbyScene { get { return GetLoadedScene(lobbySceneData, false); } }
    public Scene? MapScene { get { return GetLoadedScene(mapSceneData, false); } }
    public int OpenSlots { get { return KartsIRManager.PlayerLimit - players.Count; } }
}

[Serializable]
public struct LobbyData 
{
    public List<string> playerNames;
    public LobbyState state;
    public float timeInState;
}

[Serializable]
public enum LobbyState 
{
    WAITING_FOR_PLAYERS, 
    MAP_SELECTION, 
    RACING // The lobby is in game
}