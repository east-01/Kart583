using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(KartsIRManager))]
public class RaceManager : NetworkBehaviour
{

    public static float RACE_TIME = 60*60*30;

    /* ----- Settings fields ---- */
    public RaceSettings settings;

    /* ----- Runtime fields ----- */
    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    [Header("Runtime Fields"), SerializeField, SyncVar(OnChange = nameof(RacePhaseChange), SendRate = 0f)] 
    private RacePhase phase; 
    public delegate void RacePhaseChangeHandler(RacePhase previousPhase, RacePhase currentPhase);
    public event RacePhaseChangeHandler RacePhaseChanged;

    [SyncObject]
    private readonly SyncTimer raceTime = new();
    public float RaceTime => raceTime.Remaining;
    public float RaceTimeElapsed => raceTime.Elapsed;
    [SerializeField]
    private float raceTimeReadout;
    private float simulatedTimer;

    private int countdownSecond;

    /// <summary>
    /// Stores raceFinishTime first in RaceCompleted(), then gets position and point data in PopulatePlacements()
    /// </summary>
    [SyncObject] 
    private readonly SyncDictionary<string, RacePlacementData> placements = new();

    private void Awake()
    {
        gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;

        raceTime.StopTimer(true);

        if(base.IsClientOnly)
            return;

        // Initialize phases
        /*if(PlayerObjectManager.Instance == null) {
            waitingForPlayerInput = true; // TODO: This is really dumb: we should only be waiting for player input on clients
            Debug.LogWarning("This is really dumb: we should only be waiting for player input on clients");
        } else */if(gameplayManager.GameLobby == null) {
            phase = RacePhase.WAITING_FOR_LOBBY;
        } else {
            InitializeWithLobby();
        }

    }

    public void InitializeWithLobby() {
        if(gameplayManager.GameLobby.PlayerCount == 0) {
            phase = RacePhase.LATE_JOIN;
        } else if(gameplayManager.PlayerManager.HumanPlayerCount < gameplayManager.GameLobby.PlayerCount) {
            phase = RacePhase.WAITING_FOR_PLAYERS;
        } else {
            phase = RacePhase.COUNTDOWN;
            PrepareRace();
        }
    }

    private void OnEnable() { raceTime.OnChange += RaceTime_OnChange; }
    private void OnDisable() { raceTime.OnChange -= RaceTime_OnChange; }

    private void Update() 
    {
        raceTime.Update(Time.deltaTime);

        if(!base.IsServer)
            return;

        // We'll attempt to escalate the race phase each Update()
        // Only allowed to escalate once per frame
        switch(phase) {
            case RacePhase.WAITING_FOR_LOBBY:
                if(gameplayManager.GameLobby != null)
                    InitializeWithLobby();
                break;
            case RacePhase.LATE_JOIN:
                break;
            case RacePhase.WAITING_FOR_PLAYERS:
                // TODO: Add a timer that kicks the player if they don't ready up by said time
                // bool introAnimComplete = !kartLevelManager.HasRaceCamera || !kartLevelManager.RaceCamera.Animating;
                // TODO: Add intro anim back in
                KartsIRManager playerManager = gameplayManager.PlayerManager;
                bool allPlayersReady = playerManager.AllPlayersReady && playerManager.HumanPlayerCount == gameplayManager.GameLobby.PlayerCount;
                bool allBotsReady = gameplayManager.PlayerManager.BotPlayerCount == gameplayManager.PlayerManager.BotsToSpawn;
                bool needToSpawnBots = gameplayManager.RaceManager.settings.Bots && playerManager.BotPlayerCount == 0 && playerManager.BotsToSpawn > 0;
                // Two tracks if we're spawning bots or not
                if(needToSpawnBots) {
                    if(allPlayersReady) {
                        playerManager.SpawnBots();
                    } else if(allBotsReady) {
                        phase = RacePhase.COUNTDOWN;
                    }
                } else {
                    if(allPlayersReady)
                        phase = RacePhase.COUNTDOWN;
                }
                break;
            case RacePhase.COUNTDOWN:
                break;
            case RacePhase.RACING:
                bool allHumanPlayersFinished = true;
                foreach(GameObject kartObj in gameplayManager.PlayerManager.kartObjects) {
                    KartManager km = KartBehavior.LocateManager(kartObj);
                    if(!km.IsHuman)
                        continue;
                    if(km.GetPositionTracker().RaceCompletion < 1 || !placements.ContainsKey(km.GetPlayerData().uuid)) {
                        allHumanPlayersFinished = false;
                        break;
                    }
                }
                if(allHumanPlayersFinished) {
                    FinalizePlacements(); // Populate placements here so that we can ensure the results are ready once clients need to show results.
                    phase = RacePhase.FINISHED;
                }
                break;
            case RacePhase.FINISHED:
                break;
        }

    }

