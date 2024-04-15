using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/** The gameplay manager is responsible for general game loop mgmt.
  * It will check if everything is in order on Start(), printing out an error if it fails.
  * Other classes will refer to this to access game data. */
[RequireComponent(typeof(RaceManager))]
[RequireComponent(typeof(KartsIRManager))]
[RequireComponent(typeof(KartSpawner))]
[RequireComponent(typeof(ItemManager))]
public class GameplayManager : NetworkBehaviour
{

    // /** Singleton instance of the Gameplay manager */
    // public static GameplayManager Instance;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject playerObjectManagerPrefab;
    [SerializeField]
    private GameObject atlasesPrefab;

    [Header("Settings")]
    public bool showWarnings = false;
    public bool showBotPathWarnings = false;
    public bool playStartAnimation = true;
    public float startAnimationDuration = 3.0f;

    [Header("Runtime")]
    public bool ready;

    private RaceManager _raceManager;
    private KartsIRManager _kartsIRManager;
    private KartSpawner _kartSpawner;
    private ItemManager _itemManager;

    private KartLevelManager kartLevelManager;
    private GameLobby lobby;
    [SyncVar]
    private string lobbyID = null;

    void Awake() 
    {

        List<string> problems = new();
        List<string> warnings = new();

        // Load everything
        _raceManager = GetComponent<RaceManager>();
        _kartsIRManager = GetComponent<KartsIRManager>();
        _kartSpawner = GetComponent<KartSpawner>();
        _itemManager = GetComponent<ItemManager>();

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

    public RaceManager RaceManager { get { return _raceManager; } }
    public KartsIRManager PlayerManager { get { return _kartsIRManager; } }
    public KartSpawner KartSpawner { get { return _kartSpawner; } }
    public ItemManager ItemManager { get { return _itemManager; } }

    public KartLevelManager KartLevelManager { get { return kartLevelManager; } }
    public GameLobby GameLobby { get { return lobby; } }

    public ItemAtlas ItemAtlas { get { return atlasesPrefab.GetComponent<ItemAtlas>(); } }
    public LevelAtlas LevelAtlas { get { return atlasesPrefab.GetComponent<LevelAtlas>(); } }
    public KartAtlas KartAtlas { get { return atlasesPrefab.GetComponent<KartAtlas>(); } }

    [Server]
    public void SetGameLobby(GameLobby gameLobby) {
        if(this.lobby != null)
            Debug.LogWarning($"Overwriting lobby in scene \"{gameObject.scene.name}\"");
        this.lobby = gameLobby;
        this.lobbyID = gameLobby.ID;
    }

    public bool HasLobby { get { return lobbyID != null; } }

}
