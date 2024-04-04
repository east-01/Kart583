using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Editing;
using FishNet.Managing.Scened;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// The scene delegate is a global networked object that will handle clients being placed
///   into scenes. Some of the more confusing code I've written. See flowchart:
/// https://lucid.app/lucidchart/df418eac-4680-413e-9bbd-19c1bc7376ef/edit?viewport_loc=-2414%2C-625%2C2387%2C1147%2C0_0&invitationId=inv_a757cd21-5e23-440e-8b4d-cd3943fe5ef7
/// </summary>
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(LobbyManager))]
public class SceneDelegate : NetworkBehaviour
{

    public static SceneDelegate Instance;
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance._gameplayManagerDelegate; } }
    public static LobbyManager LobbyManager { get { return Instance._lobbyManager; } }

    public delegate void SceneRegisteredHandler(SceneLookupData sceneLookupData); // We don't provide SceneElements here to require users of event to go through SceneDelegate
    /// <summary>
    /// Called when a scene is registered with the SceneDelegate
    /// </summary>
    public event SceneRegisteredHandler SceneRegisteredEvent;

    public delegate void ClientAddedToSceneHandler(NetworkConnection client, SceneLookupData sceneLookupData);
    /// <summary>
    /// Called when a client is added to the scene.
    /// For now, only is called on the client that was added.
    /// </summary>
    public event ClientAddedToSceneHandler ClientAddedToSceneEvent;

    private GameplayManagerDelegate _gameplayManagerDelegate;
    private LobbyManager _lobbyManager;
    [SerializeField]
    private GameObject _atlasPrefab;

    private Dictionary<SceneLookupData, GameLobby> expectingScene = new(); // Server only, connects GameLobbies to scenes
    private Dictionary<SceneLookupData, SceneElements> loadedScenes = new();
    private Dictionary<SceneLookupData, GameplayManager> gameplayManagers = new();

    /// <summary>
    /// Client side only, the data that we're trying to get the client to load
    /// </summary>
    private SceneLookupData loadTarget;

#region Initializers
    void Awake() 
    {
        if(Instance != null)
            throw new InvalidOperationException("Tried to create a new SceneDelegate when one already exists.");

        Instance = this;

        _gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        _lobbyManager = GetComponent<LobbyManager>();
    }

    private void OnEnable() 
    { 
        InstanceFinder.SceneManager.OnLoadEnd += FishSceneManager_SceneLoaded; 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += UnitySceneManager_SceneLoaded;
    }

    private void OnDisable() { 
        if(InstanceFinder.SceneManager != null)
            InstanceFinder.SceneManager.OnLoadEnd -= FishSceneManager_SceneLoaded; 
    }
#endregion

