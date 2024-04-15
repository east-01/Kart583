using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceEnd_GSM : IState
{

    public void OnStateEnter()
    {
        Debug.Log("WINNER!");
    }

    public void OnStateExit()
    {
        throw new System.NotImplementedException();
    }

    public void OnTick()
    {
        throw new System.NotImplementedException();
    }
}
