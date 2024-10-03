using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/** The gameplay manager is responsible for general game loop mgmt.
  * It will check if everything is in order on Start(), printing out an error if it fails.
  * Other classes will refer to this to access game data. */
[RequireComponent(typeof(RaceManager))]
[RequireComponent(typeof(KartsIRManager))]
[RequireComponent(typeof(ItemManager))]
public class GameplayManager : NetworkBehaviour
{

    [Header("Prefabs")]
    [SerializeField]
    private GameObject playerObjectManagerPrefab;
	[SerializeField]
	private BLogChannel logSettings;
    public BLogChannel LogSettings => logSettings;

    [Header("Settings")]
    public bool showWarnings = false;
    public bool showBotPathWarnings = false;
    public bool playStartAnimation = true;
    public float startAnimationDuration = 3.0f;

    [Header("Runtime")]
    public bool ready;

    public RaceManager RaceManager { get; private set; }
    public KartsIRManager KartsIRManager { get; private set; }
    public ItemManager ItemManager { get; private set; }
    public KartLevelManager KartLevelManager { get; private set; }

    public KartLobby KartLobby { get; private set; }
    private readonly SyncVar<string> lobbyID = null;
    public bool HasLobby => lobbyID.Value != null;

    /* Late Lobby start process. */
    private LateLobbySpawnStep spawnStep = LateLobbySpawnStep.NONE;
    private LateLobbySpawnStep SpawnStep { 
        get { return spawnStep;} 
        set { 
            SpawnStepChanged(spawnStep, value);
            spawnStep = value; 
        }
    }

    void Awake() 
    {

        List<string> problems = new();
        List<string> warnings = new();

        // Load everything
        RaceManager = GetComponent<RaceManager>();
        KartsIRManager = GetComponent<KartsIRManager>();
        ItemManager = GetComponent<ItemManager>();

        // Checking HasProcessedLoadMode is important for a Game scene that loads and then instantly unloads
        // i.e. Running TEST_TRACK as the editor scene, then DevSettings instantly loads a TEST_TRACK on top
        if(DevSettings.Settings.LoadMode != LoadMode.NONE && !DevSettings.Settings.hasProcessedLoadMode)
            return;

        if(!LobbyCommunicator.Instance.InLobby && CoreManager.IsLocal) {
            SpawnStep = LateLobbySpawnStep.STARTING_CONNECTION;
        }

        // Initialize KartLevelManager
        KartLevelManager klm = FindObjectOfType<KartLevelManager>();
        if(klm != null) {
            KartLevelManager = klm;
            (List<string>, List<string>) problemsWarnings = KartLevelManager.Initialize();
            problemsWarnings.Item1.ForEach(problem => problems.Add(problem));
            problemsWarnings.Item2.ForEach(warning => warnings.Add(warning));
        } else {
            problems.Add("Failed to find KartLevelManager!");
        }

        // Check if everything is in order
        if(warnings.Count > 0 && showWarnings) {
            Debug.Log("GameplayManager experienced " + warnings.Count + " warning(s).");
            warnings.ForEach(warning => Debug.LogError(" - " + warning));
        }

        ready = problems.Count == 0;
        if(!ready) {
            Debug.LogError("Failed to start GameplayManager. " + problems.Count + " problem(s).");
            problems.ForEach(problem => Debug.LogError(" - " + problem));
        }

    }

    private void OnEnable() 
    {
        SceneController.Instance.ClientAddedToSceneEvent += SceneDelegate_ClientAddedToSceneEvent;
    }

    private void OnDisable() 
    {
        SceneController.Instance.ClientAddedToSceneEvent -= SceneDelegate_ClientAddedToSceneEvent;
    }

    private void Update() {
        if(SpawnStep != LateLobbySpawnStep.NONE) {
            if(SpawnStep == LateLobbySpawnStep.STARTING_CONNECTION && base.IsHostInitialized) {
                SpawnStep = LateLobbySpawnStep.CREATING_LOBBY;
            } else if(SpawnStep == LateLobbySpawnStep.CREATING_LOBBY && LobbyManager.Instance.LobbyCount > 0) {
                SpawnStep = LateLobbySpawnStep.REGISTERING_MAP;
            } else if(SpawnStep == LateLobbySpawnStep.REGISTERING_MAP && KartLobby != null) {
                SpawnStep = LateLobbySpawnStep.WAITING_FOR_PLAYER;
            } else if(SpawnStep == LateLobbySpawnStep.WAITING_FOR_PLAYER && PlayerManager.Instance.PlayerCount > 0) {
                SpawnStep = LateLobbySpawnStep.MOVING_TO_SCENE;
            }
        }
    }

    private void SpawnStepChanged(LateLobbySpawnStep prev, LateLobbySpawnStep current) {
        BLog.Log($"Spawn step changed to {current}", LogSettings, 1);
        if(current == LateLobbySpawnStep.STARTING_CONNECTION) {
            BLog.Log($"No lobby existed when joining map. AutoSpawning a local one.", DevSettingsObject.Instance.LogSettings);
            CoreManager.IsLocal = true;
            LobbyCommunicator.Instance.StartCommunication();
        } else if(current == LateLobbySpawnStep.CREATING_LOBBY) {
            LobbyManager.Instance.CreateLobby();
        } else if(current == LateLobbySpawnStep.REGISTERING_MAP) {
            BLog.Log($"Spawn step- Registering scene with {LobbyManager.Instance.LobbyCount} lobbies", LogSettings, 1);
            // This is called because neither of the SceneRegistered events will be able to catch unity's scene load.
            NetSceneController.Instance.RegisterScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        } else if(current == LateLobbySpawnStep.MOVING_TO_SCENE) {
            KartLobby.Add(PlayerManager.Instance.LocalPlayers[0].UID);
            KartLobby.MovePlayersToMap();
        }
    }

    private void SceneDelegate_ClientAddedToSceneEvent(NetworkConnection client, SceneLookupData sceneLookupData)
    {
        if(!IsHostInitialized)
            return;
        BLog.Log("Recieved ClientAddedToSceneEvent scene data: " + sceneLookupData + ", target data: " + KartLobby.MapSceneData, LogSettings, 0);
        if(sceneLookupData == KartLobby.MapSceneData) {
            SpawnStep = LateLobbySpawnStep.NONE;
        }
    }

    [Server]
    public void SetGameLobby(KartLobby gameLobby) {
        if(this.KartLobby != null)
            Debug.LogWarning($"Overwriting lobby in scene \"{gameObject.scene.name}\"");
        BLog.Log($"GameplayManager set game lobby to \"{gameLobby.ID}\"", LogSettings, 0);
        this.KartLobby = gameLobby;
        this.lobbyID.Value = gameLobby.ID;
    }

}

public enum LateLobbySpawnStep 
{
    NONE, STARTING_CONNECTION, CREATING_LOBBY, REGISTERING_MAP, WAITING_FOR_PLAYER, MOVING_TO_SCENE
}

public class AutoLobbySpawner 
{

}

public static class GameplayManagerExtensions 
{
    public static Dictionary<SceneLookupData, GameplayManager> cachedGameplayManagers;

    public static GameplayManager GetGameplayManager(this SceneElements elements) {
        if(!cachedGameplayManagers.ContainsKey(elements.LookupData)) {
            cachedGameplayManagers.Add(elements.LookupData, GameplayManagerDelegate.LocateGameplayManager(elements.Scene));
        }
        return cachedGameplayManagers[elements.LookupData];
    }
}