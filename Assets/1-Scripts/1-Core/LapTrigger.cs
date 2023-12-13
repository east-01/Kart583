using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapTrigger : MonoBehaviour
{
    private LapManager _lm;

    private void Awake()
    {
        _lm = FindObjectOfType<LapManager>();
    }
    private void OnTriggerEnter(Collider other)
    {
        _lm.TriggerLap(other.gameObject, this);
        Debug.Log(other.gameObject.name + " has triggered" + this.name + " collider");
    }
}
