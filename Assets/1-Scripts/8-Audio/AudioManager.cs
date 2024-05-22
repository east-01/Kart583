using System.Collections;
using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;

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

#region Sound Playing
    /// <summary>
    /// Plays a sound on this object.
    /// </summary>
    public void PlaySound(AudioClip audioClip, float volume, bool loop = false) 
    {
        AudioSource src = GetSource();
        if(src == null) {
            Debug.LogError($"Can't play sound, failed to get AudioSource on object \"{name}\"");
            return;
        }
        src.loop = loop;
        src.clip = audioClip;
        src.volume = volume;
        src.Play();

        if(isOneShot) {
            float clipLength = src.clip.length;
            Destroy(gameObject, clipLength);
        }
    }

    /// <summary>
    /// Play a one shot sound effect, supply a Transform for position and then enable followTarget if you want
    ///   the one shot audio manager to follow its target positiom.
    /// </summary>
    public void PlayOneShotSound(AudioClip audioClip, float volume, Transform target, bool followTarget = false, bool loop = false) 
    {
        AudioManager am = Instantiate(oneShotAudioManagerPrefab, target.position, Quaternion.identity).GetComponent<AudioManager>();
        am.PlaySound(audioClip, volume, loop);
        if(followTarget) {
            am.trackingTarget = target;
        }
    }

    /// <summary>
    /// Behaves exactly the same as PlaySound with the exception that it picks a random audio clip to play.
    /// </summary>
    public void PlayRandomSound(AudioClip[] audioClips, float volume, bool loop = false) 
    {
        if(audioClips.Length == 0) {
            Debug.LogError("Can't play random sound, audioClips length is 0.");
            return;
        }
        PlaySound(audioClips[UnityEngine.Random.Range(0, audioClips.Length)], volume, loop);
    }

    /// <summary>
    /// Behaves exactly the same as PlayOneShotSound with the exception that it picks a random audio clip to play.
    /// </summary>
    public void PlayRandomOneShotSound(AudioClip[] audioClips, float volume, Transform target, bool followTarget = false, bool loop = false) 
    {
        if(audioClips.Length == 0) {
            Debug.LogError("Can't play random one shot sound, audioClips length is 0.");
            return;
        }
        PlayOneShotSound(audioClips[UnityEngine.Random.Range(0, audioClips.Length)], volume, target, followTarget, loop);
    }
#endregion

#region Source Management
    public AudioSource GetSource() 
    {
        // Find an already existing one that isn't playing
        foreach(AudioSource src in sources) {
            if(!src.isPlaying)
                return src;
        }

        // Create new source
        AudioSource newSrc = gameObject.AddComponent<AudioSource>();
        sources.Add(newSrc);
        return newSrc;
    }
#endregion

}
