using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// There will be 4 kinds of AudioManagers:
/// 1. Persistent AudioManager on the CoreManager, used for:
///   - Menu music/sounds
///   - UI sounds
/// 2. Persistent AudioManager on the GameplayManager, used for:
///   - Ingame music/sounds
///   - Environment sounds
/// 3. Persistent AudioManager on the Kart, used for kart sounds
/// 4. Non-persistent AudioManager on prefab, used for playing one-off
///    sounds
/// </summary>
public class AudioManager : MonoBehaviour
{

    [SerializeField] private GameObject oneShotAudioManagerPrefab;

    /// <summary>
    /// Destroy the game object the AudioManager is on one it finishes playing sound.
    /// NOTE: This field is set in the inspector on the OneShotAudioManagerPrefab
    /// </summary>
    [Header("Settings"), SerializeField] private bool isOneShot;

    private List<AudioSource> sources;
    /// <summary>
    /// If set, this AudioManager will always follow the position of the target
    /// </summary>
    [HideInInspector] public Transform trackingTarget;

    private void Awake() 
    {
        sources = new(GetComponents<AudioSource>());
    }

    private void Update() 
    {
        if(trackingTarget != null) 
            transform.position = trackingTarget.position;
        
        if(isOneShot && sources.Count > 0) {
            AudioSource oneShotSource = sources[0];
            if(!oneShotSource.loop && !oneShotSource.isPlaying)
                Destroy(gameObject);
        }
    }

#region Sound Playing
    /// <summary>
    /// Plays a sound on this object.
    /// </summary>
    public AudioSource PlaySound(AudioFile audioFile, float volume, bool loop = false) 
    {
        AudioSource src = GetSource();
        if(src == null) {
            Debug.LogError($"Can't play sound, failed to get AudioSource on object \"{name}\"");
            return null;
        }

        AudioClipPackage acp = CoreManager.AudioClipPackage(audioFile);
        src.loop = loop;
        src.volume = volume;
        src.clip = acp.audioClip;
        src.spatialBlend = acp.spatialBlend;
        if(CoreManager.AudioAtlas.AudioMixerGroups.ContainsKey(audioFile))
            src.outputAudioMixerGroup = CoreManager.AudioAtlas.AudioMixerGroups[audioFile];
 
        src.Play();

        if(isOneShot && !loop) {
            float clipLength = src.clip.length;
            Destroy(gameObject, clipLength);
        }

        return src;
    }

    /// <summary>
    /// Play a one shot sound effect, supply a Transform for position and then enable followTarget if you want
    ///   the one shot audio manager to follow its target positiom.
    /// </summary>
    public AudioSource PlayOneShotSound(AudioFile audioFile, float volume, Transform target, bool followTarget = false, bool loop = false) 
    {
        AudioManager am = Instantiate(oneShotAudioManagerPrefab, target.position, Quaternion.identity).GetComponent<AudioManager>();
        if(followTarget) {
            am.trackingTarget = target;
        }

        return am.PlaySound(audioFile, volume, loop);
    }

    /// <summary>
    /// Behaves exactly the same as PlaySound with the exception that it picks a random audio clip to play.
    /// </summary>
    public AudioSource PlayRandomSound(AudioFile[] audioFiles, float volume, bool loop = false) 
    {
        if(audioFiles.Length == 0) {
            Debug.LogError("Can't play random sound, audioFiles length is 0.");
            return null;
        }
        return PlaySound(audioFiles[UnityEngine.Random.Range(0, audioFiles.Length)], volume, loop);
    }

    /// <summary>
    /// Behaves exactly the same as PlayOneShotSound with the exception that it picks a random audio clip to play.
    /// </summary>
    public AudioSource PlayRandomOneShotSound(AudioFile[] audioFiles, float volume, Transform target, bool followTarget = false, bool loop = false) 
    {
        if(audioFiles.Length == 0) {
            Debug.LogError("Can't play random one shot sound, audioFiles length is 0.");
            return null;
        }
        return PlayOneShotSound(audioFiles[UnityEngine.Random.Range(0, audioFiles.Length)], volume, target, followTarget, loop);
    }
#endregion

#region Source Management
    public AudioSource GetSource() 
    {
        if(sources == null)
            sources = new();

        // Find an already existing one that isn't playing
        foreach(AudioSource src in sources) {
            if(!src.isPlaying)
                return src;
        }

        // Create new source
        AudioSource newSrc = gameObject.AddComponent<AudioSource>();
        ProcessAudioSource(newSrc);

        sources.Add(newSrc);
        return newSrc;
    }
#endregion

    public static void ProcessAudioSource(AudioSource src, bool isKart = false) 
    {
        src.rolloffMode = isKart ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
        src.dopplerLevel = 0.2f;
        src.minDistance = 1f;
        src.maxDistance = isKart ? 1f : 100f;
    }

}
