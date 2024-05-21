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
    /// <summary>
    /// Destroy the game object the AudioManager is on one it finishes playing sound.
    /// </summary>
    [Header("Settings"), SerializeField] private bool isOneShot;

    [Space] private GameObject oneShotAudioManagerPrefab;

    private AudioSource localSource;
    /// <summary>
    /// If set, this AudioManager will always follow the position of the target
    /// </summary>
    public Transform trackingTarget;

    private void Awake() 
    {
        localSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Plays a sound on this object.
    /// </summary>
    public void PlaySound(AudioClip audioClip, float volume) 
    {
        localSource.clip = audioClip;
        localSource.volume = volume;
        localSource.Play();

        if(isOneShot) {
            float clipLength = localSource.clip.length;
            Destroy(gameObject, clipLength);
        }
    }

    /// <summary>
    /// Play a one shot sound effect, supply a Transform for position and then enable followTarget if you want
    ///   the one shot audio manager to follow its target positiom.
    /// </summary>
    public void PlayOneShotSound(AudioClip audioClip, float volume, Transform target, bool followTarget = false) 
    {
        AudioManager am = Instantiate(oneShotAudioManagerPrefab, target.position, Quaternion.identity).GetComponent<AudioManager>();
        am.PlaySound(audioClip, volume);
        if(followTarget) {
            am.trackingTarget = target;
        }
    }

    /// <summary>
    /// Behaves exactly the same as PlaySound with the exception that it picks a random audio clip to play.
    /// </summary>
    public void PlayRandomSound(AudioClip[] audioClips, float volume) 
    {
        if(audioClips.Length == 0) {
            Debug.LogError("Can't play random sound, audioClips length is 0.");
            return;
        }
        PlaySound(audioClips[UnityEngine.Random.Range(0, audioClips.Length)], volume);
    }

    /// <summary>
    /// Behaves exactly the same as PlayOneShotSound with the exception that it picks a random audio clip to play.
    /// </summary>
    public void PlayRandomOneShotSound(AudioClip[] audioClips, float volume, Transform target, bool followTarget = false) 
    {
        if(audioClips.Length == 0) {
            Debug.LogError("Can't play random one shot sound, audioClips length is 0.");
            return;
        }
        PlayOneShotSound(audioClips[UnityEngine.Random.Range(0, audioClips.Length)], volume, target, followTarget);
    }

}
