using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The scene delegate is a global networked object that will handle clients being placed
///   into scenes. The current setup works like this:
/// Server-side scene delegate:
///   - Spawns scenes
///   - Recieves client requests to be placed into scenes
/// Client-side scene delegate:
///   - On start, makes a request to join a scene
/// </summary>
[RequireComponent(typeof(LobbyManager))]
public class SceneDelegate : NetworkBehaviour
{

    public static SceneDelegate Instance;
    public static LobbyManager LobbyManager { get { return Instance._lobbyManager; } }

    private List<Scene> ScenesLoaded = new(); // Unused for now, see RegisterScenes

    private LobbyManager _lobbyManager;
    [SerializeField]
    private GameObject _gameplayManagerPrefab;

    /// <summary>
    /// A queue of lobby ids requesting a lobby scene.
    /// </summary>
    private List<GameLobby> requestingLobbyScenes = new(); // TODO: Chopping block
    /// <summary>
    /// A queue of lobby ids requesting a map scene.
    /// </summary>
    private List<GameLobby> requestingMapScenes = new(); // TODO: Chopping block

    private Dictionary<SceneLookupData, GameLobby> expectingScene = new();

    /// <summary>
    /// Client only variable storing the lobby scene
    /// </summary>
    private Scene clientSceneLobby;
    /// <summary>
    /// Client only variable storing server scene
    /// </summary>
    private Scene clientSceneMap;

    /// <summary>
    /// Client side only, the data that we're trying to get the client to load
    /// </summary>
    private SceneLookupData loadTarget;

    void Awake() 
    {
        if(Instance != null)
            throw new InvalidOperationException("Tried to create a new SceneDelegate when one already exists.");

        Instance = this;

        _lobbyManager = GetComponent<LobbyManager>();
    }

    void OnEnable() 
    { 
        InstanceFinder.SceneManager.OnLoadEnd += RegisterScenes; 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_SceneLoaded;
    }

    void OnDisable() { 
        if(InstanceFinder.SceneManager != null)
            InstanceFinder.SceneManager.OnLoadEnd -= RegisterScenes; 
    }

    /* Move a network connection to a specific scene.<br>
     * Process outlined here: https://github.com/FirstGearGames/FishNet/discussions/564#discussioncomment-8212721
     *   1. Ensure client has scene loaded, if not, load it
     *   2. Client tells server they have scene
     *   3. We can perform AddConnectionToScene */

    [Server]
    public void LoadSceneForGameLobby(string lobbyID, SceneLookupData lookupData) 
    {
        GameLobby gameLobby = _lobbyManager.GetLobby(lobbyID);
        if(gameLobby == null) {
            Debug.LogError("Tried to load a scene for a game lobby but lobby id doesn't exist.");
            return;
        }

        expectingScene.Add(lookupData, gameLobby);

        SceneLoadData sld = new SceneLoadData(lookupData);
        sld.Options.AllowStacking = true;
        sld.Options.AutomaticallyUnload = false;
        sld.ReplaceScenes = ReplaceOption.All;

        base.SceneManager.LoadConnectionScenes(sld);
        print($"SceneDelegate#LoadSceneForGameLobby: Telling server to load scene w/ data name: {lookupData.Name} handle: {lookupData.Handle}");
    }

    [Server]
    public void MoveToLobby(NetworkConnection client) 
    {
        GameLobby gameLobby = _lobbyManager.GetLobby(client);
        if(gameLobby == null) {
            Debug.LogError("Tried to move client connection to lobby scene but connection doesn't have an assigned GameLobby.");
            return;
        }
        
        int existingHandle = 0;
        if(gameLobby.LobbySceneData is not null) {
            existingHandle = gameLobby.LobbySceneData.Handle;
        }

        SceneLookupData lookupData;
        if(existingHandle != 0) {
            lookupData = gameLobby.LobbySceneData;
            print("SceneDelegate#MoveToLobby: Handle exists, scene name is: " + lookupData.Name);
        } else {
            lookupData = new SceneLookupData(SceneNames.MENU_LOBBY);
            print("SceneDelegate#MoveToLobby: Creating new lookup data since we dont have a handle stored");
        }

        if(gameLobby.LobbyScene == null)
            LoadSceneForGameLobby(gameLobby.ID, lookupData);

        TargetRpcEnsureSceneLoaded(client, lookupData);
    }

    [Server]
    public void MoveToMap(NetworkConnection client) 
    {
        GameLobby gameLobby = _lobbyManager.GetLobby(client);
        if(gameLobby == null) {
            Debug.LogError("Tried to move client connection to map scene but connection doesn't have an assigned GameLobby.");
            return;
        }
        
        int existingHandle = 0;
        if(gameLobby.MapSceneData is not null) {
            existingHandle = gameLobby.MapSceneData.Handle;
        }
        
        SceneLookupData lookupData;
        if(existingHandle != 0) {
            lookupData = gameLobby.MapSceneData;
            print("SceneDelegate#MoveToMap: Handle exists, scene name is: " + lookupData.Name);
        } else {
            lookupData = new SceneLookupData(LevelAtlas.RetrieveData(gameLobby.Level.Value).sceneName);
            print("SceneDelegate#MoveToMap: Creating new lookup data since we dont have a handle stored");
        }

        if(gameLobby.MapScene == null)
            LoadSceneForGameLobby(gameLobby.ID, lookupData);

        TargetRpcEnsureSceneLoaded(client, lookupData);
    }

