// using System;
// using UnityEngine;

// [RequireComponent(typeof(PositionTracker), typeof(KartController), typeof(BotPath))]
// public class BotDriverV1 : MonoBehaviour
// {

//     /** Dot product == 1 when vectors match, Dot product == 0 when vectors are orthogonal */
//     [Header("Bot Settings")]
//     public AnimationCurve dotProductToTurn;
//     public AnimationCurve dotProductToDriftTurn;
//     public AnimationCurve turnFactorToThrottle;
//     public float tfThresholdCalcFrequency = 0.33f;

//     private PositionTracker pt;
//     private KartController kc;
//     private BotPath bp;

//     private float secondClock;

//     /* ----- Machine learnable fields ---- */
//     [Header("ML Fields")] public int turnFactorCount = 4;
//     public float tfThresholdSingleBrake = 0.8f;
//     public float tfThresholdSingleDrift = 0.5f;
//     public float tfThresholdSingleThrottle = 0.1f;

//     /* ------- Inputs to controller ------ */
//     [Header("Input"), SerializeField] private Vector2 turnInput;
//     [SerializeField] private float throttleInput;
//     [SerializeField] private bool driftInput;
//     [SerializeField] private bool boostInput;

//     /* ----- Fields used in Update() ----- */
//     [Header("Bot Brain"), SerializeField] private int checkpointIndex;

//     [SerializeField] private float turnAmount, turnFactor;

//     [SerializeField] private Vector3 targetPosition;
//     private Vector3 forward;
//     [SerializeField] private Vector3 directionToTarget;

//     [SerializeField] private float dot, cross;

//     [SerializeField] private int turnLR; // Turn direction
//     [SerializeField] private float turnValue;
//     [SerializeField] private String throttleState;
//     [SerializeField] private float averageTrackSpeed;
//     private int averageTrackSpeedCount;
//     private readonly int averageTrackSpeedCountLimit = 10;

//     [SerializeField] private bool stuck;
//     [SerializeField] private int stuckLR; // Turn direction that we pick to get out

//     // Turn Factor Thresholds
//     [SerializeField] private float tfThresholdMax;      // The highest a turn factor can be
//     [SerializeField] private float tfThresholdBrake;    // Highest level, bot will slam on brakes to make a turn
//     [SerializeField] private float tfThresholdDrift;    // Medium level, bot will drift to make turn and maintain speed
//     [SerializeField] private float tfThresholdThrottle; // Lowest level, bot will ease throttle slightly to make turn
//     private float tfThresholdCalcTime;

//     /* ----------------------------------- */

//     void Start()
//     {
//         pt = GetComponent<PositionTracker>();
//         kc = GetComponent<KartController>();
//         bp = GetComponent<BotPath>();

//         checkpointIndex = -1;
//         secondClock = 1;

//         DetermineTurnFactorThresholds();
//     }

//     void Update() 
//     {

//         // Threshold calculations
//         if(tfThresholdCalcTime > 0) {
//             tfThresholdCalcTime -= Time.deltaTime;
//             if(tfThresholdCalcTime <= 0) {
//                 DetermineTurnFactorThresholds();
//                 tfThresholdCalcTime = tfThresholdCalcFrequency;
//             }
//         }

//         // Average speed calculations
//         if(secondClock > 0) {
//             secondClock -= Time.deltaTime;
//             if(secondClock <= 0) {
//                 secondClock = 1;

//                 float trackSpeedsTotal = (averageTrackSpeed*(averageTrackSpeedCount - averageTrackSpeedCount == averageTrackSpeedCountLimit ? 1 : 0)) + kc.TrackSpeed;
//                 if(averageTrackSpeedCount < averageTrackSpeedCountLimit) averageTrackSpeedCount += 1;
//                 averageTrackSpeed = trackSpeedsTotal / averageTrackSpeedCount;

//             }
//         }

//         Waypoints waypoints = pt.GetWaypoints();
//         if(checkpointIndex != pt.waypointIndex) CheckpointIndexUpdated();
//         checkpointIndex = pt.waypointIndex;

//         targetPosition = DetermineTargetPosition();

//         turnAmount = bp.GetTurnAmount(checkpointIndex);
//         if(Vector3.Distance(transform.position, targetPosition) < 1f) {
//             turnFactor = bp.GetTurnFactor(checkpointIndex, turnFactorCount);
//         } else {
//             turnFactor = bp.GetSmartTurnFactor(kc.KartForward, checkpointIndex, turnFactorCount);
//         }

//         forward = kc.KartForward;
//         forward.y = 0;
//         directionToTarget = bp.ReadPath(transform.position).Item2.normalized;
//         directionToTarget.y = 0;

//         dot = Vector3.Dot(forward.normalized, directionToTarget.normalized); 
//         cross = Vector3.Cross(forward.normalized, directionToTarget.normalized).y;

