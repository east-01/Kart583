using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotDriver : MonoBehaviour
{

    private PositionTracker pt;
    private KartController kc;

    public float throttle;

    private int trackedCheckpointIndex;
    void Start()
    {
        pt = GetComponent<PositionTracker>();
        kc = GetComponent<KartController>();
        throttle = 0.6f;
    }

    void Update() 
    {

        if(trackedCheckpointIndex != pt.GetWaypointIndex()) {
            SayMessage("Tracking new checkpoint " + pt.GetWaypointIndex());
        }
        trackedCheckpointIndex = pt.GetWaypointIndex();

        if(kc.Grounded()) {
            kc.SetThrottleInput(throttle);
        } else {
            kc.SetThrottleInput(0f);
        }

        Vector3 forward = kc.kartForward;
        forward.y = 0;
        Vector3 directionToWaypoint = (pt.GetNextWaypoint().position-transform.position).normalized;
        directionToWaypoint.y = 0;

        float dot = Vector3.Dot(forward, directionToWaypoint); 
        bool isFacingTarget = dot > 0;                          // Check if we're facing towards the target
        int turnLR = (int)Mathf.Sign(Vector3.Cross(forward, directionToWaypoint).z);                                         // Get turn direction, +1 for right, -1 for left
        float turnValue = 1-turnLR * Mathf.Abs(dot);

        SayMessage("dot: " + dot + ", isFacing: " + isFacingTarget + ", turnLR: " + turnLR + ", turnValue: " + turnValue);

        kc.SetTurnInput(new Vector2(turnValue, 0));
        
        // Debug code
        // Red = direction to waypoint
        // Blue = forward
        // Green = turn
        Debug.DrawRay(transform.position+Vector3.up*0.5f, directionToWaypoint, Color.red);
        Debug.DrawRay(transform.position+Vector3.up*0.5f, forward, Color.blue);       
        // End debug code

    }

    private void SayMessage(String message) {
        print("BotDriver " + gameObject.name + ": " + message);
    }

}
