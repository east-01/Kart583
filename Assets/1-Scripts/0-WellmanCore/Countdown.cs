using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Countdown : MonoBehaviour, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;

    [SerializeField]
    private AudioClip[] _audioClips;
    private AudioClip _currentAudioClip;

    private AudioSource _audioSource;

    private int _currentNumber;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
    }

    void Update()
    {
        if(gameplayManager == null)
            return;
            
        RaceManager rm = gameplayManager.RaceManager;
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
