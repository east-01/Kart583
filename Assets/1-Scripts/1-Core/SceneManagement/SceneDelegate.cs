using System;
using System.Collections;
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

    private GameplayManagerDelegate _gameplayManagerDelegate;
    private LobbyManager _lobbyManager;
    [SerializeField]
    private GameObject _atlasPrefab;

    [SerializeField]
    private Dictionary<SceneLookupData, SceneElements> loadedScenes = new();
    [SerializeField]
    private List<SceneElements> loadedScenesList = new();

    /// <summary>
    /// Client side only, the data that we're trying to get the client to load
    /// </summary>
    private SceneLookupData clientLoadTarget;

#region Events
    public delegate void SceneRegisteredHandler(SceneLookupData sceneLookupData); // We don't provide SceneElements here to require users of event to go through SceneDelegate
    /// <summary>
    /// Called when a scene is registered with the SceneDelegate
    /// </summary>
    public event SceneRegisteredHandler SceneRegisteredEvent;

    public delegate void SceneWillDeregisterHandler(SceneLookupData sceneLookupData); // No SceneElements here, see scene registered handler
    /// <summary>
    /// Called when a scene is told to unload on the server but before the unload actually happens.
    /// In place to allow things in the scene to wrap up properly.
    /// </summary>
    public event SceneWillDeregisterHandler SceneWillDeregisterEvent;

    public delegate void SceneDeregisteredHandler(SceneLookupData sceneLookupData); // No SceneElements here, see scene registered handler
    /// <summary>
    /// Called when a scene is deregistered with the scene delegate;
    /// </summary>
    public event SceneDeregisteredHandler SceneDeregisteredEvent;

    public delegate void ClientAddedToSceneHandler(NetworkConnection client, SceneLookupData sceneLookupData);
    /// <summary>
    /// Called when a client is added to the scene.
    /// For now, only is called on the client that was added.
    /// </summary>
    public event ClientAddedToSceneHandler ClientAddedToSceneEvent;
#endregion

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
        InstanceFinder.SceneManager.OnUnloadEnd += FishSceneManager_SceneUnloaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += UnitySceneManager_SceneLoaded;

        StartCoroutine(RefreshLoadedScenesListTask());
    }

    private void OnDisable() { 
        if(InstanceFinder.SceneManager != null) {
            InstanceFinder.SceneManager.OnLoadEnd -= FishSceneManager_SceneLoaded; 
            InstanceFinder.SceneManager.OnUnloadEnd -= FishSceneManager_SceneUnloaded;
        }
    }
#endregion

#region Scene Loading/Unloading
    /* Loading */
    [Server]
    public void LoadSceneAsServer(SceneLookupData lookupData) 
    {
        SceneLoadData sld = new SceneLoadData(lookupData);
        sld.Options.AllowStacking = true;
        sld.Options.AutomaticallyUnload = false;
        sld.ReplaceScenes = ReplaceOption.All;

        base.SceneManager.LoadConnectionScenes(sld);
        SceneDelegateDebug($"Telling server to load scene w/ data name: {lookupData.Name} handle: {lookupData.Handle}");
    }

    [Client]
    public void LoadSceneAsClient(SceneLookupData lookupData) 
    {
        clientLoadTarget = lookupData;
        UnityEngine.SceneManagement.SceneManager.LoadScene(lookupData.Name, LoadSceneMode.Single);
        SceneDelegateDebug($"Client calling load scene \"{lookupData.Name}\"");
    }

    [TargetRpc]
    public void TargetRpcLoadSceneAsClient(NetworkConnection client, SceneLookupData lookupData) 
    {
        LoadSceneAsClient(lookupData);
    }

    /* Unloading */
    [Server]
    public void UnloadSceneAsServer(SceneLookupData lookupData) 
    {
        SceneWillDeregisterEvent?.Invoke(lookupData);

        SceneUnloadData sud = new(lookupData);
        base.SceneManager.UnloadConnectionScenes(sud);
        SceneDelegateDebug($"Telling server to unload scene w/ data name: {lookupData.Name} handle: {lookupData.Handle}");
    } 
#endregion

