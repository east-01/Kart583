using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuAmbiance : MonoBehaviour
{
    private AudioSource audioSource;
    void Update()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        if(audioSource == null && SceneNames.IsMenuScene(currentSceneName)) {
            audioSource = CoreManager.AudioManager.PlaySound(AudioFile.ENV_MENU_AMBIANCE, 1f, true);
        } else if(audioSource != null && !SceneNames.IsMenuScene(currentSceneName)) {
            audioSource.Stop();
            audioSource = null;
        }
    }
}
