using System;
using AClockworkBerry;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
[RequireComponent(typeof(DevSettings))]
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(LobbyCommunicator))]
[RequireComponent(typeof(TransitionManager))]
[RequireComponent(typeof(BLog))]
public class CoreManager : MonoBehaviour
{

    public static CoreManager Instance;
    public static DevSettings DevSettings { get { return Instance.devSettings;} }
    public static AudioManager AudioManager { get { return Instance.audioManager; } }
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance.gameplayManagerDelegate; } }
    public static LobbyCommunicator LobbyCommunicator { get { return Instance.lobbyCommunicator; } }
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }

    public static NetworkStateManager NetworkStateManager { get { return InstanceFinder.NetworkManager.GetComponent<NetworkStateManager>(); } }
    public static LevelAtlas LevelAtlas { get { return Instance.atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public static KartAtlas KartAtlas { get { return Instance.atlasesPrefab.GetComponent<KartAtlas>(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.atlasesPrefab.GetComponent<ItemAtlas>(); } }
    public static AudioAtlas AudioAtlas { get { return Instance.atlasesPrefab.GetComponent<AudioAtlas>(); } }
    public static AudioClip AudioClip(AudioFile file) { return Instance.atlasesPrefab.GetComponent<AudioAtlas>().clips[(int)file]; }

    public static NetworkConnection LocalConnection { get { return NetSceneController.IsReady ? NetSceneController.GetLocalConnection() : null; } }
    public static bool HasLocalConnection { get { return NetSceneController.IsReady; } }

    [Header("Prefabs"), SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject sceneControllerPrefab;
    [SerializeField] private GameObject netSceneControllerPrefab;
    [SerializeField] private GameObject playerObjectManagerPrefab;
    [SerializeField] private GameObject screenLoggerPrefab;
    [SerializeField] private GameObject atlasesPrefab;

    [Header("Settings")] public bool isMultiplayer;
    [SerializeField] private int playerLimit = 8;

    private DevSettings devSettings;
    private AudioManager audioManager;
    private GameplayManagerDelegate gameplayManagerDelegate;
    private LobbyCommunicator lobbyCommunicator;
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
        audioManager = GetComponent<AudioManager>();
        gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        lobbyCommunicator = GetComponent<LobbyCommunicator>();
        transitionManager = GetComponent<TransitionManager>();
        bLog = GetComponent<BLog>();

        CheckNetworkManager();
        CheckSceneController();
        CheckNetSceneController();
        CheckPlayerObjectManager();
        CheckScreenLogger();
    }

    private void Update() 
    {
        CheckNetworkManager();
        CheckNetSceneController();
    }

    private void CheckNetworkManager() 
    {
        if(NetworkManager.Instances.Count > 0)
            return;
        
        Instantiate(networkManagerPrefab);
    }

    private void CheckSceneController() 
    {
        if(sceneControllerPrefab == null) {
            Debug.LogWarning("Scene controller prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        if(SceneController.Instance != null)
            return;

        GameObject go = Instantiate(sceneControllerPrefab);
        go.name = "SceneController";
    }

    private void CheckNetSceneController() 
    {
        if(sceneControllerPrefab == null) {
            Debug.LogWarning("Network scene controller prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        if(NetSceneController.Instance != null)
            return;
        // Check if there's a NetworkManager in place and the server is started
        if(NetworkManager.Instances.Count <= 0 || InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started || !InstanceFinder.IsServer)
            return;

        GameObject go = Instantiate(netSceneControllerPrefab);
        InstanceFinder.ServerManager.Spawn(go);

        go.GetComponent<NetSceneController>().CheckInitialGlobalScene();
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
