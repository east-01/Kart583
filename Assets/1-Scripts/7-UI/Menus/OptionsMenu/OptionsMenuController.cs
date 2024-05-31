using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenuController : MenuController
{
    
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private List<ChannelData> channelDatas;

#region Save/Load
    public void SaveOptions() 
    {
        // Set mixer data
        for(int i = 0; i < channelDatas.Count; i++) {
            PlayerPrefs.SetFloat(channelDatas[i].label, GetVolume(i));
        }

        PlayerPrefs.Save();
    }

    public void LoadOptions() 
    {
        // Load mixer data
        for(int i = 0; i < channelDatas.Count; i++) {
            ChannelData cd = channelDatas[i];
            float volume = PlayerPrefs.GetFloat(cd.label, 1);
            SetVolume(i, volume, true);
        }
    }
#endregion

#region Mixer
    public void SetVolumeMaster(float volume) { SetVolume(0, volume); }
    public void SetVolumeMusic(float volume) { SetVolume(1, volume); }
    public void SetVolumeEnvironment(float volume) { SetVolume(2, volume); }
    public void SetVolumeSoundFX(float volume) { SetVolume(3, volume); }

    public void SetVolume(int channel, float volume, bool setSliderValue = false) 
    {
        if(channel < 0 || channel >= channelDatas.Count) {
            Debug.LogError($"Can't set channel volume, provided channel is out of bounds {channel} isn't in bounds [0, {channelDatas.Count})");
            return;
        }
        ChannelData cd = channelDatas[channel];
        mixer.SetFloat(cd.label, Mathf.Log10(volume)*20f);

        if(setSliderValue)
            cd.slider.value = volume;
    }

    public float GetVolume(int channel) 
    {
        if(channel < 0 || channel >= channelDatas.Count) {
            Debug.LogError($"Can't get channel volume, provided channel is out of bounds {channel} isn't in bounds [0, {channelDatas.Count})");
            return 0;
        }
        mixer.GetFloat(channelDatas[channel].label, out float vol);
        // Invert function from setting the mixer float
        vol = Mathf.Pow(10f, vol / 20f);
        return vol;
    }
#endregion

}

[Serializable]
public struct ChannelData {
    /// <summary>
    /// The label for the mixer channel, must correspond with an exposed variable in the mixer
    /// </summary>
    public string label;
    public Slider slider;
}