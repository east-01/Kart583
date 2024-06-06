using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes checks to ensure that things are in working order. For now it does:
///  - Ensures the AudioManager is enabled once player leaves game
///  - Plays menu ambiance sounds
/// </summary>
public class PersistentAudioWatchdog : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioListener audioListener;
    private static bool initialized = false;

    // Initialization call
    private void Awake() 
    {
        audioListener = GetComponentInParent<AudioListener>();
        if(!initialized) {
            initialized = true;
            UnitySceneManager_SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); 
        }
    }

    private void Update() 
    {
        AudioListener.volume = CoreManager.IsServerOnly ? 0 : 1;
    }

    private void OnEnable() { SceneManager.sceneLoaded += UnitySceneManager_SceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= UnitySceneManager_SceneLoaded; }

    private void UnitySceneManager_SceneLoaded(Scene scene, LoadSceneMode loadMode) 
    {
        if(SceneNames.IsMenuScene(scene.name) && !audioListener.enabled && !CoreManager.IsServerOnly)
            audioListener.enabled = true;

        if(audioSource == null && SceneNames.IsMenuScene(scene.name)) {
            audioSource = CoreManager.AudioManager.PlaySound(AudioFile.ENV_MENU_AMBIANCE, 1f, true);
        } else if(audioSource != null && !SceneNames.IsMenuScene(scene.name)) {
            audioSource.Stop();
            audioSource = null;
        }
    }
}
