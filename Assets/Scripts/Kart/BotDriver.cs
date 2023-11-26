using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotDriver : MonoBehaviour
{

    private PositionTracker pt;
    private KartController kc;

    public float throttle;

    void Start()
    {
        pt = GetComponent<PositionTracker>();
        kc = GetComponent<KartController>();
        throttle = 0.6f;
    }

    void Update() 
    {
        if(kc.Grounded()) {
            kc.SetThrottleInput(throttle);
        } else {
            kc.SetThrottleInput(0f);
        }
        Vector3 forward = kc.kartForward;
        forward.y = 0;
        Vector3 directionToWaypoint = (pt.GetNextWaypoint().position-transform.position).normalized;
        directionToWaypoint.y = 0;
        kc.SetTurnInput((forward-directionToWaypoint).normalized);
    }

}
