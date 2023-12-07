using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** The gameplay manager is responsible for general game loop mgmt.
  * It will check if everything is in order on Start(), printing out an error if it fails.
  * Other classes will refer to this to access game data. */
public class GameplayManager : MonoBehaviour
{

    /** Singleton instance of the Gameplay manager */
    public static GameplayManager Instance;
    public static RaceManager RaceManager { get { return Instance.GetRaceManager(); } }
    public static PlayerManager PlayerManager { get { return Instance.GetPlayerManager(); } }
    public static ItemAtlas ItemAtlas { get { return Instance.GetItemAtlas(); } }
    public static SpawnPositions SpawnPositions { get { return Instance.GetSpawnPositions(); }}
    public static Waypoints Waypoints { get { return Instance.GetWaypoints(); }}

    public bool ready;

    private RaceManager rm;
    private PlayerManager pm;
    private ItemAtlas ia;

    private SpawnPositions spawnPositions;
    private Waypoints waypoints;

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
        ia = GetComponent<ItemAtlas>();

        GameObject spo = GameObject.Find("SpawnPositions");
        if(spo != null) spawnPositions = spo.GetComponent<SpawnPositions>();

        GameObject wpo = GameObject.Find("Waypoints");
        if(wpo != null) waypoints = wpo.GetComponent<Waypoints>();

        // Check if everything is in order
        List<String> problems = new List<String>();
        if(rm == null) problems.Add("GameplayManager object doesn't have a RaceManager script component!");
        if(pm == null) problems.Add("GameplayManager object doesn't have a PlayerManager script component!");
        if(spawnPositions == null) problems.Add("Failed to find SpawnPositions. " + spo == null ? "No spawn position object found." : "Game object found, no SpawnPositions script component though.");
        if(waypoints == null) problems.Add("Failed to find Waypoints. " + wpo == null ? "No waypoint object found." : "Game object found, no Waypoints script component though.");

        ready = problems.Count == 0;
        if(!ready) {
            Debug.LogError("Failed to start GameplayManager. " + problems.Count + " problem(s).");
            problems.ForEach(problem => Debug.LogError(" - " + problem));
        }

    }

    public RaceManager GetRaceManager() { return rm; }
    public PlayerManager GetPlayerManager() { return pm; }
    public ItemAtlas GetItemAtlas() { return ia; }

    public SpawnPositions GetSpawnPositions() { return spawnPositions; }
    public Waypoints GetWaypoints() { return waypoints; }

}
