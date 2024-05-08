using System;
using System.Collections.Generic;
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

    [Header("Settings")]
    public bool showWarnings = false;
    public bool showBotPathWarnings = false;
    public bool playStartAnimation = true;
    public float startAnimationDuration = 3.0f;

    [Header("Runtime")]
    public bool ready;

    private RaceManager _raceManager;
    private KartsIRManager _kartsIRManager;
    private ItemManager _itemManager;

    private KartLevelManager kartLevelManager;
    private GameLobby lobby;
    [SyncVar]
    private string lobbyID = null;

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
        _raceManager = GetComponent<RaceManager>();
        _kartsIRManager = GetComponent<KartsIRManager>();
        _itemManager = GetComponent<ItemManager>();

        // Checking HasProcessedLoadMode is important for a Game scene that loads and then instantly unloads
        // i.e. Running TEST_TRACK as the editor scene, then DevSettings instantly loads a TEST_TRACK on top
        if(CoreManager.DevSettings.LoadMode != LoadMode.NONE && !CoreManager.DevSettings.HasProcessedLoadMode)
            return;

        if(!CoreManager.LobbyCommunicator.InLobby) {
            SpawnStep = LateLobbySpawnStep.STARTING_CONNECTION;
        }

        // Initialize KartLevelManager
        KartLevelManager klm = FindObjectOfType<KartLevelManager>();
        if(klm != null) {
            kartLevelManager = klm;
            (List<string>, List<string>) problemsWarnings = kartLevelManager.Initialize();
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
        BLog.Highlight("Registered client added to scene event");
    }

    private void OnDisable() 
    {
        SceneController.Instance.ClientAddedToSceneEvent -= SceneDelegate_ClientAddedToSceneEvent;
        BLog.Highlight("Unregistered client add event");
    }

    private void Update() {
        if(RaceManager.Phase == RacePhase.FINISHED && Input.GetKeyDown(KeyCode.Space)) {
            BLog.Highlight("Debug race complete continue button pressed, this should be done by the results menu MenuController.");
            GameLobby.MovePlayersToLobby();
        }

        if(SpawnStep != LateLobbySpawnStep.NONE) {
            if(SpawnStep == LateLobbySpawnStep.STARTING_CONNECTION && base.IsHost) {
                SpawnStep = LateLobbySpawnStep.CREATING_LOBBY;
            } else if(SpawnStep == LateLobbySpawnStep.CREATING_LOBBY && NetSceneController.LobbyManager.LobbyCount > 0) {
                SpawnStep = LateLobbySpawnStep.REGISTERING_MAP;
            } else if(SpawnStep == LateLobbySpawnStep.REGISTERING_MAP && lobby != null) {
                SpawnStep = LateLobbySpawnStep.WAITING_FOR_PLAYER;
            } else if(SpawnStep == LateLobbySpawnStep.WAITING_FOR_PLAYER && PlayerObjectManager.Instance.PlayerObjectCount > 0) {
                SpawnStep = LateLobbySpawnStep.MOVING_TO_SCENE;
            }
        }
    }

    private void SpawnStepChanged(LateLobbySpawnStep prev, LateLobbySpawnStep current) {
        BLog.Log($"Spawn step changed to {current}", LogChannel.GameplayManager, 1);
        if(current == LateLobbySpawnStep.STARTING_CONNECTION) {
            BLog.Log($"No lobby existed when joining map. AutoSpawning a local one.", LogChannel.DevSettings);
            CoreManager.IsLocal = true;
            CoreManager.LobbyCommunicator.StartCommunication();
        } else if(current == LateLobbySpawnStep.CREATING_LOBBY) {
            NetSceneController.LobbyManager.CreateLobby();
        } else if(current == LateLobbySpawnStep.REGISTERING_MAP) {
            BLog.Log($"Spawn step- Registering scene with {NetSceneController.LobbyManager.LobbyCount} lobbies", LogChannel.GameplayManager, 1);
            // This is called because neither of the SceneRegistered events will be able to catch unity's scene load.
            NetSceneController.Instance.RegisterScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        } else if(current == LateLobbySpawnStep.MOVING_TO_SCENE) {
            GameLobby.AddPlayer(base.LocalConnection, PlayerObjectManager.Instance.PlayerOne.data);
            GameLobby.MovePlayersToMap();
        }
    }

    private void SceneDelegate_ClientAddedToSceneEvent(NetworkConnection client, SceneLookupData sceneLookupData)
    {
        BLog.Log("Recieved ClientAddedToSceneEvent scene data: " + sceneLookupData + ", target data: " + GameLobby.MapSceneData, LogChannel.GameplayManager, 0);
        if(sceneLookupData == GameLobby.MapSceneData) {
            SpawnStep = LateLobbySpawnStep.NONE;
        }
    }

    public RaceManager RaceManager { get { return _raceManager; } }
    public KartsIRManager PlayerManager { get { return _kartsIRManager; } }
    public ItemManager ItemManager { get { return _itemManager; } }

    public KartLevelManager KartLevelManager { get { return kartLevelManager; } }
    public GameLobby GameLobby { get { return lobby; } }

    [Server]
    public void SetGameLobby(GameLobby gameLobby) {
        if(this.lobby != null)
            Debug.LogWarning($"Overwriting lobby in scene \"{gameObject.scene.name}\"");
        print($"<color=red>setting game lobby to \"{gameLobby.ID}\"</color>");
        this.lobby = gameLobby;
        this.lobbyID = gameLobby.ID;
    }

    public bool HasLobby { get { return lobbyID != null; } }

}

public enum LateLobbySpawnStep {
    NONE, STARTING_CONNECTION, CREATING_LOBBY, REGISTERING_MAP, WAITING_FOR_PLAYER, MOVING_TO_SCENE
}

public class AutoLobbySpawner {

}