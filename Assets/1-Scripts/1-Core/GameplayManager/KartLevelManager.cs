using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Checks if the KartLevel is in order, provides references to essential components
///   in the level.
/// </summary>
public class KartLevelManager : MonoBehaviour
{
    private SpawnPositions spawnPositions;
    private Waypoints waypoints;
    private Transform kartContainer;
    private Transform itemContainer;
    private RaceCamera raceCamera;
    private ScreenManager screenManager;
    private IntroCamData introCamData;

    /// <summary>
    /// Load, and check, everything for the KartLevel.
    /// </summary>
    /// <returns>A tuple containing (problems, warnings).</returns>
    public (List<string>, List<string>) Initialize() 
    {
        List<string> problems = new();
        List<string> warnings = new();

        GameObject spo = GameObject.Find("SpawnPositions");
        if(spo != null) 
            spawnPositions = spo.GetComponent<SpawnPositions>();

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
        if(icdo != null) 
            introCamData = icdo.GetComponent<IntroCamData>();

        if(spawnPositions == null) problems.Add("Failed to find SpawnPositions. " + (spo == null ? "No spawn position object found." : "Game object found, no SpawnPositions script component though."));
        if(waypoints == null) problems.Add("Failed to find Waypoints. " + (wpo == null ? "No waypoint object found." : "Game object found, no Waypoints script component though."));
        if(kartContainer == null) problems.Add("Failed to find KartContainer. Add an empty object named KartContainer as a child of KartLevel.");
        if(itemContainer == null) problems.Add("Failed to find ItemContainer. Add an empty object named ItemContainer as a child of KartLevel. ");
        if(raceCamera == null) problems.Add("Failed to find Race Camera. " + (rco == null ? "No race camera object found." : "Game object found, no RaceCamera script component though."));
        if(screenManager == null) problems.Add("RaceCamera object doesn't have a ScreenManager script component!");
        if(introCamData == null) warnings.Add("Failed to find IntroCamData. " + (icdo == null ? "No intro cam data object found." : "Game object found, no IntroCamData script component though."));

        return (problems, warnings);
    }

    public SpawnPositions SpawnPositions { get { return spawnPositions; } }
    public Waypoints Waypoints { get { return waypoints; } }
    public Transform KartContainer { get { return kartContainer; } }
    public Transform ItemContainer { get { return itemContainer; } }
    public RaceCamera RaceCamera { get { return raceCamera; } }
    public ScreenManager ScreenManager { get { return screenManager; } } 
    public IntroCamData IntroCamData { get { return introCamData; } }

    public bool HasRaceCamera { get { return raceCamera != null; } }

}
