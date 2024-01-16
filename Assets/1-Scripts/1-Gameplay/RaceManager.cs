using System;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class RaceManager : MonoBehaviour
{

    /* ----- Settings fields ---- */
    public RaceSettings settings;

    /* ----- Runtime fields ----- */
    [Header("Runtime Fields"), SerializeField] private RacePhase phase; 
    [SerializeField] private float raceTime;

    private void Start()
    {
        raceTime = -100;

        // Spawn in bots where necessary
        PlayerManager pm = GameplayManager.PlayerManager;
        if(settings.bots) { // Use pm.BotPlayerCount == 0 so that we don't spawn more bots after we've already spawned them.
            int botsToSpawn = Math.Min(settings.botLimit, PlayerManager.PlayerLimit-pm.KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                pm.SpawnBot();
            }
        }

        // Initialize phases
        if(GameplayManager.HasRaceCamera) {
            phase = RacePhase.INTRO_ANIMATION;
        } else {
            phase = RacePhase.COUNTDOWN;
            PrepareRace();
        }

    }

    private void Update() 
    {

        // We'll attempt to escalate the race phase each Update()
        // Only allowed to escalate once per frame
        switch(phase) {
            case RacePhase.INTRO_ANIMATION:
                if(!GameplayManager.HasRaceCamera || !GameplayManager.RaceCamera.Animating) {
                    phase = RacePhase.COUNTDOWN;
                    PrepareRace();
                }
                break;
            case RacePhase.COUNTDOWN:
                if(raceTime >= 0) {
                    phase = RacePhase.RACING;
                }
                break;
            case RacePhase.RACING:
                bool allHumanPlayersFinished = true;
                foreach(GameObject kartObj in GameplayManager.PlayerManager.kartObjects) {
                    KartManager km = KartBehavior.LocateManager(kartObj);
                    if(km.IsHuman && km.GetPositionTracker().raceCompletion < 1) {
                        allHumanPlayersFinished = false;
                        break;
                    }
                }
                if(allHumanPlayersFinished) {
                    phase = RacePhase.FINISHED;
                }
                break;
            case RacePhase.FINISHED:
                if(!GameplayManager.ScreenManager.ResultsBuilder.ResultsShown) {
                    GameplayManager.ScreenManager.ResultsBuilder.ShowResults();
                }
                break;
        }

        // Don't do other race things if we're doing the intro animation
        if(phase == RacePhase.INTRO_ANIMATION) return;

        raceTime += Time.deltaTime;

    }

    /** Prepare's the player objects and splitscreen manager for the race */
    public void PrepareRace() 
    {
        // Enable player cameras and splitscreen
        PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => {
            po.input.camera.enabled = true;
            if(po.PlayerIndex == 0) po.input.camera.GetComponent<AudioListener>().enabled = true;
        });
        GameplayManager.PlayerInputManager.splitScreen = true;

        // Disable main camera audio listener so we get player 0's camera audio
        GameplayManager.RaceCamera.GetComponent<AudioListener>().enabled = false;

        // Load settings values
        raceTime = -Math.Abs(settings.startDelay);
    }

    public float RaceTime { get { return raceTime; }}
    public bool CanMove { get { return raceTime >= 0; } }

}

[Serializable]
public struct RaceSettings 
{
    public int laps;
    public float startDelay;
    public float startBoostPercent;
    public bool bots;
    public int botLimit;
    public RaceSettings(int laps, float startDelay, float startBoostPercent, bool bots, int botLimit) {
        this.laps = laps;
        this.startDelay = startDelay;
        this.startBoostPercent = startBoostPercent;
        this.bots = bots;
        this.botLimit = botLimit;
    }
}

public enum RaceType 
{

}

public enum RacePhase
{
    INTRO_ANIMATION, COUNTDOWN, RACING, FINISHED
}