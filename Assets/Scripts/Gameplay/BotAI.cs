using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotAI : MonoBehaviour
{
    List<Transform> _wayPoints = new List<Transform> { GameObject.Find("Cube").GetComponentInChildren(Transform) }
    _wayPoints
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        WayPointMovement();
    }

    private void WayPointMovement()
    {
        if (_wayPoints.Count > 0 && _wayPoints[_currentTarget] != null)
        {
            _kartAgent.SetDestination(_wayPoints[_currentTarget].position);
        }
    }
}