#region EventHandlers/SceneRegistration
    /// <summary>
    /// The server side scene manager load event
    /// </summary>
    /// <param name="args"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private void FishSceneManager_SceneLoaded(SceneLoadEndEventArgs args)
    {
        foreach(Scene scene in args.LoadedScenes) {
            SceneDelegate.SceneDelegateDebug($"{(base.IsServer ? "Server" : "Client")} loaded scene " + scene.name + ", handle: " + scene.handle);

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

            // ---Scene loaded, add it to loadedScenes dictionary, also done in SceneManager_SceneLoaded
            // ---loadedScenes.Add(sceneLookupData, scene);

            RegisterScene(scene);
        }

        // Disable event systems
        if(base.IsServer) {
            int disabledEventSystems = 0;
            foreach(EventSystem system in FindObjectsOfType<EventSystem>()) {
                system.enabled = false;
                disabledEventSystems++;
            }
            print($"Disabled {disabledEventSystems} event system(s).");
        }

        if(args.SkippedSceneNames.Length > 0)
            SceneDelegate.SceneDelegateDebug($"RegisterScenes skipped {args.SkippedSceneNames.Length} scene(s).");
    }

    /// <summary>
    /// The client side scene manager load event
    /// </summary>
    private void UnitySceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the servers UnityEngine SceneManager. That's for RegisterScenes.
        if(!base.IsClient)
            return;
        if(scene != null && loadTarget != null && scene.name != loadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }

        // ---Scene loaded, add to loadedScenes Dictionary. Also done in RegisterScenes for server
        // ---loadedScenes.Add(new(scene.handle, scene.name), scene);
        // Register scene
        RegisterScene(scene);

        NetworkConnection client = base.LocalConnection;
        SceneDelegate.SceneDelegateDebug("LoadedScenes#UnitySceneManager_SceneLoaded: Validated client loaded, scene. Disconnecting them from their other scenes.");
        foreach(Scene otherScene in client.Scenes) {
            if(otherScene != scene) {
                SceneDelegate.SceneDelegateDebug($"LoadedScenes#SceneManager_SceneLoaded: Clearing other scene {otherScene.name}/{otherScene.handle} from client");
                base.SceneManager.RemoveConnectionsFromScene(new NetworkConnection[] { client }, otherScene);
            }
        }

        ServerRpcClientLoadedScene(base.LocalConnection, loadTarget);

        SceneDelegate.SceneDelegateDebug($"SceneDelegate#UnitySceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"");
    }

    /// <summary>
    /// Registers the scene in the SceneDelegate and issues a SceneRegisteredEvent when done.
    /// </summary>
    private void RegisterScene(Scene scene) 
    {
        SceneLookupData lookupData = new(scene.handle, scene.name);
        SceneElements elements = new() {
            Scene = scene
            // GameLobby owner is set by a GameLobby when the SceneRegistered event is called
            // GameplayManager is loaded by GameplayManagerDelegate when it's requested
        };

        loadedScenes.Add(lookupData, elements);

        SceneDelegateDebug($"Registered scene \"{lookupData}\". Calling event.");
        SceneRegisteredEvent?.Invoke(lookupData);
    }
#endregion

    /* Move a network connection to a specific scene.<br>
     * Process outlined here: https://github.com/FirstGearGames/FishNet/discussions/564#discussioncomment-8212721
     *   1. Ensure client has scene loaded, if not, load it
     *   2. Client tells server they have scene
     *   3. We can perform AddConnectionToScene */

