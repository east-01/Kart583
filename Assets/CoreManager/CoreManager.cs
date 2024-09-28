using System;
using System.Collections.Generic;
using AClockworkBerry;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using GameKit.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SocialPlatforms;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
[RequireComponent(typeof(AudioManager))]
[RequireComponent(typeof(GameplayManagerDelegate))]
[RequireComponent(typeof(LobbyCommunicator))]
[RequireComponent(typeof(TransitionManager))]
[RequireComponent(typeof(BLog))]
public class CoreManager : MonoBehaviour
{

    /* On-component references */
    public static CoreManager Instance;
    public static AudioManager AudioManager { get { return Instance.audioManager; } }
    public static GameplayManagerDelegate GameplayManagerDelegate { get { return Instance.gameplayManagerDelegate; } }
    public static LobbyCommunicator LobbyCommunicator { get { return Instance.lobbyCommunicator; } }
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }

    /* Child component references */
    public static OptionsMenuController OptionsMenuController => Instance.optionsMenuController;
    public static EventSystem EventSystem => Instance.eventSystem;
    public static InputSystemUIInputModule InputSystemUIInputModule => Instance.inputSystemUIInputModule;
    public static InputActionAsset UIInputActionAsset => Instance.uiInputActionAsset;

    /* Atlas prefab access*/
    public static LevelAtlas LevelAtlas { get { return Instance.atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public static KartAtlas KartAtlas { get { return Instance.atlasesPrefab.GetComponent<KartAtlas>(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.atlasesPrefab.GetComponent<ItemAtlas>(); } }
    public static AudioAtlas AudioAtlas { get { return Instance.atlasesPrefab.GetComponent<AudioAtlas>(); } }
    public static AudioClipPackage AudioClipPackage(AudioFile file) { return Instance.atlasesPrefab.GetComponent<AudioAtlas>().clips[(int)file]; }
    public static AudioClip AudioClip(AudioFile file) { return AudioClipPackage(file).audioClip; }

    /* Utility references */
    public static NetworkStateManager NetworkStateManager { get { return InstanceFinder.NetworkManager.GetComponent<NetworkStateManager>(); } }
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

    private AudioManager audioManager;
    private GameplayManagerDelegate gameplayManagerDelegate;
    private LobbyCommunicator lobbyCommunicator;
    private TransitionManager transitionManager;
    private BLog bLog;
    private EventSystem eventSystem;
    private InputSystemUIInputModule inputSystemUIInputModule;
    [SerializeField] private InputActionAsset uiInputActionAsset;

    [SerializeField] private OptionsMenuController optionsMenuController;

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

        if(!notifiedOfRelease && !DevSettings.IsDevelopment()) {
            Debug.Log($"<color=aqua>Running release build {DevSettings.GetVersionString()}</color>");
            notifiedOfRelease = true;
        }

        audioManager = GetComponent<AudioManager>();
        gameplayManagerDelegate = GetComponent<GameplayManagerDelegate>();
        lobbyCommunicator = GetComponent<LobbyCommunicator>();
        transitionManager = GetComponent<TransitionManager>();
        bLog = GetComponent<BLog>();
        eventSystem = GetComponentInChildren<EventSystem>();
        inputSystemUIInputModule = GetComponentInChildren<InputSystemUIInputModule>();

        CheckNetworkManager();
        CheckSceneController();
        CheckNetSceneController();
        CheckPlayerObjectManager();
        CheckScreenLogger();
    }

    private void Start() 
    {
        optionsMenuController.gameObject.SetActive(true);
        optionsMenuController.Close();
        optionsMenuController.LoadOptions();

        HandleDeveloperSettings();
    }

    private void OnDestroy() 
    {
        optionsMenuController.SaveOptions();
    }

    private void Update() 
    {
        CheckNetworkManager();
        CheckNetSceneController();

        DeveloperSettingsUpdate();
    }

#region Essential component checks
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
#endregion

#region Developer Settings
    public void HandleDeveloperSettings() 
    {

        DevSettings.SettingsPrintout.ForEach(s => BLog.Log(s, LogChannel.DevSettings));

        // If we're automatically running as server we don't want to do anything else, so return
        // The actual start call is in NetworkStateManager#Start
        if(DevSettings.Settings.HaveStandalonePlayerRunAsServer && !Application.isEditor)
            return;

        if(DevSettings.Settings.LoadMode != LoadMode.NONE)
            SimulateLoad();
    }

    /// <summary> Call from Update() to update developer settings. </summary>
    public void DeveloperSettingsUpdate() 
    {
        if(IsLocal && DevSettings.Settings.EnableWarpPoint) {
            bool savePressed = Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N);
            bool loadPressed = Input.GetKey(KeyCode.LeftCommand) && Input.GetKeyDown(KeyCode.M);
            if(savePressed || loadPressed &&
               PlayerObjectManager.Instance != null && PlayerObjectManager.Instance.PlayerOne != null) {
                static KartManager PlayerOneKartManager() {
                    List<GameObject> kartObjects = NetSceneController.LobbyManager.GetLobby(LocalConnection).GameplayManager.PlayerManager.kartObjects;
                    foreach(GameObject kartObject in kartObjects) {
                        KartManager km = KartBehavior.LocateManager(kartObject);
                        if(km == null) {
                            Debug.LogError("Failed to get KartManager from kartObject");
                            continue;
                        }
                        if(km.PlayerData.uuid == PlayerObjectManager.Instance.PlayerOne.data.uuid)
                            return km;
                    }
                    return null;
                }

                if(savePressed) {
                    DevSettings.Settings.WarpPosition = PlayerOneKartManager().transform.position;
                } else if(loadPressed) {
                    PlayerOneKartManager().transform.SetPosition(false, DevSettings.Settings.WarpPosition);
                }
            }
        }
    }

    /// <summary>
    /// When loadMode != LoadMode.NONE this will load the user into a map either in local play or multiplayer depending on load mode.
    /// For multiplayer load mode, there must be a server instance running to recieve the player.
    /// </summary>
    public void SimulateLoad() 
    {
        if(DevSettings.Settings.LoadMode == LoadMode.NONE) {
            Debug.LogError("Tried to simulate load but the LoadMode was set to NONE.");
            return;
        }

        CoreManager.Instance.isMultiplayer = DevSettings.Settings.LoadMode == LoadMode.LOAD_LOBBY;

        if(DevSettings.Settings.LoadMode == LoadMode.LOAD_LOBBY) {
            CoreManager.TransitionManager.LoadScene(SceneNames.MENU_LOBBY);
        } else if(DevSettings.Settings.LoadMode == LoadMode.LOAD_MAP_LOCAL) {
            KartLevel mapPick = DevSettings.Settings.OverrideMapPick ? DevSettings.Settings.Map : LevelAtlas.PickRandomLevel();
            if(!DevSettings.Settings.OverrideMapPick) 
                BLog.Log($"SimulateLoad: loading into local play map but override map pick is off, picked {mapPick} randomly.", LogChannel.DevSettings, 0);
            string sceneName = CoreManager.LevelAtlas.RetrieveData(mapPick).sceneName;
            CoreManager.TransitionManager.LoadScene(sceneName);
        }

        DevSettings.Settings.hasProcessedLoadMode = true;
    }
#endregion

    /// <summary>
    /// Check if the running instance is a server instance. More reliable than InstanceFinder because 
    ///   it will handle cases where a NetworkManager doesn't exist.
    /// </summary>
    public static bool IsServerOnly { get {
        if(NetworkManager.Instances.Count == 0) return false;
        return InstanceFinder.IsServer && !InstanceFinder.IsHost;
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
        if(DevSettings.Settings.OverridePlayerLimit)
            return DevSettings.Settings.PlayerLimit;
        else
            return playerLimit;
    } }
}
