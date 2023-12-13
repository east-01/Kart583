using System;
using System.IO;
using UnityEditor;


#if UNITY_EDITOR
using UnityEngine.UI;
#endif
using UnityEngine;

public class Waypoints : MonoBehaviour
{

    private float _avgWaypointDistance = -1;
    public float avgWaypointDistance {
        get {
            if(_avgWaypointDistance == -1) {
                float accumulator = 0;
                int countedPoints = 0;
                Transform previous = transform.GetChild(0);
                foreach(Transform t in transform) { 
                    if(countedPoints == 0) {
                        countedPoints++;
                        continue;
                    }
                    float dist = Vector3.Distance(t.position, previous.position);
                    accumulator += dist;
                    previous = t;
                    countedPoints++;
                }
                _avgWaypointDistance = accumulator / countedPoints;
            }
            return _avgWaypointDistance;
        }
    }

    [Range(0f, 2f)]
    [SerializeField] private float waypointSize = 1f;
    private void OnDrawGizmos()
    {
        foreach(Transform t in transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(t.position, waypointSize);
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }


        //Draw final line back to the first waypoint
        Gizmos.DrawLine(transform.GetChild(transform.childCount - 1).position, transform.GetChild(0).position);
    }

    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        if (currentWaypoint == null)
        {
            return transform.GetChild(0);
        }

        if (currentWaypoint.GetSiblingIndex() < transform.childCount - 1)
        {
            return transform.GetChild(currentWaypoint.GetSiblingIndex() + 1);
        } else
        {
            return transform.GetChild(0);
        }
    }

    public Transform GetNextWaypoint(int currentIndex) 
    {
        currentIndex += 1;
        if(currentIndex >= transform.childCount) currentIndex = 0;
        return GetWaypointFromIndex(currentIndex);
    }

    public Transform GetPreviousWaypoint(int currentIndex) 
    {
        currentIndex -= 1;
        if(currentIndex < 0) currentIndex = transform.childCount-1;
        return GetWaypointFromIndex(currentIndex);
    }

    public Transform GetWaypointFromIndex(int index) 
    {
        return transform.GetChild(Mathf.Clamp(index, 0, transform.childCount));
    }

    //Inundated code replaced by GetTurnFactor
    public (Transform, Transform, Transform) ThreeWPLookAhead(Transform currentWaypoint)
    {
        if (currentWaypoint == null)
        {
            return (transform.GetChild(0), transform.GetChild(1), transform.GetChild(2));
        }

        if (currentWaypoint.GetSiblingIndex() < transform.childCount - 1)
        {
            return (transform.GetChild(currentWaypoint.GetSiblingIndex() + 1), transform.GetChild(currentWaypoint.GetSiblingIndex() + 2), transform.GetChild(currentWaypoint.GetSiblingIndex() + 3));
        } else
        {
            return (transform.GetChild(0), transform.GetChild(1), transform.GetChild(2));
        }
    }

    public int Count { get { return transform.childCount;} }

}