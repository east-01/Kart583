using System;
using System.Collections.Generic;
using UnityEngine;

/** This class is a wrapper between the Waypoints class and the BotDriver class.
  * This class will pick waypoints from inside each checkpoint box bounds, then
  *   the optimal path can be read from any position on the track. 
  * Read more about the optimal path in ReadPath(). */
[RequireComponent(typeof(PositionTracker))]
public class BotPath : MonoBehaviour
{

    public List<Vector3> waypointPositions;
    /** Cache the curve and it's associated waypoint */
    public Dictionary<int, BezierCurve> curveSegments;

    public bool randomizeWaypoints = true;
    public bool drawBotPath = true;
    public int drawResolution = 10;
    public float drawFrequency = 1;
    public float scale = 5;

    void Start() 
    {
        if(randomizeWaypoints)
            PickPath();
        else
            PickWaypointPath();
    }

    void Update()
    {
        if(drawBotPath) DrawPath();
        

    }

    /** For each checkpoint bounding box, we'll pick a random point inside it. 
      * In order for a point to be valid, two conditions have to be met:
      *   1. The previous waypoint has a line of sight to the picked one.
      *   2. The waypoint has a clear nm radius where n can be chosen by editor. */
    public void PickPath() 
    {
        waypointPositions = new List<Vector3>();
        curveSegments = new Dictionary<int, BezierCurve>();
        int i = 0;
        foreach(Transform pos in GameplayManager.Waypoints.transform) {
            BoxCollider collider = pos.gameObject.GetComponent<BoxCollider>();

            bool foundPoint = false;
            for(int pickAttempt = 0; pickAttempt < 100; pickAttempt++) {

                // Pick point
                Vector3 center = collider.bounds.center;
                Vector3 size = collider.bounds.size;

                float randomX = UnityEngine.Random.Range(center.x - size.x/2f, center.x + size.x/2f);
                float randomZ = UnityEngine.Random.Range(center.z - size.z/2f, center.z + size.z/2f);
                RaycastHit hit;
                Physics.Raycast(new Vector3(randomX, center.y, randomZ), -Vector3.up, out hit, 10f);
                if(hit.point == null) continue;
                
                Vector3 testPos = new(randomX, pos.position.y, randomZ);

                // Check if point is within radius of checkpoint
                if(Vector3.Distance(pos.position, testPos) >= (Math.Max(size.x, size.z)*0.8f)/2f) continue;

                // Check if point has line of sight
                // RaycastHit losHit;
                // Physics.Raycast(waypointPositions[waypointPositions.Count-1], testPos-waypointPositions[waypointPositions.Count-1], out losHit);
                // if(losHit.point != null) continue;

                // Check if waypoint has clear radius
                bool hasObstruction = false;
                Collider[] colliders = Physics.OverlapSphere(center, 2.5f);
                foreach(Collider c in colliders) {
                    bool isGround = Math.Abs(c.gameObject.transform.position.y) < 0.1f;
                    if(!isGround && c.gameObject.tag != "Kart" && c.gameObject.tag != "Waypoint") {
                        print("hit obstruction " + c.gameObject.name);
                        hasObstruction = true;
                        break;
                    }
                } 

                if(hasObstruction) continue;

                // If we passed checks, found point = true
                foundPoint = true;
                waypointPositions.Add(testPos);
                break;

            }

            if(!foundPoint) {
                Debug.LogError("Failed to find a usable position at checkpoint \"" + pos.gameObject.name + "\"");
                waypointPositions.Add(pos.position);
            }
            i++;
        }

        if(waypointPositions.Count != GameplayManager.Waypoints.Count) {
            Debug.LogError("Error: Botpath on \"" + gameObject.name + "\" failed to pick the same amount of waypoints as the defined waypoints.");
        }

    }

    public void PickWaypointPath() 
    {
        waypointPositions = new List<Vector3>();

        foreach(Transform pos in GameplayManager.Waypoints.transform) {
            waypointPositions.Add(pos.position);
        }
    }

