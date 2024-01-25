using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/** Keeps track of a Kart's position on a track */
public class PositionTracker : KartBehavior, IComparable<PositionTracker>
{

    private Waypoints waypoints;

    [SerializeField] public int waypointIndex;
    public float segmentCompletion;
    public float lapCompletion;
    public float raceCompletion;
    public int lapNumber;
    public int racePos;
    public bool hasStartedRace;
    public float raceFinishTime;

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
        
        segmentCompletion = lapCompletion = raceCompletion = 0;

        hasStartedRace = false;
        lapNumber = 0;
        raceFinishTime = -1;
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
            lapNumber = 0;
            raceFinishTime = -1;
        } else if(advancedLapCompleted) 
            lapNumber += 1;
    }

    void Update() {
        if(GameplayManager.RaceManager.RaceTime < 0) {
            waypointIndex = waypoints.Count-1;
            lapNumber = 0;
        }

        segmentCompletion = GetSegmentCompletion();
        lapCompletion = GetLapCompletion();
        raceCompletion = GetRaceCompletion();
    
        if(raceCompletion >= 1 && raceFinishTime == -1) {
            RaceFinished();
        }
    
    }

    void RaceFinished() 
    {
        raceFinishTime = GameplayManager.RaceManager.RaceTime;

        if(kartManager.HasPOIGDelegate) {
            kartManager.POIGDelegate.HUD.enabled = false;
        }
    }

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
        RaceManager rm = GameplayManager.RaceManager;
        return Mathf.Lerp((float)lapNumber/rm.settings.laps, Mathf.Clamp01((float)(lapNumber+1)/rm.settings.laps), lapCompletion);
    }

    public int CompareTo(PositionTracker other)
    {
        return -raceCompletion.CompareTo(other.raceCompletion);
    }
}
