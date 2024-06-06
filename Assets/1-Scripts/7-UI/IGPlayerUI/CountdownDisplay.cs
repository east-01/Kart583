using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CountdownDisplay : MonoBehaviour, GameplayManagerBehavior
{
    /* ----- Settings variables ---- */
    [Header("General animation")] public float regularSize;
    public Color regularColor;
    public float regularHeight;
    public int finalCountdownSeconds = 2;
    public float finalCountdownSize;
    public Color finalCountdownColor;
    public float finalCountdownHeight;

    
    [Header("Single Second animation")] public AnimationCurve height;
    public float delay = 0.2f;

    /* ----- Runtime variables ----- */
    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    private RectTransform rt;
    [Header("Runtime fields")] public int displayedSecond;
    public int raceFloor;
    public float secondProgress;

    public float goDisplayTime = 0;

    void Awake() 
    {
        rt = GetComponent<RectTransform>();

        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);

        displayedSecond = 100;
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;

        gameplayManager.RaceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;
    }

    void Update()
    {
        if(gameplayManager == null)
            return;

        RaceManager rm = gameplayManager.RaceManager;

        if(rm.Phase == RacePhase.COUNTDOWN && rm.RaceTime >= 0) {
            // -1.2f --> |_-1.2_| == -2 --> |-2 - -1.2| --> 0.8 correct, -1.2 does represent 80% progress through 0.8
            raceFloor = (int)Math.Floor(rm.RaceTime);
            secondProgress = Math.Abs(raceFloor-rm.RaceTime);

            if(secondProgress < delay) return;

            if(raceFloor != displayedSecond) {
                int currSecond = displayedSecond-1;
                int prevSecond = raceFloor-1;
                if(currSecond != prevSecond) {
                    if(currSecond >= 0 && currSecond < 3) {
                        CoreManager.AudioManager.PlaySound(AudioFile.FX_COUNTDOWN, 1f);
                    }
                }
            }

            displayedSecond = raceFloor;

            Vector3 pos = rt.anchoredPosition;
            pos.y = (displayedSecond <= finalCountdownSeconds ? finalCountdownHeight : regularHeight) + height.Evaluate(secondProgress);
            rt.anchoredPosition = pos;

            TMP_Text text = GetComponent<TMP_Text>();
            text.fontSize = displayedSecond <= finalCountdownSeconds ? finalCountdownSize : regularSize;
            text.color = displayedSecond <= finalCountdownSeconds ? finalCountdownColor : regularColor;
            text.text = (displayedSecond+1).ToString();

        } else if(rm.Phase == RacePhase.RACING && goDisplayTime > 0) {
            displayedSecond = 0;
            goDisplayTime -= Time.deltaTime;
            GetComponent<TMP_Text>().text = "GO";
        } else {
            displayedSecond = 0;
            GetComponent<TMP_Text>().text = "";
        }
    }

    private void RaceManager_RacePhaseChanged(RacePhase previousPhase, RacePhase currentPhase)
    {
        if(currentPhase == RacePhase.RACING) {
            CoreManager.AudioManager.PlaySound(AudioFile.FX_COUNTDOWN_START, 1f);
            goDisplayTime = 1f;
        }
    }
}
