using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/** The gameplay manager is responsible for general game loop mgmt.
  * It will check if everything is in order on Start(), printing out an error if it fails.
  * Other classes will refer to this to access game data. */
public class GameplayManager : NetworkBehaviour
{

    /** Singleton instance of the Gameplay manager */
    public static GameplayManager Instance;

    [Header("Prefabs")]
    public GameObject playerObjectManagerPrefab;

    [Header("Settings")]
    public bool showWarnings = false;
    public bool showBotPathWarnings = false;
    public bool playStartAnimation = true;
    public float startAnimationDuration = 3.0f;

    [Header("Runtime")]
    public bool ready;

    private RaceManager rm;
    private KartsIRManager pm;
    private PlayerInputManager pim;
    private ItemAtlas ia;
    private LevelAtlas la;
    private KartAtlas ka;

    private SpawnPositions spawnPositions;
    private Waypoints waypoints;
    private Transform kartContainer;
    private Transform itemContainer;
    private RaceCamera raceCamera;
    private ScreenManager screenManager;
    private IntroCamData introCamData;

    void Start() 
    {

        if(Instance != null) {
            Destroy(Instance);
            print("Destroyed existing GameplayManager singleton.");            
        }

        Instance = this;

        List<string> problems = new();
        List<string> warnings = new();

        // Connect to player object manager
        if(PlayerObjectManager.Instance == null) {
            Instantiate(playerObjectManagerPrefab);
        } else if(PlayerObjectManager.Instance.GetPlayerObjects().Count == 0) {
            problems.Add("PlayerObjectManager doesn't have any players! Was the GameplayManager loaded before a PlayerObjectManager?");
        }

        pim = PlayerObjectManager.Instance.GetPlayerInputManager();
        if(pim == null) problems.Add("GameplayManager object doesn't have a PlayerInputManager script/input component!");

        // Load everything
        rm = GetComponent<RaceManager>();
        pm = GetComponent<KartsIRManager>();
        ia = GetComponent<ItemAtlas>();
        la = GetComponent<LevelAtlas>();
        ka = GetComponent<KartAtlas>();

        GameObject spo = GameObject.Find("SpawnPositions");
        if(spo != null) spawnPositions = spo.GetComponent<SpawnPositions>();

        GameObject wpo = GameObject.Find("Waypoints");
        if(wpo != null) waypoints = wpo.GetComponent<Waypoints>();

        GameObject kco = GameObject.Find("KartContainer");
        if(kco != null) kartContainer = kco.transform;

        GameObject ico = GameObject.Find("ItemContainer");
        if(ico != null) itemContainer = ico.transform;

        GameObject rco = GameObject.Find("RaceCamera");
        if(rco != null) raceCamera = rco.GetComponent<RaceCamera>();

        screenManager = rco.GetComponentInChildren<ScreenManager>();

        GameObject icdo = GameObject.Find("IntroCamData");
        if(icdo != null) introCamData = icdo.GetComponent<IntroCamData>();

        // Check if everything is in order
        if(rm == null) problems.Add("GameplayManager object doesn't have a RaceManager script component!");
        if(pm == null) problems.Add("GameplayManager object doesn't have a PlayerManager script component!");
        if(screenManager == null) problems.Add("RaceCamera object doesn't have a ScreenManager script component!");
        if(ia == null) problems.Add("GameplayManager object doesn't have an ItemAtlas script component!");
        if(la == null) problems.Add("GameplayManager object doesn't have a LevelAtlas script component!");
        if(ka == null) problems.Add("GameplayManager object doesn't have a KartAtlas script component!");
        if(spawnPositions == null) problems.Add("Failed to find SpawnPositions. " + (spo == null ? "No spawn position object found." : "Game object found, no SpawnPositions script component though."));
        if(waypoints == null) problems.Add("Failed to find Waypoints. " + (wpo == null ? "No waypoint object found." : "Game object found, no Waypoints script component though."));
        if(kartContainer == null) problems.Add("Failed to find KartContainer. Add an empty object named KartContainer as a child of KartLevel.");
        if(itemContainer == null) problems.Add("Failed to find ItemContainer. Add an empty object named ItemContainer as a child of KartLevel. ");
        if(raceCamera == null) problems.Add("Failed to find Race Camera. " + (rco == null ? "No race camera object found." : "Game object found, no RaceCamera script component though."));
        if(introCamData == null) warnings.Add("Failed to find IntroCamData. " + (icdo == null ? "No intro cam data object found." : "Game object found, no IntroCamData script component though."));

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

    public static RaceManager RaceManager { get { return Instance.GetRaceManager(); } }
    public static KartsIRManager PlayerManager { get { return Instance.GetPlayerManager(); } }
    public static PlayerInputManager PlayerInputManager { get { return Instance.GetPlayerInputManager(); } }
    public static ScreenManager ScreenManager { get { return Instance.GetScreenManager(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.GetItemAtlas(); } }
    public static LevelAtlas LevelAtlas { get { return Instance.GetLevelAtlas(); } }
    public static KartAtlas KartAtlas { get { return Instance.GetKartAtlas(); } }
    public static SpawnPositions SpawnPositions { get { return Instance.GetSpawnPositions(); } }
    public static Waypoints Waypoints { get { return Instance.GetWaypoints(); } }
    public static Transform KartContainer { get { return Instance.GetKartContainer(); } }
    public static Transform ItemContainer { get { return Instance.GetItemContainer(); } }
    public static RaceCamera RaceCamera { get { return Instance.GetRaceCamera(); } }
    public static bool HasRaceCamera { get { return RaceCamera != null; } }
    public static IntroCamData IntroCamData { get { return Instance.GetIntroCamData();} }

    public RaceManager GetRaceManager() { return rm; }
    public KartsIRManager GetPlayerManager() { return pm; }
    public PlayerInputManager GetPlayerInputManager() { return pim; }
    public ScreenManager GetScreenManager() { return screenManager; } 
    public ItemAtlas GetItemAtlas() { return ia; }
    public LevelAtlas GetLevelAtlas() { return la; }
    public KartAtlas GetKartAtlas() { return ka; }

    public SpawnPositions GetSpawnPositions() { return spawnPositions; }
    public Waypoints GetWaypoints() { return waypoints; }
    public Transform GetKartContainer() { return kartContainer; }
    public Transform GetItemContainer() { return itemContainer; }
    public RaceCamera GetRaceCamera() { return raceCamera; }
    public IntroCamData GetIntroCamData() { return introCamData; }

}
