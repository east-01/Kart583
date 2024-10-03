using System;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
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
    

    /* ----- Runtime fields ----- */
    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    private SyncVar<RacePhase> phase; 
    public RacePhase Phase => phase.Value;

    private readonly SyncTimer raceTime = new();
    public float RaceTime => raceTime.Remaining;
    public float RaceTimeElapsed => raceTime.Elapsed;

    private int countdownSecond;

#region Runtime fields
    private RaceSettings settings;
    /// <summary>
    /// The settings for the race, can be an editor field or set by code.
    /// </summary>
    public RaceSettings Settings => settings;
#endregion

#region Events
    public delegate void RacePhaseChangeHandler(RacePhase previousPhase, RacePhase currentPhase);
    /// <summary>
    /// Event call for when the race phase.Value is changed. Called 
    /// </summary>
    public event RacePhaseChangeHandler RacePhaseChanged;
#endregion

    /// <summary>
    /// Stores raceFinishTime first in RaceCompleted(), then gets position and point data in PopulatePlacements()
    /// </summary>
    private readonly SyncDictionary<string, RacePlacementData> placements = new();

    private void Awake()
    {
        gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;

        raceTime.StopTimer(true);

        phase.OnChange += RacePhaseChange;

        if(base.IsClientOnlyInitialized)
            return;

        // Initialize phases
        /*if(PlayerManager.Instance == null) {
            waitingForPlayerInput = true; // TODO: This is really dumb: we should only be waiting for player input on clients
            Debug.LogWarning("This is really dumb: we should only be waiting for player input on clients");
        } else */if(gameplayManager.KartLobby == null) {
            phase.Value  = RacePhase.WAITING_FOR_LOBBY;
        } else {
            InitializeWithLobby();
        }

    }

    private void OnDestroy() 
    {

    }

    public void InitializeWithLobby() {
        if(gameplayManager.KartLobby.PlayerCount == 0) {
            phase.Value  = RacePhase.LATE_JOIN;
        } else if(gameplayManager.KartsIRManager.HumanPlayerCount < gameplayManager.KartLobby.PlayerCount) {
            phase.Value  = RacePhase.WAITING_FOR_PLAYERS;
        } else {
            phase.Value  = RacePhase.COUNTDOWN;
            PrepareRace();
        }
    }

    private void OnEnable() { raceTime.OnChange += RaceTime_OnChange; }
    private void OnDisable() { raceTime.OnChange -= RaceTime_OnChange; }

    private void Update() 
    {
        raceTime.Update(Time.deltaTime);

        if(!base.IsServerInitialized)
            return;

        // We'll attempt to escalate the race phase.Value each Update()
        // Only allowed to escalate once per frame
        switch(phase.Value ) {
            case RacePhase.WAITING_FOR_LOBBY:
                if(gameplayManager.KartLobby != null)
                    InitializeWithLobby();
                break;
            case RacePhase.LATE_JOIN:
                break;
            case RacePhase.WAITING_FOR_PLAYERS:
                // TODO: Add a timer that kicks the player if they don't ready up by said time
                // bool introAnimComplete = !kartLevelManager.HasRaceCamera || !kartLevelManager.RaceCamera.Animating;
                // TODO: Add intro anim back in
                KartsIRManager playerManager = gameplayManager.KartsIRManager;
                bool allPlayersReady = playerManager.AllPlayersReady && playerManager.HumanPlayerCount == gameplayManager.KartLobby.PlayerCount;
                bool allBotsReady = gameplayManager.KartsIRManager.BotPlayerCount == gameplayManager.KartsIRManager.BotsToSpawn;
                bool needToSpawnBots = gameplayManager.RaceManager.Settings.Bots && playerManager.BotPlayerCount == 0 && playerManager.BotsToSpawn > 0;
                // Two tracks if we're spawning bots or not
                if(needToSpawnBots) {
                    if(allPlayersReady) {
                        playerManager.SpawnBots();
                    } else if(allBotsReady) {
                        phase.Value  = RacePhase.COUNTDOWN;
                    }
                } else {
                    if(allPlayersReady)
                        phase.Value  = RacePhase.COUNTDOWN;
                }
                break;
            case RacePhase.COUNTDOWN:
                break;
            case RacePhase.RACING:
                bool allHumanPlayersFinished = true;
                foreach(GameObject kartObj in gameplayManager.KartsIRManager.kartObjects) {
                    KartManager km = KartBehavior.LocateManager(kartObj);
                    if(!km.IsHuman)
                        continue;
                    if(km.GetPositionTracker().RaceCompletion < 1 || !placements.ContainsKey(km.OwnerUID)) {
                        allHumanPlayersFinished = false;
                        break;
                    }
                }
                if(allHumanPlayersFinished) {
                    FinalizePlacements(); // Populate placements here so that we can ensure the results are ready once clients need to show results.
                    phase.Value  = RacePhase.FINISHED;
                }
                break;
            case RacePhase.FINISHED:
                break;
        }

    }

    private void RacePhaseChange(RacePhase prev, RacePhase current, bool asServer) {

        // Getting double-calls from the syncvar, this just makes sure we block a double call in a host instance.
        if(base.IsHostInitialized && !asServer)
            return;

        BLog.Log($"Race phase.Value changed to {current}", gameplayManager.LogSettings);

        // Call phase.Value change event
        RacePhaseChanged?.Invoke(prev, current);

        PlayerInputManager pim = PlayerManager.Instance.PlayerInputManager;

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
                        phase.Value  = RacePhase.RACING;
                    else
                        raceTime.StartTimer(Settings.startDelay, true);
                        
                    placements.Clear();

                    PrepareRace();
                }
                break;
            case RacePhase.RACING:
                if(asServer)
                    raceTime.StartTimer(RACE_TIME, true);
                break;
            case RacePhase.FINISHED:
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
            if(phase.Value  == RacePhase.COUNTDOWN)
                phase.Value  = RacePhase.RACING;
            else if(phase.Value == RacePhase.RACING) {
                FinalizePlacements();
                phase.Value = RacePhase.FINISHED;
            }
        } //else if(op == SyncTimerOperation.Start)
            // simulatedTimer = next;
    }

    public override void OnStartClient() 
    {
        base.OnStartClient();
        RacePhaseChange(RacePhase.LATE_JOIN, phase.Value, false);
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
        if(!base.IsServerInitialized) {
            ServerRpcPassLateJoin();
            return;
        }
        if(phase.Value == RacePhase.LATE_JOIN)
            phase.Value = RacePhase.WAITING_FOR_PLAYERS;
    }

    /// <summary>
    /// Called when we enter the countdown phase.
    /// Prepares the client for the race, enabling splitscreen and player components
    /// </summary>
    [ObserversRpc]
    public void PrepareRace() 
    {
        BLog.Log("Preparing race", gameplayManager.LogSettings, 3);

        // Enable player cameras and splitscreen, ensure we're on Gameplay control map
        PlayerManager.Instance.LocalPlayers.ToList().ForEach(lp => {
            lp.Input.enabled = true;
            lp.Input.SwitchCurrentActionMap("Gameplay");
            if(lp.Input.camera != null) {
                lp.Input.camera.enabled = true;
                if(lp.Input.playerIndex == 0 && !CoreManager.IsServerOnly) 
                    lp.Input.camera.GetComponent<AudioListener>().enabled = true;
            }
        });

        PlayerManager.Instance.PlayerInputManager.splitScreen = true;

        // Disable main camera audio listener so we get player 0's camera audio
        CoreManager.Instance.GetComponent<AudioListener>().enabled = false;
        // kartLevelManager.RaceCamera.GetComponent<AudioListener>().enabled = false;
    }

    /// <summary>
    /// Notify the server that this player has completed the race
    /// </summary>
    public void CompletedRace(PlayerData data, float raceCompletion) 
    {
        if(IsClientInitialized) {
            ServerRpcCompletedRace(data, raceCompletion);
            return;
        }

        float raceFinishTime = RaceTimeElapsed;
        if(raceCompletion < 1)
            raceFinishTime = -1;

        RacePlacementData rpd = new() {
            raceFinishTime = raceFinishTime,
            raceCompletion = raceCompletion
        };

        // Add to placements
        if(!placements.ContainsKey(data.GetUID()))
            placements.Add(data.GetUID(), rpd);
    }
    /// <summary> Server RPC calling RaceManager#CompletedRace </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcCompletedRace(PlayerData data, float raceCompletion) { CompletedRace(data, raceCompletion); }

    [Server]
    public void FinalizePlacements() 
    {
        // Ensure everyone is in the placements array
        foreach(GameObject kartObject in gameplayManager.KartsIRManager.kartObjects) {
            KartManager kartManager = KartBehavior.LocateManager(kartObject);
            CompletedRace(PlayerDataRegistry.Instance.GetPlayerData(kartManager.OwnerUID), kartManager.GetPositionTracker().RaceCompletion);
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

    public bool CanMove { get { return (phase.Value == RacePhase.RACING || phase.Value == RacePhase.FINISHED) && RaceTime >= 0; } }

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