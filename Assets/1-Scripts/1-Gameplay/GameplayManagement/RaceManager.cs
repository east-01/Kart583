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

    /* ----- Settings fields ---- */
    public RaceSettings settings;

    /* ----- Runtime fields ----- */
    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    [Header("Runtime Fields"), SerializeField, SyncVar(OnChange = nameof(RacePhaseChange))] 
    private RacePhase phase; 
    public delegate void RacePhaseChangeHandler(RacePhase previousPhase, RacePhase currentPhase);
    public event RacePhaseChangeHandler RacePhaseChanged;

    [SerializeField] 
    private float raceTime;
    private bool waitingForPlayerInput = false;

    /// <summary>
    /// Stores raceFinishTime first in RaceCompleted(), then gets position and point data in PopulatePlacements()
    /// </summary>
    [SyncObject] 
    private readonly SyncDictionary<string, RacePlacementData> placements = new();

    private void Awake()
    {
        gameplayManager = GetComponent<GameplayManager>();
        kartLevelManager = gameplayManager.KartLevelManager;

        raceTime = -100;

        if(!base.IsServer)
            return;

        // Initialize phases
        if(PlayerObjectManager.Instance == null) {
            waitingForPlayerInput = true; // TODO: This is really dumb: we should only be waiting for player input on clients
            Debug.LogWarning("This is really dumb: we should only be waiting for player input on clients");
        } else if(gameplayManager.GameLobby.PlayerCount == 0) {
            phase = RacePhase.LATE_JOIN;
        } else if(kartLevelManager.HasRaceCamera) {
            phase = RacePhase.WAITING_FOR_PLAYERS;
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

        if(!waitingForPlayerInput && phase != RacePhase.LATE_JOIN && phase != RacePhase.WAITING_FOR_PLAYERS)
            raceTime += Time.deltaTime;

        if(!base.IsServer)
            return;

        // We'll attempt to escalate the race phase each Update()
        // Only allowed to escalate once per frame
        switch(phase) {
            case RacePhase.LATE_JOIN:
                break;
            case RacePhase.WAITING_FOR_PLAYERS:
                // TODO: Add a timer that kicks the player if they don't ready up by said time
                // bool introAnimComplete = !kartLevelManager.HasRaceCamera || !kartLevelManager.RaceCamera.Animating;
                // TODO: Add intro anim back in
                if(gameplayManager.PlayerManager.AllPlayersReady)
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

        // If this is the case, waitingForPlayerObjectManager will be true and phase will be changed again
        if(waitingForPlayerInput)
            return;

        // Call phase change event
        RacePhaseChanged?.Invoke(prev, current);

        PlayerInputManager pim = PlayerObjectManager.Instance.GetPlayerInputManager();

        switch(current) {
            case RacePhase.LATE_JOIN:
                if(!asServer)
                    pim.EnableJoining();
                break;
            case RacePhase.WAITING_FOR_PLAYERS:
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
            phase = RacePhase.WAITING_FOR_PLAYERS;
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

        // Data management
        if(base.IsServer)
            placements.Clear();

        // Load settings values
        raceTime = -Math.Abs(settings.startDelay);
    }

    /// <summary>
    /// Server RPC calling RaceManager#CompletedRace
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcCompletedRace(PlayerData data, float raceCompletion) 
    {
        CompletedRace(data, raceCompletion);
    }

    /// <summary>
    /// Notify the server that this player has completed the race
    /// </summary>
    [Server]
    public void CompletedRace(PlayerData data, float raceCompletion) 
    {
        if(placements.ContainsKey(data.uuid))
            return;

        float raceFinishTime = raceTime;
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

    public float RaceTime { get { return raceTime; }}
    public bool CanMove { get { return raceTime >= 0; } }

    public SyncDictionary<string, RacePlacementData> GetPlacements() { return placements; }

}

[Serializable]
public struct RaceSettings 
{
    public int laps;
    public float startDelay;
    public float startBoostPercent;
    public bool bots;
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
    LATE_JOIN, WAITING_FOR_PLAYERS, COUNTDOWN, RACING, FINISHED
}