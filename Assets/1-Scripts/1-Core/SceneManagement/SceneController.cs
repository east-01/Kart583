using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// The Scene Controller is a core level object that manages scenes, it also interfaces
///   with the NetSceneController (when connected to the network) to allow for server-side
///   scene actions.
/// It essentially acts as the client side for the NetSceneController.
/// Events are called from here, so we'll be able to trust that the SceneController's events
///   we subscribe to will stay the same even if the network changes.
/// </summary>
public class SceneController : MonoBehaviour
{

    public static SceneController Instance;
    private NetSceneController nsc { get { return NetSceneController.Instance; } }

    /// <summary>
    /// Client side only, the data that we're trying to get the client to load
    /// </summary>
    public SceneLookupData clientLoadTarget;

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

    /// <summary>SHOULD ONLY BE USED BY NetSceneController!! Will issue SceneController events.</summary>
    public void NetSceneController_InvokeSceneRegisteredEvent(SceneLookupData sceneLookupData) { SceneRegisteredEvent?.Invoke(sceneLookupData); }
    /// <summary>SHOULD ONLY BE USED BY NetSceneController!! Will issue SceneController events.</summary>
    public void NetSceneController_InvokeSceneWillDeregisterEvent(SceneLookupData sceneLookupData) { SceneWillDeregisterEvent?.Invoke(sceneLookupData); }
    /// <summary>SHOULD ONLY BE USED BY NetSceneController!! Will issue SceneController events.</summary>
    public void NetSceneController_InvokeSceneDeregisteredEvent(SceneLookupData sceneLookupData) { SceneDeregisteredEvent?.Invoke(sceneLookupData); }
    /// <summary>SHOULD ONLY BE USED BY NetSceneController!! Will issue SceneController events.</summary>
    public void NetSceneController_InvokeClientAddedToSceneEvent(NetworkConnection client, SceneLookupData sceneLookupData) { ClientAddedToSceneEvent?.Invoke(client, sceneLookupData); }
#endregion

#region Initializers
    void Awake() 
    {
        BLog.Log("SceneController woke up", LogChannel.SceneDelegate, 0);
        if(Instance != null)
            throw new InvalidOperationException("Tried to create a new SceneDelegate when one already exists.");

        Instance = this;
        DontDestroyOnLoad(this);
    }

    private void OnEnable() 
    { 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += UnitySceneManager_SceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += UnitySceneManager_SceneUnloaded;
    }

    private void OnDisable() { 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= UnitySceneManager_SceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= UnitySceneManager_SceneUnloaded;
    }
#endregion

#region Scene Loading
    /// <summary>
    /// Load a scene for the client using the UnityEngine SceneManager.
    /// Will only be tracked by the SceneManager if shouldTrack = true
    /// </summary>
    /// <param name="shouldTrack">Track the scene in the scene manager</param>
    public void LoadScene(SceneLookupData lookupData, bool shouldTrack) 
    {
        // Call will deregister event for the active scene
        BLog.Log($"Loading scene \"{lookupData}\" as client, shouldTrack: {shouldTrack}", LogChannel.SceneDelegate, 0);
        if(NetSceneController.IsReady)
            BLog.Log($"LoadSceneAsClient: Is active scene \"{ActiveSceneLookupData}\" registered: {nsc.IsSceneRegistered(ActiveSceneLookupData)}", LogChannel.SceneDelegate, 2);
        if(NetSceneController.IsReady && nsc.IsSceneRegistered(ActiveSceneLookupData))
            SceneWillDeregisterEvent?.Invoke(ActiveSceneLookupData);

        clientLoadTarget = shouldTrack ? lookupData : null;

        UnityEngine.SceneManagement.SceneManager.LoadScene(lookupData.Name, LoadSceneMode.Single);
    }
#endregion

#region Event Handlers
    /// <summary>
    /// The client side scene manager load event
    /// </summary>
    private void UnitySceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the server side of this event
        if(InstanceFinder.IsServerOnly)
            return;
        if(!NetSceneController.IsReady)
            return;

        BLog.Log("LoadedScenes#UnitySceneManager_SceneLoaded: Validated client loaded, scene. Disconnecting them from their other scenes.", LogChannel.SceneDelegate, 0);
        nsc.ServerRpcRemoveClientFromScene(CoreManager.LocalConnection);

        // If the client load target is null, we're not tracking this scene load
        if(clientLoadTarget == null)
            return;

        if(scene != null && clientLoadTarget is not null && scene.name != clientLoadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }

        // Only register the scene if we're not in a local instance, this is because the FishNet register method will do it
        if(!CoreManager.IsLocal)
            nsc.RegisterScene(scene);

        BLog.Log($"SceneDelegate#UnitySceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"", LogChannel.SceneDelegate, 0);
        BLog.Highlight($"Calling serverrpc client loaded scene with loadTarget={clientLoadTarget}");
        nsc.ServerRpcClientLoadedScene(CoreManager.LocalConnection, clientLoadTarget);
    }

    private void UnitySceneManager_SceneUnloaded(Scene scene) 
    {
        if(!NetSceneController.IsReady)
            return;
        SceneLookupData lookupData = new(scene.handle, scene.name);
        if(!nsc.IsSceneRegistered(lookupData))
            return;
        nsc.DeregisterScene(lookupData);
    }
#endregion

    private SceneLookupData ActiveSceneLookupData { get { 
        Scene active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return new(active.handle, active.name);
    } }

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