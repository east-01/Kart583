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

    private AudioSource ambianceSource;
    private AudioSource musicSource;

    private void OnEnable() 
    {
        gameplayManager = GetComponent<GameplayManager>();

        gameplayManager.RaceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;

        if(ambianceFile != AudioFile.NONE && ambianceSource == null) {
            ambianceSource = AudioManagerMaster.Instance.PlaySound(ambianceFile, 0.2f, true);
        }
    }
    
    private void OnDisable() 
    {
        gameplayManager.RaceManager.RacePhaseChanged -= RaceManager_RacePhaseChanged;

        if(ambianceSource != null) {
            ambianceSource.Stop();
            ambianceSource = null;
        }

        if(musicSource != null) {
            musicSource.Stop();
            musicSource = null;
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
