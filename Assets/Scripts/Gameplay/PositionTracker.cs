using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Keeps track of a Kart's position on a track */
public class PositionTracker : MonoBehaviour
{

    private Waypoints waypoints;

    [SerializeField] private int waypointIndex;
    public float lapCompletion;

    void Awake() 
    {
        GameObject waypointsObj = GameObject.Find("Waypoints");
        if(waypointsObj != null && waypointsObj.GetComponent<Waypoints>() == null) waypointsObj = null;
        if(waypointsObj == null) {
            Debug.LogError("Failed to find waypoints GameObject!");
            Destroy(gameObject);
            return;
        }
        waypoints = waypointsObj.GetComponent<Waypoints>();
    }

    void OnTriggerEnter(Collider other) 
    {
        if(other.tag != "Waypoint") return;
        int enteredIndex = other.gameObject.transform.GetSiblingIndex();

        bool advancedNaturally = enteredIndex == waypointIndex + 1;
        bool advancedLapCompleted = waypointIndex + 1 == waypoints.transform.childCount && enteredIndex == 0;

        if(advancedNaturally || advancedLapCompleted) waypointIndex = enteredIndex;

        if(advancedLapCompleted) print("LAP COMPLETED");
    }

    void Update() {
        lapCompletion = GetLapCompletion();
    }

    public Transform GetCurrentWaypoint() { return waypoints.GetWaypointFromIndex(waypointIndex); }
    public Transform GetNextWaypoint() 
    {
        int idx = waypointIndex+1;
        if(idx >= waypoints.Count) idx = 0;
        return waypoints.GetWaypointFromIndex(idx);
    }

    public float GetLapCompletion() 
    {
        float selfToTargetWaypoint = Vector3.Distance(transform.position, GetNextWaypoint().position);
        float prevToTargetWaypoint = Vector3.Distance(GetCurrentWaypoint().position, GetNextWaypoint().position);
        float segmentCompletion = Mathf.Clamp01(selfToTargetWaypoint/prevToTargetWaypoint);

        float lc1 = waypointIndex/(float)waypoints.Count;
        float lc2 = (waypointIndex+1)/(float)waypoints.Count;
        if(lc2 == 0) lc2 = 1;

        return Mathf.Lerp(lc1, lc2, segmentCompletion);
    }

}