    /** Read a position and tangent vector from the bezier curve path given
      *   an arbitrary progress value and waypoint index. 
      * This will return a (Vector3, Vector3) that holds the position in the
      *   first vector and the tangent vector in the second. */
    public (Vector3, Vector3) ReadPath(float progress, int waypointIndex) 
    {        

        if(!curveSegments.ContainsKey(waypointIndex)) {
            Vector3 wi = waypointPositions[waypointIndex]; 
            Vector3 wi_n1 = waypointPositions[WaypointAdder(waypointIndex, -1)];
            Vector3 wi_p1 = waypointPositions[WaypointAdder(waypointIndex, 1)];
            Vector3 wi_p2 = waypointPositions[WaypointAdder(waypointIndex, 2)];

            float localScalar = Math.Max(Math.Abs(GetTurnAmount(waypointIndex)), 0.1f);

            curveSegments.Add(waypointIndex, new BezierCurve(
        /*p0*/  wi,
        /*p1*/  wi + localScalar*scale*(wi_p1-wi_n1).normalized,
        /*p2*/  wi_p1 + localScalar*scale*(wi-wi_p2).normalized,
        /*p3*/  wi_p1
            ));
        }

        BezierCurve curve = curveSegments[waypointIndex];
        return (
            curve.CalculateBezierPoint(progress),
            curve.CalculateTangent(progress)
        );
    }

    /** Applies the bot's progress and currentWaypointIndex to ReadPath(float, float). */
    public (Vector3, Vector3) ReadPath(Vector3 position)
    {
        PositionTracker pt = GetComponent<PositionTracker>();
        // Progress along curve (aka t), lets try using segment progress for now.
        float progress = EstimateProgress();
        return ReadPath(progress, pt.waypointIndex);
    }

    public float EstimateProgress() 
    {
        PositionTracker pt = GetComponent<PositionTracker>();
        return curveSegments[pt.waypointIndex].ClosestEstimate(transform.position, 2, pt.segmentCompletion);
    }

    public float GetTurnAmount(int waypointIndex) 
    {
        Vector3 a = (waypointPositions[waypointIndex] - waypointPositions[WaypointAdder(waypointIndex, -1)]).normalized;
        a.y = 0;
        Vector3 b = (waypointPositions[WaypointAdder(waypointIndex, 1)] - waypointPositions[waypointIndex]).normalized;
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

    public float GetSmartTurnFactor(Vector3 currentVector, int currentWaypointIndex, int lookAheadAmount) 
    {  
        Vector3 a = (waypointPositions[currentWaypointIndex] - waypointPositions[WaypointAdder(currentWaypointIndex, -1)]).normalized;
        a.y = 0;
        Vector3 b = (waypointPositions[WaypointAdder(currentWaypointIndex, 1)] - waypointPositions[currentWaypointIndex]).normalized;
        b.y = 0;

        float accumulator = Vector3.Cross(a, b).y;
        for(int i = 0; i < lookAheadAmount; i++) {
            float turnAmount = GetTurnAmount(currentWaypointIndex);
            accumulator += turnAmount;


            currentWaypointIndex += 1;
            if(currentWaypointIndex >= transform.childCount) 
                currentWaypointIndex = 0;
        }
        return accumulator;
    }

    /** Add to and subtract from the waypoint index and wrap if we go outside the
      *   bounds of waypoint amount. 
      * Supports positive and negative numbers. */
    public int WaypointAdder(int cur, int num) 
    {
        int max = waypointPositions.Count;

        int result = (cur + num) % max;
        if (result < 0) result += max; // Handle negative numbers

        return result;
    }

    private float drawCooldown;
    public void DrawPath() 
    {
        if(drawCooldown > 0) {
            drawCooldown -= Time.deltaTime;
            return;
        }

        Vector3? prevPos = null;
        for(int wpidx = 0; wpidx < waypointPositions.Count; wpidx++) {
            for(int i = 0; i < drawResolution; i++) {
                float progress = (float)i/drawResolution;
                Vector3 pos = ReadPath(progress, wpidx).Item1;

                if(prevPos.HasValue) {
                    Debug.DrawLine(prevPos.Value, pos, new Color(1f, 0.5f, 0), drawFrequency+0.05f);
                }  
                prevPos = pos;
            }
        }

        drawCooldown = drawFrequency;
    }

}
