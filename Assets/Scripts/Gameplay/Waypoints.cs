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

    public float GetSmartTurnFactor(Vector3 currentVector, int currentWaypointIndex, int lookAheadAmount) 
    {  
        Transform currentWaypoint = GetWaypointFromIndex(currentWaypointIndex);
        Vector3 a = (currentVector - GetPreviousWaypoint(currentWaypointIndex).position).normalized;
        a.y = 0;
        Vector3 b = (GetNextWaypoint(currentWaypointIndex).position - currentWaypoint.position).normalized;
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

    public bool saveTrackReadoutPseudoButton = false;
    void OnValidate()
    {
        if (saveTrackReadoutPseudoButton)
        {
            TrackReadout();

            saveTrackReadoutPseudoButton = false;
        }
    }

    public void TrackReadout() 
    {
        // Get the path of the script file
        string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(this));
        string scriptDirectory = Path.GetDirectoryName(scriptPath);

        // Define the output file path
        string outputPath = Path.Combine(scriptDirectory, "WaypointReadout.txt");

        // Open or create the output file
        using (StreamWriter writer = File.CreateText(outputPath))
        {
            // Loop through all waypoints
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform currentWaypoint = transform.GetChild(i);

                // Get turnAmount and turnFactor for the current waypoint
                float turnAmount = GetTurnAmount(i);
                float turnFactor = GetTurnFactor(i, 3); // Adjust the lookAheadAmount as needed

                // Format the row
                string row = string.Format("[{0}] turnAmount: {1}, turnFactor: {2}", i, turnAmount, turnFactor);

                // Write the row to the file
                writer.WriteLine(row);
            }
        }

        // Print a message in the console
        Debug.Log("Track readout saved to: " + outputPath);
    }

}
