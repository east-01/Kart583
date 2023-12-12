using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Countdown : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _countdownNumbers;

    [SerializeField]
    private AudioClip[] _audioClips;
    private AudioClip _currentAudioClip;

    private AudioSource _audioSource;

    private float _timePerNumber;
    private float _timer;
    private int _currentNumber;

    public bool StartTimer;

    private void Awake()
    {
        _currentNumber = 4;
        _countdownNumbers[_currentNumber].SetActive(true);
        _timer = 1f;
        _timePerNumber = 1f;
        _audioSource = GetComponent<AudioSource>();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (StartTimer)
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0f && _currentNumber != 0)
            {
                _countdownNumbers[_currentNumber].SetActive(false);
                _currentNumber--;
                _countdownNumbers[_currentNumber].SetActive(true);
                _currentAudioClip = _audioClips[_currentNumber];
                _audioSource.PlayOneShot(_currentAudioClip);
                _timer = _timePerNumber;
            }
            else if(_timer <= 0f && _currentNumber == 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
