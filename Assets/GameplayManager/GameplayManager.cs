using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.Networking;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
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
    private readonly SyncVar<string> lobbyID = new();
    public bool HasLobby => lobbyID.Value != null;

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

    private void Update() {

    }

    public void SetGameLobby(KartLobby gameLobby) {
        if(!InstanceFinder.IsServerStarted) {
            Debug.LogError("Can't set game lobby, call was made on a non-server instance.");
            return;
        }
        if(this.KartLobby != null)
            Debug.LogWarning($"Overwriting lobby in scene \"{gameObject.scene.name}\"");
        BLog.Log($"GameplayManager set game lobby to \"{gameLobby.ID}\"", LogSettings, 0);
        this.KartLobby = gameLobby;
        this.lobbyID.Value = gameLobby.ID;
    }

}