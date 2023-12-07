using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [SerializeField] private Waypoints waypoints;

    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float distanceThreshold = 0.1f;

    private Transform currentWaypoint;

    private Transform wp1, wp2, wp3;

    private float count = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Set initial position to first waypoint
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.position = currentWaypoint.position;


        //Set next waypoint target
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.LookAt(currentWaypoint);

    }

    // Update is called once per frame
    void Update()
    {
        //Calculate the next three waypoint transforms into a Tuple for later curvature use
        (wp1, wp2, wp3) = waypoints.ThreeWPLookAhead(currentWaypoint);

        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
            transform.LookAt(currentWaypoint);
        }

        //TODO: slap into function to be called when a curve is necessary (can be RT determined or Baked)

        //Following Bezier curve with point 1 being starting and point 3 being ending; point 2 is middle curve trajectory
        if (count < 1.0f)
        {
            //lower value for float multiplier makes curving process slower
            count += 0.1f * Time.deltaTime;

            Vector3 m1 = Vector3.Lerp(wp1.position, wp2.position, count);
            Vector3 m2 = Vector3.Lerp(wp2.position, wp3.position, count);

            transform.position = Vector3.Lerp(m1, m2, count);
        }
    }
}
