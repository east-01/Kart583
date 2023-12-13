using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaceCamera : MonoBehaviour
{

    public AnimationCurve titleFade;
    public TMP_Text mapTitleText;
    public float startAnimationDuration;
    public float startAnimationTimeLeft;

    void Awake() 
    {
        StartAnimation();
    }

    void Update()
    {
        if(startAnimationTimeLeft > 0) {
            startAnimationTimeLeft -= Time.deltaTime;
        }

        if(startAnimationDuration <= 0) {
            startAnimationDuration = 0;
            mapTitleText.enabled = false;
            return;
        }

        RaceManager rm = GameplayManager.RaceManager;
        if(rm.raceTime > 0) return;

        float animProgress = 1-(startAnimationTimeLeft/startAnimationDuration);

        mapTitleText.alpha = titleFade.Evaluate(animProgress);
        if(GameplayManager.IntroCamData != null) {
            transform.position = Vector3.Lerp(GameplayManager.IntroCamData.CamStartPos.position, GameplayManager.IntroCamData.CamEndPos.position, animProgress);
        }
    }

    public void StartAnimation() {
        startAnimationTimeLeft = startAnimationDuration;
        mapTitleText.enabled = true;
    }

    public bool Animating { get { return startAnimationTimeLeft > 0; } }

}
