using System;
using UnityEngine;

public class BotDriver : MonoBehaviour
{

    /** Dot product == 1 when vectors match, Dot product == 0 when vectors are orthogonal */
    public AnimationCurve dotProductToTurn;
    public AnimationCurve turnFactorToThrottle;
    public int turnFactorCount = 3;

    private PositionTracker pt;
    private KartController kc;

    public float throttle;

    private int checkpointIndex;

    void Start()
    {
        pt = GetComponent<PositionTracker>();
        kc = GetComponent<KartController>();
        throttle = 0.6f;
        checkpointIndex = -1;
    }

    void Update() 
    {

        Waypoints waypoints = pt.GetWaypoints();
        bool checkpointIndexUpdated = checkpointIndex != pt.GetWaypointIndex();
        checkpointIndex = pt.GetWaypointIndex();

        float turnAmount = waypoints.GetTurnAmount(checkpointIndex);
        float turnFactor = waypoints.GetTurnFactor(checkpointIndex, turnFactorCount);

        if(checkpointIndexUpdated) {
            SayMessage("Reached " + checkpointIndex + ". turnAmount: " + turnAmount + ", turnFactor:" + turnFactor);
        }

        Vector3 forward = kc.kartForward;
        forward.y = 0;
        Vector3 targetPosition = DetermineTargetPosition();
        Vector3 directionToTarget = (targetPosition-transform.position).normalized;
        directionToTarget.y = 0;

        float dot = Vector3.Dot(forward, directionToTarget); 
        float cross = Vector3.Cross(forward, directionToTarget).y;

        bool isFacingTarget = dot > 0;                  // Check if we're facing towards the target
        int turnLR = (int)Mathf.Sign(cross);            // Get turn direction, +1 for right, -1 for left
        float turnValue = turnLR * dotProductToTurn.Evaluate(Mathf.Abs(dot));      // Convert turnLR into a turn value for use in turn input

        kc.SetTurnInput(new Vector2(turnValue, 0));
        
        float throttle = turnFactorToThrottle.Evaluate(Mathf.Abs(turnFactor)/(0.5f*turnFactorCount));
        if(!isFacingTarget) throttle = -throttle;
        kc.SetThrottleInput(Mathf.Clamp01(0.05f + throttle));

        bool drift = Math.Abs(turnFactor) > 1.3;
        if(!kc.IsDrifting() && drift) {
            // Engaging drift, ensure that turn input matches drift direction
            kc.SetTurnInput(new Vector2(Mathf.Abs(turnValue)*Mathf.Sign(turnFactor), 0));
        }
        kc.SetDriftInput(drift);

        SayMessage("dot: " + dot + ", isFacing: " + isFacingTarget + ", turnLR: " + turnLR + ", turnValue: " + turnValue + ", throttle: " + throttle);

        // Debug code
        // Red = direction to waypoint
        // Blue = forward
        // Green = Line to target position
        Debug.DrawRay(transform.position+Vector3.up*0.5f, directionToTarget, Color.red);
        Debug.DrawRay(transform.position+Vector3.up*0.5f, forward, Color.blue);       
        Debug.DrawLine(transform.position, targetPosition, Color.green);
        // End debug code

    }

    private Vector3 DetermineTargetPosition() {
        Vector3 targetPosition = pt.GetNextWaypoint().position;
        RaycastHit[] hits = Physics.RaycastAll(transform.position, kc.kartForward, Mathf.Infinity, LayerMask.GetMask("Waypoint"));
        print("hit count: " + hits.Length);
        foreach (RaycastHit hit in hits)
        {
            print("hit " + hit.collider.gameObject.name);
            if (hit.collider.isTrigger)
            {
                targetPosition = hit.point;
                break;
            }
        }
        return targetPosition;
    }

    private void SayMessage(String message) {
        print("BotDriver " + gameObject.name + ": " + message);
    }

}
