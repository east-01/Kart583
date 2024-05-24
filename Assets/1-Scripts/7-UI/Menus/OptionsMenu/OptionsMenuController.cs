using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class OptionsMenuController : MenuController
{
    
    [SerializeField] private AudioMixer mixer;

    public void SetVolumeMaster(float volume) { mixer.SetFloat("MasterVolume", Mathf.Log10(volume)*20f); }
    public void SetVolumeMusic(float volume) { mixer.SetFloat("MusicVolume", Mathf.Log10(volume)*20f); }
    public void SetVolumeEnvironment(float volume) { mixer.SetFloat("EnvironmentVolume", Mathf.Log10(volume)*20f); }
    public void SetVolumeSoundFX(float volume) { mixer.SetFloat("SoundFXVolume", Mathf.Log10(volume)*20f); }

}
