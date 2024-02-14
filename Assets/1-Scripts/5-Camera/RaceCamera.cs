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
        print("TODO: Play start animation after all players join");
        // if(GameplayManager.Instance.playStartAnimation) StartAnimation();
    }

    void Update()
    {
        if(GameplayManager.Instance == null) return;

        if(startAnimationTimeLeft > 0) {
            startAnimationTimeLeft -= Time.deltaTime;
        }

        if(startAnimationTimeLeft < 0) {
            startAnimationTimeLeft = 0;
            mapTitleText.enabled = false;
            return;
        }

        // Ensure that the camera is active while animating, the PlayerInputManager likes to disable these early
        if(Animating) {
            GetComponent<Camera>().enabled = true;
            GetComponent<AudioListener>().enabled = true;
        }

        RaceManager rm = GameplayManager.RaceManager;
        if(rm.RaceTime > 0) return;

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