//         // Determine if we're stuck/unstuck
//         if(stuck && dot > 0) {
//             stuckLR = -turnLR;
//             stuck = false;
//         } else if(!stuck && dot <= 0 && averageTrackSpeedCount == averageTrackSpeedCountLimit && averageTrackSpeed <= 0.33f) {
//             stuckLR = 0;
//             stuck = true;
//         }

//         turnLR = (int)Mathf.Sign(cross); // Get turn direction, +1 for right, -1 for left
//         if(turnLR == 0) turnLR = (int)Mathf.Sign(turnFactor);
//         if(stuck) {
//             turnValue = stuckLR;
//         } else if(!kc.DriftInput) {
//             turnValue = turnLR * dotProductToTurn.Evaluate(Mathf.Abs(dot)); // Convert turnLR into a turn value for use in turn input
//         } else {
//             // Drift engaged, ensure that turn input matches drift direction
//             turnValue = turnLR * dotProductToDriftTurn.Evaluate(Mathf.Abs(dot)); // Convert turnLR into a turn value for use in turn input
//         }

//         turnInput = new(turnValue, 0);
//         kc.TurnInput = turnInput;

//         throttleInput = DetermineThrottle();
//         kc.ThrottleInput = throttleInput;

//         driftInput = !stuck && kc.CanDriftEngage && Math.Abs(bp.GetTurnFactor(checkpointIndex, turnFactorCount)) > tfThresholdDrift;
//         if(!kc.DriftInput && driftInput) DriftEngaged();
//         kc.DriftInput = driftInput;

//         boostInput = kc.ActivelyBoosting || (turnFactor < tfThresholdThrottle && kc.BoostRatio >= kc.requiredBoostPercentage);
//         kc.BoostInput = boostInput;

//         // Debug code
//         // Red = direction to waypoint
//         // Blue = forward
//         // Green = Line to target position
//         Debug.DrawRay(transform.position+Vector3.up*0.5f, directionToTarget, Color.red);
//         Debug.DrawRay(transform.position+Vector3.up*0.5f, forward, Color.blue);       
//         Debug.DrawLine(transform.position, targetPosition, Color.green);
//         bool fu = false;
//         if(fu) print(throttleState); // Get rid of  "throttleState isn't being used warning"
//         // End debug code

//     }

//     /* Determine methods, take values from brain and convert them to inputs/other values */

//     private Vector3 DetermineTargetPosition() 
//     {
//         return pt.GetNextWaypoint().position;
//     }

//     private bool GetKartForwardWaypointIntersection() 
//     {
//         int targetColliderIndex = checkpointIndex + 1;
//         if(targetColliderIndex > pt.GetWaypoints().Count) targetColliderIndex = 0;
//         foreach (RaycastHit hit in Physics.RaycastAll(transform.position + kc.KartForward*2, kc.KartForward, Mathf.Infinity, 1 << 6)) {
//             if (hit.collider.isTrigger && hit.collider.gameObject.transform.GetSiblingIndex() == targetColliderIndex) {
//                 return true;
//             }
//         }
//         return false;
//     }

//     // This method is kind of expensive, lets only run it on an interval (tfThresholdCalcFrequency)
//     private void DetermineTurnFactorThresholds() 
//     {
//         tfThresholdMax = DetermineTurnFactorThreshold(1f);
//         tfThresholdBrake = DetermineTurnFactorThreshold(tfThresholdSingleBrake);
//         tfThresholdDrift = DetermineTurnFactorThreshold(tfThresholdSingleDrift);
//         tfThresholdThrottle = DetermineTurnFactorThreshold(tfThresholdSingleThrottle);
//     }

//     private float DetermineTurnFactorThreshold(float turnAmount) { return turnAmount*turnFactorCount; }

//     private float DetermineThrottle() 
//     {
//         throttleState = "None";
//         if(kc.airtime > 0.15f) return 0;
//         if(stuck) return -1f;

//         if(Math.Abs(turnFactor) >= tfThresholdBrake) {            // Priority 1a
//             float range = tfThresholdMax - tfThresholdBrake;
//             float amountInRange = turnFactor - tfThresholdBrake;
//             throttleState = "Braking";
//             return -(amountInRange/range);
//         } else if(Math.Abs(turnFactor) >= tfThresholdDrift || Math.Abs(turnFactor) >= tfThresholdThrottle) {
//             throttleState = "Drift/Easing";
//             return turnFactorToThrottle.Evaluate(Mathf.Abs(turnFactor)/(0.5f*turnFactorCount));
//         } else if(kc.TrackSpeed < kc.CurrentMaxSpeed) { // Priority 2
//             throttleState = "Full throttle";
//             if(throttleInput < 0.75f) throttleInput = 0.75f;
//             return Mathf.Clamp01(throttleInput + 3*Time.deltaTime);
//         }
//         return 0f;
//     }

//     /* Action callbacks */
//     private void CheckpointIndexUpdated() 
//     {

//     }

//     private void DriftEngaged() 
//     {

//     }

//     private void SayMessage(String message) {
//         print("BotDriver " + gameObject.name + ": " + message);
//     }

// }