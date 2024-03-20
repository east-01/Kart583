using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Object;
using UnityEngine.EventSystems;
using FishNet.Connection;
using FishNet.Object.Synchronizing;

/** Keeps track of a Kart's position on a track */
public class PositionTracker : KartBehavior, IComparable<PositionTracker>, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;

    private Waypoints waypoints;

    public int waypointIndex;
    public float segmentCompletion;
    public float lapCompletion;
    [SyncVar]
    private float raceCompletion;
    public float RaceCompletion {
        get { return raceCompletion; }
        set {
            if(base.IsServer) 
                raceCompletion = value;
            else if(base.IsOwner)
                ServerRpcSetRaceCompletion(value);
        }
    }
    public int lapNumber;
    public int racePos;
    public bool hasStartedRace;
    private bool hasFinishedRace; // A boolean tracking if we've notified the server of finishing

	new protected void Awake() 
	{
		base.Awake();
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
	}

    void Start() 
    {
        GameObject waypointsObj = GameObject.Find("Waypoints");
        if(waypointsObj != null && waypointsObj.GetComponent<Waypoints>() == null) waypointsObj = null;
        if(waypointsObj == null) {
            Debug.LogError("Failed to find waypoints GameObject in this scene, disabling.");
            gameObject.SetActive(false);
            return;
        }
        waypoints = waypointsObj.GetComponent<Waypoints>();
        
        segmentCompletion = lapCompletion = RaceCompletion = 0;

        hasStartedRace = false;
        hasFinishedRace = false;
        lapNumber = 0;
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
    }

    void OnTriggerEnter(Collider other) 
    {
        if(other.tag != "Waypoint") return;
        int enteredIndex = other.gameObject.transform.GetSiblingIndex();

        bool advancedNaturally = enteredIndex == waypointIndex + 1;
        bool advancedLapCompleted = waypointIndex + 1 == waypoints.transform.childCount && enteredIndex == 0;

        if(advancedNaturally || advancedLapCompleted) waypointIndex = enteredIndex;

        if(!hasStartedRace) {
            hasStartedRace = true;
            hasFinishedRace = false;
            lapNumber = 0;
        } else if(advancedLapCompleted) 
            lapNumber += 1;
    }

    void Update() {
        if(gameplayManager == null)
            return;

        if(gameplayManager.RaceManager.RaceTime < 0) {
            waypointIndex = waypoints.Count-1;
            lapNumber = 0;
        }

        segmentCompletion = GetSegmentCompletion();
        lapCompletion = GetLapCompletion();
        if(base.IsServer)
            RaceCompletion = GetRaceCompletion();
        else if(base.IsOwner)
            ServerRpcSetRaceCompletion(GetRaceCompletion());

        if(RaceCompletion >= 1 && !hasFinishedRace) {
            hasFinishedRace = true;
            RaceFinished();
        }
    
    }

    private void RaceFinished() 
    {
        if(base.IsServer)
            gameplayManager.RaceManager.CompletedRace(kartManager.GetPlayerData(), raceCompletion);
        else if(base.IsClient && base.IsOwner)
            gameplayManager.RaceManager.ServerRpcCompletedRace(kartManager.GetPlayerData(), raceCompletion);

        if(kartManager.HasPOIGDelegate) {
            kartManager.POIGDelegate.HUD.enabled = false;
        }
    }

    [ServerRpc]
    private void ServerRpcSetRaceCompletion(float raceCompletion) { this.RaceCompletion = raceCompletion; }

    /// <summary>
    /// Call for the server's RaceManager to tell client what our race position is
    /// </summary>
    [TargetRpc]
    public void TargetRpcSetRacePosition(NetworkConnection client, int racePosition) { this.racePos = racePosition; }

    public Waypoints GetWaypoints() { return waypoints; }
    public Transform GetCurrentWaypoint() { return waypoints.GetWaypointFromIndex(waypointIndex); }
    public Transform GetNextWaypoint() 
    {
        int idx = waypointIndex+1;
        if(idx >= waypoints.Count) idx = 0;
        return waypoints.GetWaypointFromIndex(idx);
    }

    public BoxCollider GetNextWaypointCollider() { return GetNextWaypoint().GetComponent<BoxCollider>(); }

    public float GetSegmentCompletion() 
    {
        return GetLineProgress(transform.position, GetCurrentWaypoint().position, GetNextWaypoint().position);
    }

    public float GetLineProgress(Vector3 position, Vector3 current, Vector3 next)
    {
        Vector3 lineStart = current;
        Vector3 lineEnd = next;
        Vector3 lineDirection = lineEnd - lineStart;
        float lineMagnitude = lineDirection.magnitude;
        Vector3 lineNormalized = lineDirection / lineMagnitude;

        Vector3 pointLineStart = transform.position - lineStart;
        float dotProduct = Vector3.Dot(pointLineStart, lineNormalized);

        dotProduct = Mathf.Clamp(dotProduct, 0f, lineMagnitude);
        return dotProduct/lineMagnitude;
    }

    public float GetLapCompletion() 
    {  
        return ConvertToLapProgress((waypointIndex, segmentCompletion));
    }

    public float ConvertToLapProgress((int waypointIndex, float segmentCompletion) position) 
    {
        // lc1&2 represent the lap completion percentage at both waypoint positions
        float lc1 = position.waypointIndex/(float)waypoints.Count;
        float lc2 = ((position.waypointIndex+1)%waypoints.Count)/(float)waypoints.Count;
        if(lc2 == 0) lc2 = 1;
        return Mathf.Clamp01(Mathf.Lerp(lc1, lc2, position.segmentCompletion));        
    }

    public (int, float) ConvertFromLapProgress(float lapProgress) 
    {
        float segmentComp = 1/(float)waypoints.Count;
        return ((int)(lapProgress/segmentComp), (lapProgress%segmentComp)/segmentComp);
    }

    public float GetRaceCompletion() 
    {
        RaceManager rm = gameplayManager.RaceManager;
        return Mathf.Lerp((float)lapNumber/rm.settings.laps, Mathf.Clamp01((float)(lapNumber+1)/rm.settings.laps), lapCompletion);
    }

    public int CompareTo(PositionTracker other)
    {
        return -RaceCompletion.CompareTo(other.RaceCompletion);
    }
}