    private void RacePhaseChange(RacePhase prev, RacePhase current, bool asServer) {

        // Getting double-calls from the syncvar, this just makes sure we block a double call in a host instance.
        if(base.IsHost && !asServer)
            return;

        BLog.Log($"Race phase changed to {current}", LogChannel.GameplayManager);

        // Call phase change event
        RacePhaseChanged?.Invoke(prev, current);

        PlayerInputManager pim = PlayerObjectManager.Instance.GetPlayerInputManager();

        switch(current) {
            case RacePhase.LATE_JOIN:
                if(!asServer)
                    pim.EnableJoining();
                break;
            case RacePhase.WAITING_FOR_PLAYERS:
                if(asServer)
                    placements.Clear();
                break;
            case RacePhase.COUNTDOWN:
                if(asServer) {
                    if(DevSettings.Settings.OverrideRaceProgressAtStart)
                        phase = RacePhase.RACING;
                    else
                        raceTime.StartTimer(settings.startDelay, true);
                        
                    placements.Clear();

                    PrepareRace();
                }
                break;
            case RacePhase.RACING:
                if(asServer)
                    raceTime.StartTimer(RACE_TIME, true);
                break;
            case RacePhase.FINISHED:
                IGScreenMenuController sm = kartLevelManager.ScreenManager;
                sm.OpenSubMenu(IGScreenMenuController.RESULTS_MENU_ID);
                sm.ResultsMenuController.waitingForPlacements = true;
                break;
        }

        if(current != RacePhase.LATE_JOIN) 
            pim.DisableJoining();
    }

    private void RaceTime_OnChange(SyncTimerOperation op, float prev, float next, bool asServer) 
    {
        if(!asServer)
            return;
        if(op == SyncTimerOperation.Finished) {
            if(phase == RacePhase.COUNTDOWN)
                phase = RacePhase.RACING;
            else if(phase == RacePhase.RACING) {
                FinalizePlacements();
                phase = RacePhase.FINISHED;
            }
        } else if(op == SyncTimerOperation.Start)
            simulatedTimer = next;
    }

