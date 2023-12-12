using System;
using UnityEngine;

/** BotDriver V2 by Ethan Mullen.
  * We'll be following the bot path script, which picks waypoints out on the map
  *   and generates a driving path from that. */
[RequireComponent(typeof(PositionTracker), typeof(KartController), typeof(BotPath))]
public class BotDriver : MonoBehaviour
{

    /** Dot product == 1 when vectors match, Dot product == 0 when vectors are orthogonal */
    [Header("Bot Settings")]
    public AnimationCurve dotProductToTurn;
    public AnimationCurve dotProductToDriftTurn;
    public AnimationCurve dotProductToThrottle;
    public float tfThresholdCalcFrequency = 0.33f;
    public float stuckAnimationDuration = 0.2f;

    private PositionTracker pt;
    private KartController kc;
    private BotPath bp;

    private float secondClock;

    /* ----- Machine learnable fields ---- */
    [Header("ML Fields")] public PathAnalysisSettings driftVision;
    public float driftThreshold = 200;
    public PathAnalysisSettings throttleVision;
    public float throttleThreshold = 300;
    public Vector2 throttleVisionExtents;

    /* ------- Inputs to controller ------ */
    [Header("Input"), SerializeField] private Vector2 turnInput;
    [SerializeField] private float throttleInput;
    [SerializeField] private bool driftInput;
    [SerializeField] private bool boostInput;

    /* ----- Fields used in Update() ----- */
    [Header("Bot Brain"), SerializeField] private float throttleSight, driftSight;
    [SerializeField] private float distanceFromPath;
    [SerializeField] private float dot; // Dot product to target direction

    [SerializeField] private int turnLR; // Turn direction
    [SerializeField] private float turnValue;
    [SerializeField] private float averageTrackSpeed;
    private int averageTrackSpeedCount;
    private readonly int averageTrackSpeedCountLimit = 10;

    [SerializeField] private bool stuck;
    [SerializeField] private float stuckAnimTime;
    /* ----------------------------------- */

    void Start()
    {
        pt = GetComponent<PositionTracker>();
        kc = GetComponent<KartController>();
        bp = GetComponent<BotPath>();

        secondClock = 1;
    }

    /** Bot logic:
      * The goal is to stay on the orange line. This will be done using the tangent to the line at that position */
    void Update() 
    {
        /* Average speed */
        if(secondClock > 0) {
            secondClock -= Time.deltaTime;
            if(secondClock <= 0) {
                secondClock = 1;

                float trackSpeedsTotal = (averageTrackSpeed*(averageTrackSpeedCount - averageTrackSpeedCount == averageTrackSpeedCountLimit ? 1 : 0)) + kc.TrackSpeed;
                if(averageTrackSpeedCount < averageTrackSpeedCountLimit) averageTrackSpeedCount += 1;
                averageTrackSpeed = trackSpeedsTotal / averageTrackSpeedCount;

            }
        }

        throttleVision.distance = throttleVisionExtents.x + (throttleVisionExtents.y - throttleVisionExtents.x) * Mathf.Max(kc.SpeedRatio, 0.2f);

        float progressEstimate = bp.EstimateProgress();
        Vector3 closestPathPoint = bp.curveSegments[pt.waypointIndex].CalculateBezierPoint(progressEstimate);
        distanceFromPath = Vector3.Distance(transform.position, closestPathPoint);
        float distanceFromPathFactor = Mathf.Clamp01(distanceFromPath/1.5f);

        /* If the bot is far away from their path, move the target position forward further along the path
         *   to smooth out our return path. */
        (int, float) posOffsetByDFP = bp.MoveAlongPath(8f*distanceFromPathFactor, pt.waypointIndex, progressEstimate);
        (Vector3, Vector3) pathData = bp.ReadPath(posOffsetByDFP.Item2, posOffsetByDFP.Item1);
        Vector3 pathPosition = pathData.Item1;
        Vector3 pathTangent = pathData.Item2;
        Vector3 targetForward = Vector3.Lerp(pathTangent, (pathPosition-transform.position).normalized, distanceFromPathFactor);

        dot = Vector3.Dot(targetForward, transform.forward);
        turnLR = (int)Mathf.Sign(Vector3.Cross(kc.KartForward, targetForward).y); // Get turn direction, +1 for right, -1 for left

        if(!kc.DriftInput) {
            turnValue = turnLR * dotProductToTurn.Evaluate(Mathf.Abs(dot)); // Convert turnLR into a turn value for use in turn input
        } else {
            // Drift engaged, ensure that turn input matches drift direction
            turnValue = turnLR * dotProductToDriftTurn.Evaluate(Mathf.Abs(dot)); // Convert turnLR into a turn value for use in turn input
        }
        kc.TurnInput = new(turnValue, 0);

        /* Throttle */
        throttleSight = bp.AnalyzePathFromCurrentPosition(throttleVision);

        float dfpValue = Mathf.Clamp01(4*(distanceFromPathFactor*distanceFromPathFactor) - 4*distanceFromPathFactor + 1);
        float dotValue = dotProductToThrottle.Evaluate(Math.Abs(dot));
        float thrValue = 1-Mathf.Clamp01((throttleSight-throttleThreshold)/throttleThreshold);

        throttleInput =  Mathf.Clamp01((dotValue+thrValue)*(1-distanceFromPathFactor) + (dfpValue*distanceFromPathFactor));
        kc.ThrottleInput = !stuck ? throttleInput : 0;

        /* Drift */
        driftSight = bp.AnalyzePathFromCurrentPosition(driftVision);
        kc.DriftInput = !stuck && driftSight > driftThreshold;

        /* Manage stuck */
        if(stuck && stuckAnimTime > 0) {
            transform.forward = Vector3.Lerp(transform.forward, targetForward, 1-(stuckAnimTime/stuckAnimationDuration));
            stuckAnimTime -= Time.deltaTime;
            if(stuckAnimTime <= 0) {
                stuckAnimTime = 0;
                stuck = false;
                transform.forward = targetForward;
            }
        }

        if(!stuck && averageTrackSpeedCount == averageTrackSpeedCountLimit && averageTrackSpeed <= 0.33f) {
            if(dot < 0.9) {
                stuckAnimTime = stuckAnimationDuration;
                stuck = true;
            } else {
                GetComponent<Rigidbody>().AddForce((closestPathPoint-transform.position).normalized*25f, ForceMode.Acceleration);
            }
        }

        // Debug code
        // Red = direction to waypoint
        // Blue = forward
        // Green = Line to target position
        Debug.DrawLine(transform.position, pathPosition, new(1f, 0.5f, 0));
        Debug.DrawRay(pathPosition, pathTangent, new(1f, 0.5f, 0));
        // End debug code

    }

}