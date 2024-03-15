using System;
using System.Collections.Generic;
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
    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    [Header("Runtime Fields"), SerializeField, SyncVar(OnChange = nameof(RacePhaseChange))] 
    private RacePhase phase; 
    [SerializeField] 
    private float raceTime;
    private bool waitingForPlayerInput = false;

    [SerializeField, SyncObject] private readonly SyncList<PlayerData> placements = new();

    private void Awake()
    {
        gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;

        raceTime = -100;

        Debug.LogWarning("Game currently detects initial phase as late join because it's looking for the PlayerInputManager's player count, when it should be looking at the kart player count");

        if(!base.IsServer)
            return;

        // Initialize phases
        if(PlayerObjectManager.Instance == null) {
            waitingForPlayerInput = true;
        } else if(PlayerObjectManager.Instance.GetPlayerInputManager().playerCount == 0) {
            phase = RacePhase.LATE_JOIN;
        } else if(kartLevelManager.HasRaceCamera) {
            phase = RacePhase.INTRO_ANIMATION;
        } else {
            phase = RacePhase.COUNTDOWN;
            PrepareRace();
        }

        // Spawn bots if we're not waiting on a late join
        // If we are waiting for a late join, the bots will be spawn after said player joins
        if(phase != RacePhase.LATE_JOIN) 
            gameplayManager.KartSpawner.SpawnBots();

    }

    private void Update() 
    {
        if(waitingForPlayerInput && PlayerObjectManager.Instance != null) {
            waitingForPlayerInput = false;
            // Re-call race phase change since we probably missed something important by not having player input
            RacePhaseChange(RacePhase.LATE_JOIN, phase, false);
        }

        if(!waitingForPlayerInput && phase != RacePhase.LATE_JOIN && phase != RacePhase.INTRO_ANIMATION)
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
                bool introAnimComplete = !kartLevelManager.HasRaceCamera || !kartLevelManager.RaceCamera.Animating;
                if(introAnimComplete && gameplayManager.PlayerManager.AllPlayersReady)
                    phase = RacePhase.COUNTDOWN;
                break;
            case RacePhase.COUNTDOWN:
                if(raceTime >= 0)
                    phase = RacePhase.RACING;
                break;
            case RacePhase.RACING:
                bool allHumanPlayersFinished = true;
                foreach(GameObject kartObj in gameplayManager.PlayerManager.kartObjects) {
                    KartManager km = KartBehavior.LocateManager(kartObj);
                    if(km.IsHuman && km.GetPositionTracker().raceCompletion < 1) {
                        allHumanPlayersFinished = false;
                        break;
                    }
                }
                if(allHumanPlayersFinished) {
                    PopulatePlacements(); // Populate placements here so that we can ensure the results are ready once clients need to show results.
                    phase = RacePhase.FINISHED;
                }
                break;
            case RacePhase.FINISHED:
                break;
        }

    }

    private void RacePhaseChange(RacePhase prev, RacePhase current, bool asServer) {
        print("race phase changed to " + current);

        // If this is the case, waitingForPlayerObjectManager will be true and phase will be changed again
        if(waitingForPlayerInput) {
            return;
        }

        PlayerInputManager pim = PlayerObjectManager.Instance.GetPlayerInputManager();

        switch(current) {
            case RacePhase.LATE_JOIN:
                if(!asServer)
                    pim.EnableJoining();
                break;
            case RacePhase.INTRO_ANIMATION:
                if(asServer) {
                    gameplayManager.KartSpawner.SpawnBots();
                    placements.Clear();
                }
                break;
            case RacePhase.COUNTDOWN:
                PrepareRace();
                break;
            case RacePhase.RACING:
                break;
            case RacePhase.FINISHED:
                if(!asServer) {
                    ScreenManager sm = kartLevelManager.ScreenManager;
                    sm.ResultsBuilder.gameObject.SetActive(true);
                    sm.ResultsBuilder.waitingForPlacements = true;
                    sm.ConnectPlayerInput(PlayerObjectManager.Instance.GetPlayerObjects()[0]);                    
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
            if(po.input.camera != null) {
                po.input.camera.enabled = true;
                if(po.PlayerIndex == 0) 
                    po.input.camera.GetComponent<AudioListener>().enabled = true;
            }
        });

        PlayerObjectManager.Instance.GetPlayerInputManager().splitScreen = true;

        // Disable main camera audio listener so we get player 0's camera audio
        kartLevelManager.RaceCamera.GetComponent<AudioListener>().enabled = false;

        // Load settings values
        raceTime = -Math.Abs(settings.startDelay);
    }

    [Server]
    public void PopulatePlacements() 
    {
        int kartCount = gameplayManager.PlayerManager.kartObjects.Count;

        List<KartManager> unsorted = new();
        List<KartManager> dnf = new(); 
        
        // Separate people that finished/didn't finish
        gameplayManager.PlayerManager.kartObjects.ForEach(ko => {
            KartManager km = KartBehavior.LocateManager(ko);
            if(km.GetPositionTracker().raceCompletion < 1)
                dnf.Add(km);
            else
                unsorted.Add(km);
        });

        List<KartManager> sorted = new();

        // Sort finished racers by time
        while(unsorted.Count > 0) {
            float smallestRaceTime = float.MaxValue;
            KartManager smallestKM = null;

            foreach(KartManager manager in unsorted) {
                PositionTracker pt = manager.GetPositionTracker();
                // Check if this is the lowest finish time
                if(pt.raceFinishTime < smallestRaceTime) {
                    smallestRaceTime = pt.raceFinishTime;
                    smallestKM = manager;
                }
            }

            if(smallestKM == null)
                throw new InvalidOperationException("Failed to select next fastest kart.");

            unsorted.Remove(smallestKM);
            sorted.Add(smallestKM);
        }

        // Sort unfinished racers by race completion
        while(dnf.Count > 0) {
            float highestRaceCompletion = float.MinValue;
            KartManager hrcKM = null;

            foreach(KartManager manager in dnf) {
                PositionTracker pt = manager.GetPositionTracker();
                // Check if this is the lowest finish time
                if(pt.raceCompletion > highestRaceCompletion) {
                    highestRaceCompletion = pt.raceCompletion;
                    hrcKM = manager;
                }
            }

            if(hrcKM == null)
                throw new InvalidOperationException("Failed to select next highest race completion.");

            dnf.Remove(hrcKM);
            sorted.Add(hrcKM);
        }

        placements.Clear();
        sorted.ForEach(km => placements.Add(km.GetPlayerData()));

        print("server populated " + placements.Count);

    }

    public float RaceTime { get { return raceTime; }}
    public bool CanMove { get { return raceTime >= 0; } }

    public SyncList<PlayerData> GetPlacements() { return placements; }

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