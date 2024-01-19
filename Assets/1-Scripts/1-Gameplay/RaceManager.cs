using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // Initialize phases
        if(PlayerObjectManager.Instance == null || PlayerObjectManager.Instance.GetPlayerInputManager().playerCount == 0) {
            phase = RacePhase.LATE_JOIN;
        } else if(GameplayManager.HasRaceCamera) {
            phase = RacePhase.INTRO_ANIMATION;
        } else {
            phase = RacePhase.COUNTDOWN;
            PrepareRace();
        }

        // Spawn bots if we're not waiting on a late join
        // If we are waiting for a late join, the bots will be spawn after said player joins
        if(phase != RacePhase.LATE_JOIN) 
            SpawnBots();

    }

    private void Update() 
    {

        // We'll attempt to escalate the race phase each Update()
        // Only allowed to escalate once per frame
        switch(phase) {
            case RacePhase.LATE_JOIN:
                PlayerInputManager pim = PlayerObjectManager.Instance.GetPlayerInputManager();
                if(pim.playerCount == 0) {
                    pim.EnableJoining();
                } else {
                    pim.DisableJoining();
                    SpawnBots();
                    phase = RacePhase.INTRO_ANIMATION;
                }
                break;
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
                    GameplayManager.ScreenManager.ConnectPlayerInput(PlayerObjectManager.Instance.GetPlayerObjects()[0]);
                }
                break;
        }

        // Don't do other race things if we're doing the intro animation or waiting for late join
        if(phase == RacePhase.LATE_JOIN || phase == RacePhase.INTRO_ANIMATION) return;

        raceTime += Time.deltaTime;

    }

    /** Prepare's the player objects and splitscreen manager for the race */
    public void PrepareRace() 
    {
        // Enable player cameras and splitscreen, ensure we're on Gameplay control map
        PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => {
            po.input.SwitchCurrentActionMap("Gameplay");
            po.input.camera.enabled = true;
            if(po.PlayerIndex == 0) po.input.camera.GetComponent<AudioListener>().enabled = true;
        });
        GameplayManager.PlayerInputManager.splitScreen = true;

        // Disable main camera audio listener so we get player 0's camera audio
        GameplayManager.RaceCamera.GetComponent<AudioListener>().enabled = false;

        // Load settings values
        raceTime = -Math.Abs(settings.startDelay);
    }

    public void SpawnBots() 
    {
        PlayerManager pm = GameplayManager.PlayerManager;
        if(settings.bots) { // Use pm.BotPlayerCount == 0 so that we don't spawn more bots after we've already spawned them.
            int botsToSpawn = Math.Min(settings.botLimit, PlayerManager.PlayerLimit-pm.KartCount);
            for(int i = 0; i < botsToSpawn; i++) {
                pm.SpawnBot();
            }
        }
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
    LATE_JOIN, INTRO_ANIMATION, COUNTDOWN, RACING, FINISHED
}