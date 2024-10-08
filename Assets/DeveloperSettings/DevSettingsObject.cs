using System.Collections.Generic;
using EMullen.Core;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using FishNet;
using GameKit.Dependencies.Utilities;
using UnityEngine;

public class DevSettingsObject : MonoBehaviour 
{

    public static DevSettingsObject Instance;

    [SerializeField]
    public BLogChannel logSettings;
    public BLogChannel LogSettings;

    private void Awake() 
    {
        if(Instance != null) {
            Debug.LogWarning($"New LobbyCommunicator woke up while one already exists. Destroying gameObject \"{gameObject.name}\"");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() 
    {
        HandleDeveloperSettings();
    }

    private void Update() 
    {
        if(CoreManager.IsLocal && DevSettings.Settings.EnableWarpPoint) {
            bool savePressed = Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N);
            bool loadPressed = Input.GetKey(KeyCode.LeftCommand) && Input.GetKeyDown(KeyCode.M);
            if(savePressed || loadPressed &&
               PlayerManager.Instance != null && PlayerManager.Instance.LocalPlayers[0] != null) {
                static KartManager PlayerOneKartManager() {
                    List<GameObject> kartObjects = (LobbyManager.Instance.GetLobby(InstanceFinder.ClientManager.Connection) as KartLobby).gameplayManager.KartsIRManager.kartObjects;
                    foreach(GameObject kartObject in kartObjects) {
                        KartManager km = KartBehavior.LocateManager(kartObject);
                        if(km == null) {
                            Debug.LogError("Failed to get KartManager from kartObject");
                            continue;
                        }
                        if(km.OwnerUID == PlayerManager.Instance.LocalPlayers[0].GetPlayerData().GetUID())
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

    public void HandleDeveloperSettings() 
    {

        DevSettings.SettingsPrintout.ForEach(s => BLog.Log(s, LogSettings));

        // If we're automatically running as server we don't want to do anything else, so return
        // The actual start call is in NetworkStateManager#Start
        if(DevSettings.Settings.HaveStandalonePlayerRunAsServer && !Application.isEditor)
            return;

        if(DevSettings.Settings.LoadMode != LoadMode.NONE)
            SimulateLoad();
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
                BLog.Log($"SimulateLoad: loading into local play map but override map pick is off, picked {mapPick} randomly.", LogSettings, 0);
            string sceneName = CoreManager.LevelAtlas.RetrieveData(mapPick).sceneName;
            CoreManager.TransitionManager.LoadScene(sceneName);
        }

        DevSettings.Settings.hasProcessedLoadMode = true;
    }
}