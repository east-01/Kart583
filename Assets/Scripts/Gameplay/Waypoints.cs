using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    public Transform GetWaypointFromIndex(int index) {
        return transform.GetChild(Mathf.Clamp(index, 0, transform.childCount));
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
