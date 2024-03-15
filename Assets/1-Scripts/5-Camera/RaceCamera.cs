using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaceCamera : MonoBehaviour, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    public AnimationCurve titleFade;
    public TMP_Text mapTitleText;
    public float startAnimationTimeLeft;

    void Awake() 
    {
        SceneDelegate.Instance.SubscribeForGameplayManager(this);        
    }

    void Start() 
    {
        print("TODO: Play start animation after all players join");
        // if(GameplayManager.Instance.playStartAnimation) StartAnimation();
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

    void Update()
    {
        if(gameplayManager == null) 
            return;

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

        RaceManager rm = gameplayManager.RaceManager;
        if(rm.RaceTime > 0) return;

        float animProgress = 1-(startAnimationTimeLeft/gameplayManager.startAnimationDuration);

        mapTitleText.alpha = titleFade.Evaluate(animProgress);

        if(kartLevelManager.IntroCamData != null) {
            transform.position = Vector3.Lerp(kartLevelManager.IntroCamData.CamStartPos.position, kartLevelManager.IntroCamData.CamEndPos.position, animProgress);
        }
    }

    public void StartAnimation() {
        startAnimationTimeLeft = gameplayManager.startAnimationDuration;
        if(startAnimationTimeLeft > 0) {
            mapTitleText.enabled = true;
        }
    }

    public bool Animating { get { return startAnimationTimeLeft > 0; } }

}
