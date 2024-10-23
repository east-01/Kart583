using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These audio files are played on the CoreManager for now
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    [SerializeField] private AudioFile ambianceFile;
    [SerializeField] private AudioFile musicFile;

    private GameplayManager gameplayManager;
    private RaceManager raceManager;

    private AudioSource ambianceSource;
    private AudioSource musicSource;

    private void OnEnable() 
    {
        gameplayManager = GetComponent<GameplayManager>();

        if(ambianceFile != AudioFile.NONE && ambianceSource == null) {
            ambianceSource = AudioManagerMaster.Instance.PlaySound(ambianceFile, 0.2f, true);
        }
    }
    
    private void OnDisable() 
    {
        if(ambianceSource != null) {
            ambianceSource.Stop();
            ambianceSource = null;
        }

        if(musicSource != null) {
            musicSource.Stop();
            musicSource = null;
        }
    }

    private void Update()  
    {
        if(raceManager == null && gameplayManager.RaceManager != null) {
            raceManager = gameplayManager.RaceManager;
            raceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;
        } else if(raceManager != null && raceManager != gameplayManager.RaceManager) {
            raceManager.RacePhaseChanged -= RaceManager_RacePhaseChanged;
            raceManager = null;
        }
    }

    private void RaceManager_RacePhaseChanged(RacePhase previousPhase, RacePhase currentPhase)
    {
        if(currentPhase == RacePhase.RACING && musicFile != AudioFile.NONE) {
            if(ambianceSource != null)
                ambianceSource.volume /= 2;
            musicSource = AudioManagerMaster.Instance.PlaySound(musicFile, 1f, true);
        }
    }

}
