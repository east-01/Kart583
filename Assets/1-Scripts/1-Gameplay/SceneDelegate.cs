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
[RequireComponent(typeof(LobbyManager))]
public class SceneDelegate : NetworkBehaviour
{

    public static SceneDelegate Instance;
    public static LobbyManager LobbyManager { get { return Instance._lobbyManager; } }

    public delegate void ClientAddedToSceneHandler(NetworkConnection client, SceneLookupData sceneLookupData);
    /// <summary>
    /// Called when a client is added to the scene.
    /// For now, only is called on the client that was added.
    /// </summary>
    public event ClientAddedToSceneHandler ClientAddedToSceneEvent;

    private LobbyManager _lobbyManager;
    [SerializeField]
    private GameObject _atlasPrefab;

    private Dictionary<SceneLookupData, GameLobby> expectingScene = new(); // Server only, connects GameLobbies to scenes
    private Dictionary<SceneLookupData, Scene> loadedScenes = new();
    private Dictionary<SceneLookupData, GameplayManager> gameplayManagers = new();

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

    private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the servers UnityEngine SceneManager. That's for RegisterScenes.
        if(!base.IsClient)
            return;
        if(scene != null && loadTarget != null && scene.name != loadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }

        // Scene loaded, add to loadedScenes Dictionary. Also done in RegisterScenes for server
        loadedScenes.Add(new(scene.handle, scene.name), scene);

        NetworkConnection client = base.LocalConnection;
        SceneDelegateDebug("SceneDelegate#SceneManager_SceneLoaded: Validated client loaded, scene. Disconnecting them from their other scenes.");
        foreach(Scene otherScene in client.Scenes) {
            if(otherScene != scene) {
                SceneDelegateDebug($"SceneDelegate#SceneManager_SceneLoaded: Clearing other scene {otherScene.name}/{otherScene.handle} from client");
                base.SceneManager.RemoveConnectionsFromScene(new NetworkConnection[] { client }, otherScene);
            }
        }

        ServerRpcClientLoadedScene(base.LocalConnection, loadTarget);
        SceneDelegateDebug($"SceneDelegate#SceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"");
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

    private void RegisterScenes(SceneLoadEndEventArgs args)
    {

        LevelAtlas la = _atlasPrefab.GetComponent<LevelAtlas>();
        foreach(Scene scene in args.LoadedScenes) {
            SceneDelegateDebug($"{(base.IsServer ? "Server" : "Client")} loaded scene " + scene.name + ", handle: " + scene.handle);

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

            // Scene loaded, add it to loadedScenes dictionary, also done in SceneManager_SceneLoaded
            loadedScenes.Add(sceneLookupData, scene);
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
            SceneDelegateDebug($"RegisterScenes skipped {args.SkippedSceneNames.Length} scene(s).");
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

    /// <summary>
    /// Look for a GameplayManager in a scene
    /// </summary>
    private GameplayManager LocateGameplayManager(Scene scene) 
    {
        foreach(GameObject obj in scene.GetRootGameObjects()) {
            obj.TryGetComponent(out GameplayManager testGameplayManager);
            if (testGameplayManager != null)
                return testGameplayManager;
        }
        return null;
    }

    /// <summary>
    /// Checks for a GameplayManager in the same scene as the provided GameplayManagerBehavior script.
    /// If one is found, it's returned and the GameplayManagerLoaded interface method gets called.
    /// 
    /// Use subscribeCaller = true to ensure that the GameplayManagerLoadedScript will be called 
    /// </summary>
    public bool GetGameplayManager(GameplayManagerBehavior gameplayManagerBehavior) 
    {
        if(gameplayManagerBehavior is not MonoBehaviour) {
            Debug.LogError("A GameplayManagerBehavior interface is on a script that isn't a Monobehavior!");
            return false;
        }
        Scene objectsScene = (gameplayManagerBehavior as MonoBehaviour).gameObject.scene;
        SceneLookupData lookupData = new(objectsScene.handle, objectsScene.name);
        if(!loadedScenes.ContainsKey(lookupData)) 
            return false;

        GameplayManager toReturn = null;
        if(gameplayManagers.ContainsKey(lookupData)) {
            toReturn = gameplayManagers[lookupData];
        } else {
            toReturn = LocateGameplayManager(loadedScenes[lookupData]);
            if(toReturn != null) 
                gameplayManagers.Add(lookupData, toReturn);
        }

        if(toReturn == null)
            return false;
        
        gameplayManagerBehavior.GameplayManagerLoaded(toReturn);
        return true;
    }

    public void SubscribeForGameplayManager(GameplayManagerBehavior gameplayManagerBehavior) 
    {
        // If we can get the GameplayManager right away do it so we don't have to waste time waiting for the next Update() call.
        if(GetGameplayManager(gameplayManagerBehavior))
            return;
        waitingForGameplayManagers.Add(gameplayManagerBehavior);
    }

    private List<GameplayManagerBehavior> waitingForGameplayManagers = new();
    private void Update() 
    {
        if(waitingForGameplayManagers.Count == 0)
            return;
        List<GameplayManagerBehavior> toRemove = new();
        foreach(GameplayManagerBehavior gmb in waitingForGameplayManagers) {
            if(GetGameplayManager(gmb))
                toRemove.Add(gmb);
        }
        toRemove.ForEach(gmb => waitingForGameplayManagers.Remove(gmb));
    }

    private static bool sendSceneDelegateDebug = true;
    public static void SceneDelegateDebug(string message) {
        if(sendSceneDelegateDebug)
            print(message);
    }

    public LevelAtlas LevelAtlas { get { return _atlasPrefab.GetComponent<LevelAtlas>(); } }

}
