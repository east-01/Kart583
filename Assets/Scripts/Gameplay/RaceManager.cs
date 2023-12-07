using System;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class RaceManager : MonoBehaviour
{

    /* ----- Settings fields ---- */
    public bool running = true;

    public RaceSettings defaultSettings;

    /* ----- Runtime fields ----- */
    public RaceSettings settings;
    private PlayerManager pm;
    public float raceTime;
    private float ensureRCCheck;

    private void Start()
    {
        running = false;

        pm = GetComponent<PlayerManager>();

        // TODO: Load settings from settings menu
        Activate(null);
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

        int botsInGame = pm.playerObjects.Count - pm.playerInputs.Count;
        if(pm.PlayerCount < 8 && settings.bots && raceTime > -3) {
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