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
    public float startAnimationTimeLeft;

    void Start() 
    {
        if(GameplayManager.Instance.playStartAnimation) StartAnimation();
    }

    void Update()
    {
        if(startAnimationTimeLeft > 0) {
            startAnimationTimeLeft -= Time.deltaTime;
        }

        if(startAnimationTimeLeft < 0) {
            startAnimationTimeLeft = 0;
            mapTitleText.enabled = false;
            return;
        }

        RaceManager rm = GameplayManager.RaceManager;
        if(rm.raceTime > 0) return;

        float animProgress = 1-(startAnimationTimeLeft/GameplayManager.Instance.startAnimationDuration);

        mapTitleText.alpha = titleFade.Evaluate(animProgress);

        if(GameplayManager.IntroCamData != null) {
            transform.position = Vector3.Lerp(GameplayManager.IntroCamData.CamStartPos.position, GameplayManager.IntroCamData.CamEndPos.position, animProgress);
        }
    }

    public void StartAnimation() {
        startAnimationTimeLeft = GameplayManager.Instance.startAnimationDuration;
        if(startAnimationTimeLeft > 0) {
            mapTitleText.enabled = true;
        }
    }

    public bool Animating { get { return startAnimationTimeLeft > 0; } }

}
