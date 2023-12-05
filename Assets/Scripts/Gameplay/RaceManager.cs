using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(PlayerManager))]
public class RaceManager : MonoBehaviour
{

    /* ----- Settings fields ---- */
    public bool running = true;

    /* ----- Runtime fields ----- */
    public RaceSettings settings;
    private PlayerManager pm;
    private float raceTime;
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
        settingsNullable ??= DefaultSettings;
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

        if(pm.PlayerCount < 8 && settings.bots && raceTime > -3) {
            for(int i = 0; i < 8-pm.PlayerCount; i++) {
                pm.SpawnBot();
            }
        }
        
        // Reset ensureRCCheck timer
        ensureRCCheck = raceTime < 0 ? 1 : 5;
    }

    public RaceSettings DefaultSettings 
    {
        get {
            return new RaceSettings(3, 10, false);
        }
    }

}

public struct RaceSettings 
{
    public int laps;
    public float startDelay;
    public bool bots;
    public RaceSettings(int laps, float startDelay, bool bots) {
        this.laps = laps;
        this.startDelay = startDelay;
        this.bots = bots;
    }
}

public enum RaceType 
{

}