#region Event Handlers
    /// <summary>
    /// The server side scene manager load event
    /// </summary>
    private void FishSceneManager_SceneLoaded(SceneLoadEndEventArgs args)
    {
        foreach(Scene scene in args.LoadedScenes) {
            SceneDelegate.SceneDelegateDebug($"{(base.IsServer ? "Server" : "Client")} loaded scene " + scene.name + ", handle: " + scene.handle);
            RegisterScene(scene);
        }

        // Disable event systems
        if(base.IsServer) {
            int disabledEventSystems = 0;
            foreach(EventSystem system in FindObjectsOfType<EventSystem>()) {
                system.enabled = false;
                disabledEventSystems++;
            }
            SceneDelegateDebug($"RegisterScenes disabled {disabledEventSystems} event system(s).");
        }

        if(args.SkippedSceneNames.Length > 0)
            SceneDelegateDebug($"RegisterScenes skipped {args.SkippedSceneNames.Length} scene(s).");
    }

    /// <summary>
    /// The client side scene manager load event
    /// </summary>
    private void UnitySceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the servers UnityEngine SceneManager. That's for RegisterScenes.
        if(!base.IsClient)
            return;

        if(scene != null && clientLoadTarget is not null && scene.name != clientLoadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }

        RegisterScene(scene);

        NetworkConnection client = base.LocalConnection;
        SceneDelegateDebug("LoadedScenes#UnitySceneManager_SceneLoaded: Validated client loaded, scene. Disconnecting them from their other scenes.");
        foreach(Scene otherScene in client.Scenes) {
            if(otherScene != scene) {
                SceneDelegateDebug($"LoadedScenes#SceneManager_SceneLoaded: Clearing other scene {otherScene.name}/{otherScene.handle} from client");
                base.SceneManager.RemoveConnectionsFromScene(new NetworkConnection[] { client }, otherScene);
            }
        }

        ServerRpcClientLoadedScene(base.LocalConnection, clientLoadTarget);

        SceneDelegateDebug($"SceneDelegate#UnitySceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"");
    }

    /// <summary>
    /// Server side scene unload event.
    /// </summary>
    private void FishSceneManager_SceneUnloaded(SceneUnloadEndEventArgs args) 
    {
        foreach(UnloadedScene scene in args.UnloadedScenesV2) {
            DeregisterScene(new(scene.Handle, scene.Name));
        }
    }
#endregion

#region Scene Registration
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

    private void DeregisterScene(SceneLookupData lookupData) 
    {
        if(!IsSceneRegistered(lookupData)) {
            Debug.LogWarning($"Failed to dereigster scene \"{lookupData}\". It is not registered.");
            return;
        }

        loadedScenes.Remove(lookupData);

        SceneDelegateDebug($"Deregistered scene \"{lookupData}\". Calling event.");
        SceneDeregisteredEvent?.Invoke(lookupData);
    }

    IEnumerator RefreshLoadedScenesListTask() 
    {
        while(gameObject.activeSelf) {
            RefreshLoadedScenesList();
            yield return new WaitForSeconds(1f);
        }
    }

    private void RefreshLoadedScenesList() 
    {
        loadedScenesList.Clear();
        foreach(SceneElements elements in loadedScenes.Values) {
            loadedScenesList.Add(elements);
        }
    }
#endregion

#region Client Movement
    /// <summary>
    /// Adds a client to a scene while removing them from other scenes they could be in.
    /// </summary>
    [Server]
    public void AddClientToScene(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        if(serverSceneLookupData == null) {
            Debug.LogError("Can't move client to scene because SceneLookupData is null.");
            return;
        }
        if(!IsSceneRegistered(serverSceneLookupData)) {
            Debug.LogError($"Tried to move client to scene \"{serverSceneLookupData}\" but the scene isn't loaded.");
            return;
        }

        // Remove from existing scene
        if(GetClientScene(client) is not null)
            RemoveClientFromScene(client);

        TargetRpcEnsureSceneLoaded(client, serverSceneLookupData);
    }

    /// <summary>
    /// This internal side of AddClient to scene actually performs the SceneManager#AddConnectionToScene
    ///   method and perfomrs the event call
    /// </summary>
    [Server]
    private void Internal_AddClientToScene(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        if(!Internal_AddClientToSceneElements(client, serverSceneLookupData)) {
            Debug.LogError("Failed to add client to new scene.");
            return;
        }

        base.SceneManager.AddConnectionToScene(client, loadedScenes[serverSceneLookupData].Scene);
        TargetRpcClientAddedToScene(client, serverSceneLookupData);
    }

    [Server]
    public void RemoveClientFromScene(NetworkConnection client) 
    {
        SceneLookupData currentScene = GetClientScene(client);
        if(currentScene is null) {
            Debug.LogError("Can't remove client from scene since the clients current scene is null.");
            return;
        }

        if(!loadedScenes.ContainsKey(currentScene)) {
            Debug.LogError($"Can't remove client from scene \"{currentScene}\" because it's not loaded.");
            return;
        }

        if(!Internal_RemoveClientFromSceneElements(client, currentScene))  {
            Debug.LogError($"Failed to remove client from scene \"{currentScene}\"");
            return;
        }

        SceneElements elements = loadedScenes[currentScene];

        base.SceneManager.RemoveConnectionsFromScene(new NetworkConnection[] {client}, elements.Scene);

        if(elements.ClientCount == 0 && elements.DeleteOnLastClientRemove)
            UnloadSceneAsServer(currentScene);
    }

    /// <summary>
    /// Issue ClientAddedToSceneEvent to corresponding client.
    /// </summary>
    [TargetRpc]
    private void TargetRpcClientAddedToScene(NetworkConnection client, SceneLookupData lookup) 
    {
        ClientAddedToSceneEvent?.Invoke(client, lookup);
    }

    /// <summary>
    /// Add a client to scene elements, returns success status.
    /// </summary>
    private bool Internal_AddClientToSceneElements(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        SceneElements elements = loadedScenes[serverSceneLookupData];
        if(elements.Clients.Contains(client)) {
            Debug.LogError($"Tried to add client to scene elements for scene \"{serverSceneLookupData}\" but they were already in the list!");
            return false;
        }

        elements.Clients.Add(client);
        loadedScenes[serverSceneLookupData] = elements;
        return true;
    }

    /// <summary>
    /// Removes a client from scene elements, returns success status.
    /// </summary>
    private bool Internal_RemoveClientFromSceneElements(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        SceneElements elements = loadedScenes[serverSceneLookupData];
        if(!elements.Clients.Contains(client)) {
            Debug.LogError($"Tried to remove client from scene elements for scene \"{serverSceneLookupData}\" but they weren't in the list!");
            return false;
        }

        elements.Clients.Remove(client);
        loadedScenes[serverSceneLookupData] = elements;
        return true;
    }
