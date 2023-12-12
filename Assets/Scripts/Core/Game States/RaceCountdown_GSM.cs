using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceCountdown_GSM : IState
{
    private GameStateManager _gsm;
    private Countdown _cd;
    public RaceCountdown_GSM(GameStateManager gsm)
    {
        _gsm = gsm;
    }
    public void OnStateEnter()
    {
        _gsm.CountdownPrefab.GetComponent<Countdown>().StartTimer = true;
    }

    public void OnStateExit()
    {

    }

    public void OnTick()
    {
        if (_cd == null)
        {
            _gsm.IsTimerEnded = true;
        }
    }
}
