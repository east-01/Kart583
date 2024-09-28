using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class KartAudioManager : KartBehavior
{
    private AudioSource engineNoiseSource;
    private AudioSource driftNoiseSource;
    private AudioSource boostNoiseSource;

    [SerializeField] private float kartAudioVolume = 0.45f;

    private readonly int IDLE_RPM = 1000;
    private readonly int MAX_RPM = 5000;
    /// <summary>
    /// The simulated rpm of the kart, when 0 the engine is at idle rpm, ranges from 0-5000
    /// </summary>
    [SerializeField] private int simulatedRPM;

    [SerializeField] private AudioMixerGroup kartAudioMixerGroup;

    [SerializeField] private AudioClip idle;
    [SerializeField] private AudioClip lowRPM;
    [SerializeField] private AudioClip highRPM;

    [SerializeField] private AudioClip drift;

    [SerializeField] private AudioClip boostStart;
    [SerializeField] private AudioClip boostLoop;
    /// <summary>
    /// The boost audio we're currently running.
    /// 0 = off
    /// 1 = start
    /// 2 = looping
    /// </summary>
    private int boostAudioLevel = 0;

    protected new void Awake() 
    {
        base.Awake();

        engineNoiseSource = kartManager.AddComponent<AudioSource>();
        engineNoiseSource.outputAudioMixerGroup = kartAudioMixerGroup;
        engineNoiseSource.volume = kartAudioVolume;
        engineNoiseSource.loop = true;
        AudioManager.ProcessAudioSource(engineNoiseSource, true);

        driftNoiseSource = kartManager.AddComponent<AudioSource>();
        driftNoiseSource.outputAudioMixerGroup = kartAudioMixerGroup;
        driftNoiseSource.volume = kartAudioVolume;
        driftNoiseSource.loop = true;
        driftNoiseSource.clip = drift;
        AudioManager.ProcessAudioSource(driftNoiseSource, true);

        boostNoiseSource = kartManager.AddComponent<AudioSource>();
        boostNoiseSource.outputAudioMixerGroup = kartAudioMixerGroup;
        boostNoiseSource.volume = kartAudioVolume;
        AudioManager.ProcessAudioSource(boostNoiseSource, true);
    }

    public void Update() 
    {        
        /* Simulate engine RPM */
        if(Mathf.Abs(kartCtrl.ThrottleInput) > 0.01) {
            simulatedRPM += (int)(kartCtrl.ThrottleInput*1500f*Time.deltaTime);
        } else if(simulatedRPM > 0) {
            simulatedRPM -= (int)(2000*Time.deltaTime);
        }

        simulatedRPM = Mathf.Clamp(Mathf.Abs(simulatedRPM), 0, MAX_RPM);

        /* Engine noise mgmt */
        if(kartCtrl.ActivelyBoosting && EngineNoise != highRPM) {
            // Overdrive audio (high_rpm)
            engineNoiseSource.clip = highRPM;
            engineNoiseSource.Play();
        } else if(EngineNoise == null || (simulatedRPM <= IDLE_RPM && EngineNoise != idle && !kartCtrl.ActivelyBoosting)) {
            // Idle audio
            engineNoiseSource.clip = idle;
            engineNoiseSource.Play();    
        } else if(simulatedRPM > IDLE_RPM && simulatedRPM <= MAX_RPM && EngineNoise != lowRPM && !kartCtrl.ActivelyBoosting) {
            // Standard audio (low_rpm)
            engineNoiseSource.clip = lowRPM;
            engineNoiseSource.Play();    
        }

        if(kartCtrl.ActivelyBoosting) 
            engineNoiseSource.pitch = 1;
        else if(simulatedRPM <= IDLE_RPM)
            engineNoiseSource.pitch = Mathf.Lerp(1, 1.8f, simulatedRPM/(float)MAX_RPM);
        else if(simulatedRPM > IDLE_RPM && simulatedRPM <= MAX_RPM)
            engineNoiseSource.pitch = Mathf.Lerp(1, 1.65f, (simulatedRPM-IDLE_RPM)/(float)(MAX_RPM-IDLE_RPM));
    
        /* Drift noise mgmt */
        if((kartCtrl.IsDriftEngaged || kartCtrl.EngineStallTime > 0 ) && !driftNoiseSource.isPlaying)
            driftNoiseSource.Play();
        else if(!kartCtrl.IsDriftEngaged && kartCtrl.EngineStallTime < 0.01f && driftNoiseSource.isPlaying)
            driftNoiseSource.Stop();

        if(kartCtrl.EngineStallTime > 0)
            driftNoiseSource.volume = 1f;
        else
            driftNoiseSource.volume = kartAudioVolume;

        /* Boost noise mgmt */
        if(!kartCtrl.ActivelyBoosting) {
            boostNoiseSource.Stop();
            boostAudioLevel = 0;
        } else if(kartCtrl.ActivelyBoosting && boostAudioLevel == 0) {
            boostNoiseSource.clip = boostStart;
            boostNoiseSource.loop = false;
            boostNoiseSource.Play();
            boostAudioLevel = 1;
        } else if(kartCtrl.ActivelyBoosting && boostAudioLevel == 1 && !boostNoiseSource.isPlaying) {
            boostNoiseSource.clip = boostLoop;
            boostNoiseSource.loop = true;
            boostNoiseSource.Play();
            boostAudioLevel = 2;
        }

    }

    public AudioClip EngineNoise { get { return engineNoiseSource.clip; } }
}