#region Handshake
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
        SceneDelegateDebug($"SceneDelegate#LoadSceneForGameLobby: Telling server to load scene w/ data name: {lookupData.Name} handle: {lookupData.Handle}");
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
            SceneDelegateDebug("SceneDelegate#MoveToLobby: Handle exists, scene name is: " + lookupData.Name);
        } else {
            lookupData = new SceneLookupData(SceneNames.MENU_LOBBY);
            SceneDelegateDebug("SceneDelegate#MoveToLobby: Creating new lookup data since we dont have a handle stored");
        }

        if(gameLobby.LobbyScene == null)
            LoadSceneForGameLobby(gameLobby.ID, lookupData);

        if(gameLobby.MapScene != null && gameLobby.IsMapLevelEmpty()) {
            SceneDelegateDebug("SceneDelegate#MoveToLobby: Found map scene empty, deleting it");
            gameLobby.DeleteMapScene();
        } else {
            print($"SceneDelegate#MoveToLobby: Map scene couldn't delete. is null: {gameLobby.MapScene == null} is empty: {gameLobby.IsMapLevelEmpty()}");
        }

        TargetRpcEnsureSceneLoaded(client, lookupData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcMoveToLobby(NetworkConnection client) 
    {
        MoveToLobby(client);
        //_lobbyManager.UpdateLobby(_lobbyManager.GetLobbyID(client), LobbyUpdateReason.PLAYER_JOIN);
    }

    [Client]
    public void MoveClientToLobby() 
    {
        ServerRpcMoveToLobby(base.LocalConnection);
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
            SceneDelegateDebug("SceneDelegate#MoveToMap: Handle exists, scene name is: " + lookupData.Name);
        } else {
            lookupData = new SceneLookupData(LevelAtlas.RetrieveData(gameLobby.Level.Value).sceneName);
            SceneDelegateDebug("SceneDelegate#MoveToMap: Creating new lookup data since we dont have a handle stored");
        }

        if(gameLobby.MapScene == null)
            LoadSceneForGameLobby(gameLobby.ID, lookupData);

        TargetRpcEnsureSceneLoaded(client, lookupData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcMoveToMap(NetworkConnection client) 
    {
        MoveToMap(client);
    }

    [TargetRpc]
    private void TargetRpcEnsureSceneLoaded(NetworkConnection client, SceneLookupData lookupData) 
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != lookupData.Name) {
            loadTarget = lookupData;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lookupData.Name, LoadSceneMode.Single);
            SceneDelegateDebug($"SceneDelegate#TargetRpcEnsureSceneLoaded: Client calling load scene \"{lookupData.Name}\"");
        } else {
            SceneDelegateDebug($"SceneDelegate#TargetRpcEnsureSceneLoaded: Scene \"{lookupData.Name}\" is already loaded, skipping to SceneDelegate#ServerRpcClientLoadedScene");
            ServerRpcClientLoadedScene(base.LocalConnection, lookupData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcClientLoadedScene(NetworkConnection client, SceneLookupData lookup) 
    {
        GameLobby gameLobby = _lobbyManager.GetLobby(client);
        if(gameLobby == null) {
            Debug.LogError("Tried ensure client loaded scene but client isn't in a lobby.");
            return;
        }

        Scene? targetScene = gameLobby.GetLoadedScene(lookup, true);
        if(targetScene == null) {
            Debug.LogError($"Client loaded scene but GameLobby doesn't have corresponding scene loaded. Lookup info: {lookup.Name} handle: {lookup.Handle}");
            return;
        }

        base.SceneManager.AddConnectionToScene(client, targetScene.Value);
        TargetRpcClientAddedToScene(client, lookup);
    }

    [TargetRpc]
    private void TargetRpcClientAddedToScene(NetworkConnection client, SceneLookupData lookup) 
    {
        ClientAddedToSceneEvent?.Invoke(client, lookup);
    }
#endregion

    public bool IsSceneLoaded(SceneLookupData lookupData) { return loadedScenes.ContainsKey(lookupData); }
    public SceneElements GetSceneElements(SceneLookupData lookupData) 
    {
        if(!loadedScenes.ContainsKey(lookupData)) {
            Debug.LogError($"Failed to get SceneElements for lookupData \"{lookupData}\". Put an IsSceneLoaded() check before the method calling this.");
            return default;
        }
        return loadedScenes[lookupData];     
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

    private static bool sendSceneDelegateDebug = true;
    public static void SceneDelegateDebug(string message) {
        if(sendSceneDelegateDebug)
            print(message);
    }

    public LevelAtlas LevelAtlas { get { return _atlasPrefab.GetComponent<LevelAtlas>(); } }

}

/// <summary>
/// To be paired with SceneLookupData to hold relevant scene elements in the SceneDelegate
/// </summary>
[Serializable]
public struct SceneElements {
    private SceneLookupData lookupData;

    [SerializeField]
    private Scene scene;
    /// <summary>
    /// The scene reference
    /// </summary>
    public Scene Scene {
        readonly get { return scene; } 
        set { 
            if(scene.IsValid()) {
                Debug.LogError("Can't overwrite existing scene.");
                return;
            }
            scene = value;
            lookupData = new(value.handle, value.name);
        }
    }

    [SerializeField]
    private GameLobby owner;
    /// <summary>
    /// Only usable on the server side. Can be null if no lobby claims.
    /// </summary>
    public GameLobby Owner {
        readonly get { return owner; }
        set {
            if(owner != null) {
                Debug.LogError("Can't set owner since one already exists.");
                return;
            }
            owner = value;
        }
    }
    public bool HasOwner { get { return owner != null; } }

    [SerializeField]
    private GameplayManager gameplayManager;
    /// <summary>
    /// The GameplayManager held in this scene. Can be null if a lobby scene.
    /// </summary>
    public GameplayManager GameplayManager {
        get {
            if(gameplayManager == null) {
                gameplayManager = GameplayManagerDelegate.LocateGameplayManager(scene);
            }
            return gameplayManager;
        }
    }

}