using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerManager))]
public class RaceManager : MonoBehaviour
{

    /* ----- Settings fields ---- */
    public bool running = true;

    public RaceSettings defaultSettings;

    /* ----- Runtime fields ----- */
    public RaceSettings settings;
    public float raceTime;
    private float ensureRCCheck;

    private void Start()
    {
        running = false;
        raceTime = -100;
    }

    public void Activate(RaceSettings? settingsNullable) 
    {
        // Load settings
        settingsNullable ??= defaultSettings;
        settings = settingsNullable.Value;

        // Load settings values
        raceTime = -Math.Abs(settings.startDelay);

        // Get everything going
        running = true;
        ensureRCCheck = 0.01f;
    }

    private void Update() 
    {
        PlayerInputManager pim = GameplayManager.PlayerInputManager;
        if(CanPlayersJoin && !pim.joiningEnabled) {
            GameplayManager.PlayerInputManager.EnableJoining();
        } else if(!CanPlayersJoin && pim.joiningEnabled) {
            GameplayManager.PlayerInputManager.DisableJoining();
        }

        if(!running) return;

        raceTime += Time.deltaTime;

        if(ensureRCCheck > 0) {
            ensureRCCheck -= Time.deltaTime;
            if(ensureRCCheck <= 0) EnsureRaceConditions();
        }
    }

    /** Check on everything in the race and if anything's wrong fix it or exit. */
    public void EnsureRaceConditions() 
    {  

        PlayerManager pm = GameplayManager.PlayerManager;
        int botsInGame = pm.playerObjects.Count - pm.playerInputs.Count;
        if(pm.PlayerCount < 8 && settings.bots && raceTime > -3f) {
            for(int i = 0; i < 8-pm.PlayerCount && i < settings.botLimit-botsInGame; i++) {
                pm.SpawnBot();
            }
        }
        
        // String positions = "";
        // pm.playerPositions.ForEach(pt => positions += ((int)Math.Round(pt.raceCompletion*10000)/10000f) + ", ");
        // if(positions.Length > 2) print(positions.Substring(0, positions.Length-2));

        // Reset ensureRCCheck timer
        ensureRCCheck = raceTime < 0 ? 1 : 5;
    }

    public bool CanMove { get { return raceTime >= 0; } }
    public bool CanPlayersJoin { get { 
        if(GameplayManager.HasRaceCamera && GameplayManager.RaceCamera.Animating) return false;
        return raceTime <= -3f; 
    } }

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