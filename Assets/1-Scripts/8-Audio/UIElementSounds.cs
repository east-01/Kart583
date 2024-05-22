using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementSounds : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonSelectClip;
    [SerializeField] private AudioClip backSoundClip;

    public void PlayButtonSound() { CoreManager.AudioManager.PlaySound(buttonClickClip, 1f); }
    public void PlaySelectSound() { CoreManager.AudioManager.PlaySound(buttonSelectClip, 1f); }
    public void PlayBackSound() { CoreManager.AudioManager.PlaySound(backSoundClip, 1f); }
}