    public override void OnStartClient() 
    {
        base.OnStartClient();
        RacePhaseChange(RacePhase.LATE_JOIN, phase, false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcPassLateJoin() {
        PassLateJoin();
    }

    /// <summary>
    /// Used by clients after they join
    /// </summary>
    public void PassLateJoin() 
    {
        if(!base.IsServer) {
            ServerRpcPassLateJoin();
            return;
        }
        if(phase == RacePhase.LATE_JOIN)
            phase = RacePhase.WAITING_FOR_PLAYERS;
    }

    /// <summary>
    /// Called when we enter the countdown phase.
    /// Prepares the client for the race, enabling splitscreen and player components
    /// </summary>
    [ObserversRpc]
    public void PrepareRace() 
    {
        BLog.Log("Preparing race", LogChannel.GameplayManager, 3);

        // Enable player cameras and splitscreen, ensure we're on Gameplay control map
        PlayerObjectManager.Instance.GetPlayerObjects().ForEach(po => {
            po.input.enabled = true;
            po.input.SwitchCurrentActionMap("Gameplay");
            if(po.input.camera != null) {
                po.input.camera.enabled = true;
                if(po.PlayerIndex == 0 && !CoreManager.IsServerOnly) 
                    po.input.camera.GetComponent<AudioListener>().enabled = true;
            }
        });

        PlayerObjectManager.Instance.GetPlayerInputManager().splitScreen = true;

        // Disable main camera audio listener so we get player 0's camera audio
        CoreManager.Instance.GetComponent<AudioListener>().enabled = false;
        // kartLevelManager.RaceCamera.GetComponent<AudioListener>().enabled = false;
    }

    /// <summary> Server RPC calling RaceManager#CompletedRace </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcCompletedRace(PlayerData data, float raceCompletion) { CompletedRace(data, raceCompletion); }
    /// <summary>
    /// Notify the server that this player has completed the race
    /// </summary>
    [Server]
    public void CompletedRace(PlayerData data, float raceCompletion) 
    {
        if(placements.ContainsKey(data.uuid))
            return;

        float raceFinishTime = RaceTimeElapsed;
        if(raceCompletion < 1)
            raceFinishTime = -1;

        RacePlacementData rpd = new() {
            raceFinishTime = raceFinishTime,
            raceCompletion = raceCompletion
        };

        placements.Add(data.uuid, rpd);
    }

    [Server]
    public void FinalizePlacements() 
    {
        // Ensure everyone is in the placements array
        foreach(GameObject kartObject in gameplayManager.PlayerManager.kartObjects) {
            KartManager kartManager = KartBehavior.LocateManager(kartObject);
            CompletedRace(kartManager.GetPlayerData(), kartManager.GetPositionTracker().RaceCompletion);
        }

        Dictionary<string, RacePlacementData> sortedPlacements = placements.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
        int position = 0;
        foreach(string uuid in sortedPlacements.Keys) {
            RacePlacementData storedRPD = placements[uuid];
            storedRPD.position = position;
            storedRPD.pointsAwarded = (12 - position) * 2; // TODO: ELO based points system
            placements[uuid] = storedRPD; // Required for SyncDictionary 
            position++;
        }
    }

    public RacePhase Phase { get { return phase; } }
    public bool CanMove { get { return (phase == RacePhase.RACING || phase == RacePhase.FINISHED) && RaceTime >= 0; } }

    public SyncDictionary<string, RacePlacementData> GetPlacements() { return placements; }

}

[Serializable]
public struct RaceSettings 
{
    [SerializeField] private int laps;
    public readonly int Laps { get {
        if(DevSettings.Settings.OverrideLapCount)
            return DevSettings.Settings.LapCount;
        else
            return laps;
    } }
    public float startDelay;
    public float startBoostPercent;
    [SerializeField] private bool bots;
    public readonly bool Bots { get {
        if(DevSettings.Settings.OverrideBots)
            return DevSettings.Settings.Bots;
        else
            return bots;
    } }
    public int botLimit;
}

/// <summary>
/// Data populated by the server to notify clients what they're placement results are
/// </summary>
[Serializable]
public struct RacePlacementData : IComparable<RacePlacementData>
{
    /* Data provided by CompleteRace() */
    public float raceFinishTime;
    public float raceCompletion; // Used in CompareTo to sort the RacePlacementData in RaceManager

    /* Data provided by PopulatePlacements() */
    public int position;
    public int pointsAwarded;

    public readonly int CompareTo(RacePlacementData other)
    {        
        if(raceCompletion >= 1 && other.raceCompletion < 1)
            return -1;
        else if(raceCompletion < 1 && other.raceCompletion >= 1)
            return 1;
        else if(raceCompletion >= 1 && other.raceCompletion >= 1) {
            // Both have completed race, return raceFinishTime comparison (lower is better)
            return raceFinishTime.CompareTo(other.raceFinishTime);
        } else {
            // Neither have completed race, return raceCompletion compraison (higher is better)
            return other.raceCompletion.CompareTo(raceCompletion);
        }
    }
}

public enum RaceType 
{

}

public enum RacePhase
{
    WAITING_FOR_LOBBY, LATE_JOIN, WAITING_FOR_PLAYERS, COUNTDOWN, RACING, FINISHED
}