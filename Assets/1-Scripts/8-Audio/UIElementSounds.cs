using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElementSounds : MonoBehaviour
{
    public void PlayButtonSound() { CoreManager.AudioManager.PlaySound(AudioFile.UI_PRESS, 1f); }
    public void PlaySelectSound() { CoreManager.AudioManager.PlaySound(AudioFile.UI_INTERACT, 0.5f); }
    public void PlayBackSound() { CoreManager.AudioManager.PlaySound(AudioFile.UI_NAV_BACK, 1f); }
}
