using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class AudioAtlas : MonoBehaviour
{
    private static string[] formattedEnums;
    public static string[] FormattedEnums { get {
        if(formattedEnums == null || formattedEnums.Length == 0) {
            AudioFile[] keyArray = AudioAtlas.AudioFile_TrueIndex.Keys.ToArray();
            formattedEnums = new string[keyArray.Length];
            for(int i = 0; i < keyArray.Length; i++) { formattedEnums[i] = keyArray[i].ToString(); } 
        }
        return formattedEnums;
    } }

    private static Dictionary<AudioFile, int> audioFile_TrueIndex;
    /// <summary> Cached values for the AudioFilePropertyDrawer </summary>
    public static Dictionary<AudioFile, int> AudioFile_TrueIndex { get {
        if(audioFile_TrueIndex == null) {
            audioFile_TrueIndex = new();
            for(int i = 0; i < Enum.GetValues(typeof(AudioFile)).Length; i++) {
                audioFile_TrueIndex.Add((AudioFile)i, i);
            }
            audioFile_TrueIndex = audioFile_TrueIndex.OrderBy(entry => entry.Key.ToString()).ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        return audioFile_TrueIndex;
    } }

    private static Dictionary<AudioFile, int> audioFile_SortedIndex;
    public static Dictionary<AudioFile, int> AudioFile_SortedIndex { get {
        if(audioFile_SortedIndex == null) {
            audioFile_SortedIndex = new();
            AudioFile[] sortedAudioFiles = AudioFile_TrueIndex.Keys.ToArray();
            for(int i = 0; i < sortedAudioFiles.Length; i++) {
                audioFile_SortedIndex.Add(sortedAudioFiles[i], i);
            }
        }
        return audioFile_SortedIndex;
    } }

    private Dictionary<AudioFile, AudioMixerGroup> audioMixerGroups;
    public Dictionary<AudioFile, AudioMixerGroup> AudioMixerGroups { get {
        if(audioMixerGroups == null) {
            audioMixerGroups = new();
            foreach(AudioFile file in Enum.GetValues(typeof(AudioFile))) {
                if(file == AudioFile.NONE || file == AudioFile.PLACEHOLDER)
                    continue;
                    
                string group = file.ToString().Split('_')[0];
                AudioMixerGroup correspondingAMG = null;
                foreach(PrefixAudioMixerGroup pamg in prefixAudioMixerGroups) {
                    if(pamg.group == group) {
                        correspondingAMG = pamg.audioMixerGroup;
                        break;
                    }
                }
                if(correspondingAMG == null) {
                    Debug.LogWarning($"AudioAtlas failed to match group prefix \"{group}\" to a corresponding AudioMixerGroup. Check that the AudioAtlases prefixAudioMixerGroups field has a group string \"{group}\".");
                    continue;
                }
                audioMixerGroups.Add(file, correspondingAMG);
            }
        }
        return audioMixerGroups;
    } }

    /// <summary>
    /// Using the audio files prefix (i.e. UI_PRESS group is UI), we will attach that files group to
    ///   a corresponding AudioMixerGroup.
    /// </summary>
    // public List<PrefixAudioMixerGroup> prefixAudioMixerGroups;
    public List<PrefixAudioMixerGroup> prefixAudioMixerGroups;

    [Header("NOTE: AudioClips are sorted based off of the AudioFile enum")]
    public AudioClip[] clips;
}

public enum AudioFile {
    /* 
       DEV NOTE: Do not insert new enums into the middle of this list, only append to the end. Inspector fields will be
         messed up if you insert new values.
       For organization, use the following categores:
         - UI         |
         - KART  SoundFX group
         - FX         |

         - ENV     Env group
         
         - MUSIC  Music group
    */
    NONE,
    UI_PRESS, UI_INTERACT, UI_NAV_BACK,
    KART_IDLE, KART_THROTTLE, KART_DRIFT,
    FX_COUNTDOWN, FX_COUNTDOWN_START,
    ENV_MENU_AMBIANCE, ENV_TEST_TRACK_AMBIANCE, ENV_ATUIN_SHIPYARD_AMBIANCE,
    MUSIC_TEST_TRACK, MUSIC_ATUIN_SHIPYARD,
    FX_ITEM_ZAP_1, FX_ITEM_ZAP_2, FX_ITEM_ZAP_3, FX_ITEM_GLASS_1, FX_ITEM_GLASS_2, FX_ITEM_GLASS_3, FX_ITEM_LIGHTNING_BOLT, PLACEHOLDER
}

[Serializable]
public struct PrefixAudioMixerGroup {
    public string group;
    public AudioMixerGroup audioMixerGroup;
}