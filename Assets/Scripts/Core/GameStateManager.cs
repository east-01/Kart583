using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public Camera Cam;

    public GameObject[] PlayerKarts;

    public GameObject[] BotsPrefabs;

    public GameObject CountdownPrefab;

    private StateMachine _stateMachine;

    public bool IsIntroFinished = false;
    public bool IsTimerEnded = false;
    public int LapsCompleted;

    

    private void Awake()
    {
        _stateMachine = new StateMachine();

        var levelEnter = new LevelEnter_GSM(this);
        var countdown = new RaceCountdown_GSM(this);
        var activeRace = new ActiveRace_GSM();
        var raceEnd = new RaceEnd_GSM();

        _stateMachine.AddTranstion(levelEnter, countdown, IntroFinished());
        _stateMachine.AddTranstion(countdown, activeRace, CountdownComplete());
        _stateMachine.AddTranstion(activeRace, raceEnd, RaceComplete());


        Func<bool> IntroFinished() => () => IsIntroFinished;
        Func<bool> CountdownComplete() => () => IsTimerEnded;
        Func<bool> RaceComplete() => () => LapsCompleted >= 3;

        _stateMachine.SetState(levelEnter);
    }

    // Update is called once per frame
    void Update()
    {
        _stateMachine.Tick();
    }
}
