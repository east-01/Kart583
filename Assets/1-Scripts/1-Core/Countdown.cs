using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Countdown : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] _audioClips;
    private AudioClip _currentAudioClip;

    private AudioSource _audioSource;

    private int _currentNumber;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        RaceManager rm = GameplayManager.RaceManager;
        if(rm.RaceTime > 0) { 
            Destroy(gameObject);
            return;
        }

        bool changedNumbers = _currentNumber != (int)Mathf.Abs(rm.RaceTime);
        _currentNumber = (int)Mathf.Abs(rm.RaceTime);

        if (_currentNumber < _audioClips.Length && changedNumbers)
        {
            _currentAudioClip = _audioClips[_currentNumber];
            _audioSource.PlayOneShot(_currentAudioClip);
        }
    }
}
