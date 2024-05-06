using System;
using AClockworkBerry;
using FishNet;
using FishNet.Managing;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
[RequireComponent(typeof(DevSettings))]
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(TransitionManager))]
[RequireComponent(typeof(BLog))]
public class CoreManager : MonoBehaviour
{

    public static CoreManager Instance;
    public static DevSettings DevSettings { get { return Instance.devSettings;} }
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance.gameplayManagerDelegate; } }
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }
    public static BLog BLog { get { return Instance.bLog; } }

    public static NetworkStateManager NetworkStateManager { get { return InstanceFinder.NetworkManager.GetComponent<NetworkStateManager>(); } }
    public static LevelAtlas LevelAtlas { get { return Instance.atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public static KartAtlas KartAtlas { get { return Instance.atlasesPrefab.GetComponent<KartAtlas>(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.atlasesPrefab.GetComponent<ItemAtlas>(); } }

    [Header("Prefabs"), SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject sceneDelegatePrefab;
    [SerializeField] private GameObject playerObjectManagerPrefab;
    [SerializeField] private GameObject screenLoggerPrefab;
    [SerializeField] private GameObject atlasesPrefab;

    [Header("Settings")] public bool isMultiplayer;
    [SerializeField] private int playerLimit = 8;

    private DevSettings devSettings;
    private GameplayManagerDelegate gameplayManagerDelegate;
    private TransitionManager transitionManager;
    private BLog bLog;

    private bool notifiedOfRelease = false;

    private void Awake() 
    {
        if(Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if(!notifiedOfRelease && !GameVersion.IsDevelopment) {
            Debug.Log($"<color=aqua>Running release build {GameVersion.Version}</color>");
            notifiedOfRelease = true;
        }

        devSettings = GetComponent<DevSettings>();
        gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        transitionManager = GetComponent<TransitionManager>();
        bLog = GetComponent<BLog>();

        CheckNetworkManager();
        CheckSceneDelegate();
        CheckPlayerObjectManager();
        CheckScreenLogger();
    }

    private void Update() 
    {
        CheckNetworkManager();
        CheckSceneDelegate();
    }

    private void CheckNetworkManager() 
    {
        if(NetworkManager.Instances.Count > 0)
            return;
        
        Instantiate(networkManagerPrefab);
    }

    private void CheckSceneDelegate() 
    {
        // The client can't spawn it's own scene delegate
        if(InstanceFinder.IsClientOnly)
            return;
        if(sceneDelegatePrefab == null) {
            Debug.LogWarning("Scene delegate prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        // BLog.Highlight($"Checking scene delegate: is instance null: {SceneDelegate.Instance == null}");
        // if(SceneDelegate.Instance != null) {
        //     // BLog.Highlight($"Checking scene delegate: is spawned: {SceneDelegate.Instance.NetworkObject.IsSpawned}, is this instance a server: {InstanceFinder.IsServer}");
        //     if(!SceneDelegate.Instance.NetworkObject.IsSpawned && InstanceFinder.IsServer) {
        //         InstanceFinder.ServerManager.Spawn(SceneDelegate.Instance.NetworkObject);
        //     }
        //     return;
        // }
        // Check if there's a NetworkManager in place and the server is started
        if(NetworkManager.Instances.Count <= 0 || InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started || !InstanceFinder.IsServer)
            return;

        GameObject go = Instantiate(sceneDelegatePrefab);
        InstanceFinder.ServerManager.Spawn(go);

        go.name = "SceneDelegate";
        go.GetComponent<SceneDelegate>().CheckInitialGlobalScene();
    }

    private void CheckPlayerObjectManager() 
    {
        if(PlayerObjectManager.Instance != null)
            return;

        Instantiate(playerObjectManagerPrefab);
    }

    private void CheckScreenLogger() 
    {
        if(ScreenLogger.Instance != null)
            return;

        Instantiate(screenLoggerPrefab);
    }

    /// <summary>
    /// Check if the running instance is a server instance. More reliable than InstanceFinder because 
    ///   it will handle cases where a NetworkManager doesn't exist.
    /// </summary>
    public bool IsServer { get {
        if(NetworkManager.Instances.Count == 0) return false;
        return InstanceFinder.IsServer;
    } }
    public static bool IsMultiplayer { 
        get { return Instance.isMultiplayer; } 
        set { Instance.isMultiplayer = value;}
    }
    public static bool IsLocal { 
        get { return !IsMultiplayer; }
        set { IsMultiplayer = !value;}
    }

    public int PlayerLimit { get {
        if(DevSettings.OverridePlayerLimit)
            return DevSettings.playerLimit;
        else
            return playerLimit;
    } }

}
