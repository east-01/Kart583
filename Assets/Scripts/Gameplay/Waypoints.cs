using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class Waypoints : MonoBehaviour
{
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

    public float GetTurnAmount(int waypointIndex) 
    {
        Transform currentWaypoint = GetWaypointFromIndex(waypointIndex);
        Vector3 a = (currentWaypoint.position - GetPreviousWaypoint(waypointIndex).position).normalized;
        a.y = 0;
        Vector3 b = (GetNextWaypoint(waypointIndex).position - currentWaypoint.position).normalized;
        b.y = 0;
        return Vector3.Cross(a, b).y;
    }

    public float GetTurnFactor(int currentWaypointIndex, int lookAheadAmount) 
    {
        float accumulator = 0;
        for(int i = 0; i < lookAheadAmount; i++) {
            accumulator += GetTurnAmount(currentWaypointIndex);

            currentWaypointIndex += 1;
            if(currentWaypointIndex >= transform.childCount) currentWaypointIndex = 0;
        }
        return accumulator;
    }

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
