using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceCamera : MonoBehaviour
{

    void Update()
    {
        RaceManager rm = GameplayManager.RaceManager;
        if(rm.raceTime > 0) return;

        float timeLeft = Math.Abs(rm.raceTime);

    }

    // public void StartAnimation()

}
