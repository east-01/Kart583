using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(KartsIRManager))]
public class RaceManager : NetworkBehaviour
{

    /* ----- Settings fields ---- */
    public RaceSettings settings;

    /* ----- Runtime fields ----- */
    [Header("Runtime Fields"), SerializeField, SyncVar(OnChange = nameof(RacePhaseChange))] private RacePhase phase; 
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
            GameplayManager.PlayerManager.SpawnBots();

    }

    private void Update() 
    {
        // Don't do other race things if we're doing the intro animation or waiting for late join
        if(phase != RacePhase.LATE_JOIN && phase != RacePhase.INTRO_ANIMATION)
            raceTime += Time.deltaTime;

        if(!base.IsServer)
            return;

        // We'll attempt to escalate the race phase each Update()
        // Only allowed to escalate once per frame
        switch(phase) {
            case RacePhase.LATE_JOIN:
                break;
            case RacePhase.INTRO_ANIMATION:
                // TODO: Add a timer that kicks the player if they don't ready up by said time
                bool allPlayersReady = true;
                foreach(GameObject obj in GameplayManager.PlayerManager.kartObjects) {
                    if(!KartBehavior.LocateManager(obj).GetPlayerData().ready) {
                        allPlayersReady = false;
                        break;
                    }
                }
                bool introAnimComplete = !GameplayManager.HasRaceCamera || !GameplayManager.RaceCamera.Animating;
                if(introAnimComplete && allPlayersReady)
                    phase = RacePhase.COUNTDOWN;
                break;
            case RacePhase.COUNTDOWN:
                if(raceTime >= 0)
                    phase = RacePhase.RACING;
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
                if(allHumanPlayersFinished)
                    phase = RacePhase.FINISHED;
                break;
            case RacePhase.FINISHED:
                break;
        }

    }

    private void RacePhaseChange(RacePhase prev, RacePhase current, bool asServer) {
        PlayerInputManager pim = PlayerObjectManager.Instance.GetPlayerInputManager();

        switch(current) {
            case RacePhase.LATE_JOIN:
                if(!asServer)
                    pim.EnableJoining();
                break;
            case RacePhase.INTRO_ANIMATION:
                if(asServer) 
                    GameplayManager.PlayerManager.SpawnBots();
                break;
            case RacePhase.COUNTDOWN:
                PrepareRace();
                break;
            case RacePhase.RACING:
                break;
            case RacePhase.FINISHED:
                if(!GameplayManager.ScreenManager.ResultsBuilder.ResultsShown) {
                    GameplayManager.ScreenManager.ResultsBuilder.ShowResults();
                    GameplayManager.ScreenManager.ConnectPlayerInput(PlayerObjectManager.Instance.GetPlayerObjects()[0]);
                }
                break;
        }

        if(current != RacePhase.LATE_JOIN) 
            pim.DisableJoining();
    }

    public override void OnStartClient() 
    {
        base.OnStartClient();
        RacePhaseChange(RacePhase.LATE_JOIN, phase, false);
    }

    /// <summary>
    /// Used by clients after they join
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PassLateJoin() 
    {
        if(phase == RacePhase.LATE_JOIN)
            phase = RacePhase.INTRO_ANIMATION;
    }

    /** Prepare's the player objects and splitscreen manager for the race */
    public void PrepareRace() 
    {
        // Enable player cameras and splitscreen, ensure we're on Gameplay control map
        PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => {
            po.input.enabled = true;
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