#endregion

#region Handshake
    /*NOTE: 
      During the handshake, we're passing around the SERVER SCENE LOOKUP DATA. This is
        an important distinction because the serverSceneLookupData has the reference for the
        corresponding SceneElements in the loadedScenes dictionary.
      The client only cares about the scene name since they'll load just a single scene,
        but the reference is important to keep. */

    /// <summary>
    /// Ensure that the specified SceneLookupData is loaded on the client.
    /// If the current scene name (on the client) matches the name in lookupData, we will skip
    ///   loading the scene and automatically call ServerRpcClientLoadedScene.
    /// If the current scene name does NOT match the name in lookup data, we will call
    ///   LoadSceneAsClient to load it.
    /// </summary>
    [TargetRpc]
    private void TargetRpcEnsureSceneLoaded(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        clientLoadTarget = null;
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != serverSceneLookupData.Name) {
            LoadSceneAsClient(serverSceneLookupData);
        } else {
            SceneDelegateDebug($"SceneDelegate#TargetRpcEnsureSceneLoaded: Scene \"{serverSceneLookupData.Name}\" is already loaded, skipping to SceneDelegate#ServerRpcClientLoadedScene");
            ServerRpcClientLoadedScene(base.LocalConnection, serverSceneLookupData);
        }
    }

    /// <summary>
    /// Signal to the server that the client has loaded the specified scene.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcClientLoadedScene(NetworkConnection client, SceneLookupData serverSceneLookupData) 
    {
        if(!IsSceneRegistered(serverSceneLookupData)) {
            Debug.LogError($"Client loaded scene but server doesn't have corresponding scene loaded. Lookup info: {serverSceneLookupData.Name} handle: {serverSceneLookupData.Handle}");
            return;
        }
        Internal_AddClientToScene(client, serverSceneLookupData);
    }
#endregion

#region Getters/Setters
    public bool IsSceneRegistered(SceneLookupData lookupData) { return loadedScenes.ContainsKey(lookupData); }
    public SceneElements GetSceneElements(SceneLookupData lookupData) 
    {
        if(!loadedScenes.ContainsKey(lookupData)) {
            Debug.LogError($"Failed to get SceneElements for lookupData \"{lookupData}\". Put an IsSceneLoaded() check before the method calling this.");
            return default;
        }
        return loadedScenes[lookupData];     
    }

    public void SetSceneElements(SceneLookupData lookupData, SceneElements newElements) 
    {
        if(!loadedScenes.ContainsKey(lookupData)) {
            Debug.LogError($"Can't set scene elements for lookupData \"{lookupData}\". The scene must be loaded for you to set it's elements.");
            return;
        }
        if(loadedScenes[lookupData].Scene != newElements.Scene) {
            Debug.LogError($"Can't set scene elements, scene mismatch.");
            return;
        }
        loadedScenes[lookupData] = newElements;
    }

    /// <summary>
    /// Looks for the scene that the client is in.
    /// </summary>
    public SceneLookupData GetClientScene(NetworkConnection client) 
    {
        foreach(SceneLookupData lookupData in loadedScenes.Keys) {
            SceneElements elements = loadedScenes[lookupData];
            if(elements.Clients.Contains(client))
                return lookupData;
        }
        return null;
    }
#endregion

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

    private static bool sendSceneDelegateDebug = false;
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
    [SerializeField]
    private SceneLookupData lookupData;

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

    [SerializeField]
    private List<NetworkConnection> clients;
    /// <summary>
    /// A list of clients in the scene
    /// </summary>
    public List<NetworkConnection> Clients {
        get {
            clients ??= new();
            return clients;
        }
    }
    [SerializeField]
    private int clientCount; // Exposed for serialization in editor
    /// <summary>
    /// The amount of clients in the scene
    /// </summary>
    public int ClientCount { get { 
        clientCount = Clients.Count;
        return clientCount; 
    } }

    /// <summary>
    /// When true, the SceneDelegate will delete the scene when the last player is removed from
    ///   the scene.
    /// </summary>
    public bool DeleteOnLastClientRemove;

}