    [TargetRpc]
    private void TargetRpcEnsureSceneLoaded(NetworkConnection client, SceneLookupData lookupData) 
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != lookupData.Name) {
            loadTarget = lookupData;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lookupData.Name, LoadSceneMode.Single);
            print($"SceneDelegate#TargetRpcEnsureSceneLoaded: Client calling load scene \"{lookupData.Name}\"");
        } else {
            print($"SceneDelegate#TargetRpcEnsureSceneLoaded: Scene \"{lookupData.Name}\" is already loaded, skipping to SceneDelegate#ServerRpcClientLoadedScene");
            ServerRpcClientLoadedScene(base.LocalConnection, lookupData);
        }
    }

    private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the servers UnityEngine SceneManager. That's for RegisterScenes.
        if(!base.IsClient)
            return;
        if(scene != null && loadTarget != null && scene.name != loadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }
        ServerRpcClientLoadedScene(base.LocalConnection, loadTarget);
        print($"SceneDelegate#SceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"");
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcClientLoadedScene(NetworkConnection client, SceneLookupData lookup) 
    {
        GameLobby gameLobby = _lobbyManager.GetLobby(client);
        if(gameLobby == null) {
            Debug.LogError("Tried ensure client loaded scene but client isn't in a lobby.");
            return;
        }

        print($"SceneDelegate#ServerRpcClientLoadedScene: Looking up w/ name: {lookup.Name} handle: {lookup.Handle}");

        Scene? targetScene = gameLobby.GetLoadedScene(lookup, true);
        if(targetScene == null) {
            Debug.LogError("Client loaded scene but GameLobby doesn't have corresponding scene loaded.");
            return;
        }

        base.SceneManager.AddConnectionToScene(client, targetScene.Value);

        print("SceneDelegate#ServerRpcClientLoadedScene: Validated client loaded, scene. Adding their connection");
    }

    private void RegisterScenes(SceneLoadEndEventArgs args)
    {

        LevelAtlas la = _gameplayManagerPrefab.GetComponent<LevelAtlas>();
        foreach(Scene scene in args.LoadedScenes) {
            print($"{(base.IsServer ? "Server" : "Client")} loaded scene " + scene.name + ", handle: " + scene.handle);

            // Find which lobby is expecing this scene
            SceneLookupData sceneLookupData = new(scene.handle, scene.name);
            SceneLookupData sceneLookupDataNoHandle = new(0, scene.name); // Check for handleless lookup
            if(!expectingScene.ContainsKey(sceneLookupData) && !expectingScene.ContainsKey(sceneLookupDataNoHandle)) {
                if(scene.name != SceneNames.MENU_SERVER)
                    Debug.LogWarning($"Scene \"{scene.name}\" was loaded without any lobby expecting it."); // Only send this warning message for scenes other than the server dash
                continue;
            }

            GameLobby expectingLobby = null;
            if(expectingScene.ContainsKey(sceneLookupData))
                expectingLobby = expectingScene[sceneLookupData];
            else if(expectingScene.ContainsKey(sceneLookupDataNoHandle))
                expectingLobby = expectingScene[sceneLookupDataNoHandle];
            else
                throw new InvalidOperationException("Shouldn't be able to reach this");

            expectingLobby.RegisterLoadedScene(sceneLookupData, scene);

            ScenesLoaded.Add(scene);
        }

        if(args.SkippedSceneNames.Length > 0)
            print($"RegisterScenes skipped {args.SkippedSceneNames.Length} scene(s).");
    }

    public void CheckInitialGlobalScene() 
    {
        if(!base.IsServer)
            return;

        // Ensure that the server makes its global scene MenuServer, that way we'll be able
        //   to load other maps/menus with stacking.
        Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(activeScene.name != SceneNames.MENU_SERVER) 
        {
            SceneLoadData sld = new SceneLoadData(SceneNames.MENU_SERVER);
            base.SceneManager.LoadConnectionScenes(base.LocalConnection, sld);

            SceneUnloadData sud = new SceneUnloadData(activeScene.name);
            base.SceneManager.UnloadGlobalScenes(sud);
        }
    }

    /*[Server]
    public void RequestLobbyScene(string lobbyID) 
    {
        GameLobby requestingLobby = _lobbyManager.GetLobby(lobbyID);
        if(requestingLobby == null) {
            Debug.LogError($"Lobby id {lobbyID} isn't known to the LobbyManager but still requested a LobbyScene.");
            return;
        }

        SceneLoadData loadData = new SceneLoadData("MenuLobby");
        loadData.Options.AutomaticallyUnload = false;
        base.SceneManager.LoadConnectionScenes(loadData);

        requestingLobbyScenes.Add(requestingLobby);
    }

    [Server]
    public void RequestMapScene(string lobbyID, KartLevel level) 
    {
        GameLobby requestingLobby = _lobbyManager.GetLobby(lobbyID);
        if(requestingLobby == null) {
            Debug.LogError($"Lobby id {lobbyID} isn't known to the LobbyManager but still requested a MapScene.");
            return;
        }

        LevelAtlas la = _gameplayManagerPrefab.GetComponent<LevelAtlas>();

        SceneLoadData loadData = new SceneLoadData(la.RetrieveData(level).sceneName);
        loadData.Options.AutomaticallyUnload = false;
        base.SceneManager.LoadConnectionScenes(loadData);

        requestingMapScenes.Add(requestingLobby);
    }

    [Server]
    public void UnloadMapScene(string lobbyID, Scene existingScene) 
    {
        GameLobby requestingLobby = _lobbyManager.GetLobby(lobbyID);
        if(requestingLobby == null) {
            Debug.LogError($"Lobby id {lobbyID} isn't known to the LobbyManager but still requested to unload map scene.");
            return;
        }

        SceneUnloadData sud = new SceneUnloadData(existingScene.name);
        base.SceneManager.UnloadConnectionScenes(sud);

        requestingLobby.ClearMapScene();
    }*/

    public LevelAtlas LevelAtlas { get { return _gameplayManagerPrefab.GetComponent<LevelAtlas>(); } }

}
