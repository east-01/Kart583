using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/** The gameplay manager is responsible for general game loop mgmt.
  * It will check if everything is in order on Start(), printing out an error if it fails.
  * Other classes will refer to this to access game data. */
public class GameplayManager : MonoBehaviour
{

    /** Singleton instance of the Gameplay manager */
    public static GameplayManager Instance;

    [Header("Settings")]
    public bool showWarnings = false;

    public bool ready;

    private RaceManager rm;
    private PlayerManager pm;
    private PlayerInputManager pim;
    private ItemAtlas ia;
    private LevelAtlas la;

    private SpawnPositions spawnPositions;
    private Waypoints waypoints;
    private RaceCamera raceCamera;
    private IntroCamData introCamData;

    void Start() 
    {

        if(Instance != null) {
            Destroy(Instance);
            print("Destroyed existing GameplayManager singleton.");            
        }

        Instance = this;

        // Load everything
        rm = GetComponent<RaceManager>();
        pm = GetComponent<PlayerManager>();
        pim = GetComponent<PlayerInputManager>();
        ia = GetComponent<ItemAtlas>();
        la = GetComponent<LevelAtlas>();

        GameObject spo = GameObject.Find("SpawnPositions");
        if(spo != null) spawnPositions = spo.GetComponent<SpawnPositions>();

        GameObject wpo = GameObject.Find("Waypoints");
        if(wpo != null) waypoints = wpo.GetComponent<Waypoints>();

        GameObject rco = GameObject.Find("RaceCamera");
        if(rco != null) raceCamera = rco.GetComponent<RaceCamera>();

        GameObject icdo = GameObject.Find("IntroCamData");
        if(icdo != null) introCamData = icdo.GetComponent<IntroCamData>();

        // Check if everything is in order
        List<String> problems = new List<String>();
        List<String> warnings = new List<String>();
        if(rm == null) problems.Add("GameplayManager object doesn't have a RaceManager script component!");
        if(pm == null) problems.Add("GameplayManager object doesn't have a PlayerManager script component!");
        if(pim == null) problems.Add("GameplayManager object doesn't have a PlayerInputManager script/input component!");
        if(ia == null) problems.Add("GameplayManager object doesn't have an ItemAtlas script component!");
        if(la == null) problems.Add("GameplayManager object doesn't have a LevelAtlas script component!");
        if(spawnPositions == null) problems.Add("Failed to find SpawnPositions. " + (spo == null ? "No spawn position object found." : "Game object found, no SpawnPositions script component though."));
        if(waypoints == null) problems.Add("Failed to find Waypoints. " + (wpo == null ? "No waypoint object found." : "Game object found, no Waypoints script component though."));
        if(raceCamera == null) warnings.Add("Failed to find Race Camera. " + (rco == null ? "No race camera object found." : "Game object found, no RaceCamera script component though."));
        if(introCamData == null) warnings.Add("Failed to find IntroCamData. " + (icdo == null ? "No intro cam data object found." : "Game object found, no IntroCamData script component though."));

        if(warnings.Count > 0) {
            Debug.Log("GameplayManager experienced " + warnings.Count + " warning(s).");
            if(showWarnings) warnings.ForEach(warning => Debug.LogError(" - " + warning));
        }

        ready = problems.Count == 0;
        if(!ready) {
            Debug.LogError("Failed to start GameplayManager. " + problems.Count + " problem(s).");
            problems.ForEach(problem => Debug.LogError(" - " + problem));
        }

    }

    public static RaceManager RaceManager { get { return Instance.GetRaceManager(); } }
    public static PlayerManager PlayerManager { get { return Instance.GetPlayerManager(); } }
    public static PlayerInputManager PlayerInputManager { get { return Instance.GetPlayerInputManager(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.GetItemAtlas(); } }
    public static LevelAtlas LevelAtlas { get { return Instance.GetLevelAtlas(); } }
    public static SpawnPositions SpawnPositions { get { return Instance.GetSpawnPositions(); }}
    public static Waypoints Waypoints { get { return Instance.GetWaypoints(); }}
    public static RaceCamera RaceCamera { get { return Instance.GetRaceCamera(); } }
    public static bool HasRaceCamera { get { return RaceCamera != null; } }
    public static IntroCamData IntroCamData { get { return Instance.GetIntroCamData();} }

    public RaceManager GetRaceManager() { return rm; }
    public PlayerManager GetPlayerManager() { return pm; }
    public PlayerInputManager GetPlayerInputManager() { return pim; }
    public ItemAtlas GetItemAtlas() { return ia; }
    public LevelAtlas GetLevelAtlas() { return la; }

    public SpawnPositions GetSpawnPositions() { return spawnPositions; }
    public Waypoints GetWaypoints() { return waypoints; }
    public RaceCamera GetRaceCamera() { return raceCamera; }
    public IntroCamData GetIntroCamData() { return introCamData; }

}
