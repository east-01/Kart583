using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Steamworks;
using UnityEngine;

/// <summary>
/// Engine component: Boost
/// Reponsible for
///  - Responds to drift time to generate boost
///  - Puts engine in the proper state for player to go max speed
/// </summary>
public class EngineBoost : KartBehavior, GameplayManagerBehavior
{

#region Configuration fields
    /// <summary>Allow boost to drain naturally</summary>
    [Header("Configuration fields"), SerializeField] private bool drainBoostNatural = true;
    /// <summary>Allow boost to drain while using it</summary>
    [SerializeField] private bool drainBoostUsing = true;

    [SerializeField] private float requiredBoostPercentage = 0.3f;
    public new float RequiredBoostPercentage => requiredBoostPercentage;
    [SerializeField] private float boostGain = 1f;
    /// <summary>Boost drain factor without any interaction</summary>
    [SerializeField] private float boostDrainNaturalPassive = 1.8f;
    /// <summary>Boost drain factor after the player uses boost and stops</summary>
    [SerializeField] private float boostDrainNaturalPostUse = 3f;
    /// <summary>Boost drain factor while the player is using boost</summary>
    [SerializeField] private float boostDrainUsing = 1.75f;
    /// <summary>Converts the boost decay time into a [0,1] float representing the speed of passive boost drain</summary>
    [SerializeField] private AnimationCurve boostDecayCurve;
#endregion

#region Runtime fields
    private GameplayManager gameplayManager;

    public bool Boosting { get; private set; }
    public new float BoostAmount { get; private set; }
    /// <summary>Time counting how long its been for the boost to drain</summary>
	public new float BoostDecayTime { get; private set; }
    private BoostDecayType boostDecayType; 
#endregion

#region Utility fields
    public bool CanEngageBoost => BoostRatio >= requiredBoostPercentage;

	public new bool ActivelyBoosting { get { return kartCtrl.CanMove && Boosting && BoostAmount > 0;} }
	public new float BoostRatio { get { return BoostAmount/Settings.maxBoost; } }
#endregion

    protected new void Awake() 
    {
        base.Awake();
        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.gameplayManager.RaceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;
    }

    private void OnEnable() 
    {
        if(gameplayManager != null) 
            gameplayManager.RaceManager.RacePhaseChanged += RaceManager_RacePhaseChanged;            
    }

    private void OnDisable() 
    {
        if(gameplayManager != null) 
            gameplayManager.RaceManager.RacePhaseChanged -= RaceManager_RacePhaseChanged;            
    }

    private void Update() 
    {
        void ChangeBoostValue(float val) => BoostAmount = Mathf.Clamp(BoostAmount + val, 0, Settings.maxBoost);

        bool gainingBoost = !ActivelyBoosting && EngineWheels.IsDriftEngaged;
        if(gainingBoost) {
            BoostDecayTime = 0;
            boostDecayType = BoostDecayType.NATURAL_PASSIVE;
            if(EngineSteeringWheel.SteeringWheelMatchesDrift)
                ChangeBoostValue(boostGain*Time.deltaTime);
        } else if(ActivelyBoosting && drainBoostUsing) {
            BoostDecayTime = 0;
            boostDecayType = BoostDecayType.NATURAL_POST_USE;
            ChangeBoostValue(-boostDrainUsing*Time.deltaTime);
        } else if(!ActivelyBoosting && drainBoostNatural) {
            BoostDecayTime += Time.deltaTime;

            float decayFactor = boostDecayType == BoostDecayType.NATURAL_PASSIVE ? boostDrainNaturalPassive : boostDrainNaturalPostUse;
            float decayCurve = boostDecayType == BoostDecayType.NATURAL_PASSIVE ? boostDecayCurve.Evaluate(BoostDecayTime) : 1f;
            ChangeBoostValue(-decayFactor*decayCurve*Time.deltaTime);
        }

        bool shouldExitBoost = BoostAmount <= 0.01;
        if(Boosting && shouldExitBoost)
            SetBoosting(false);
    }

    private void FixedUpdate() 
    {

    }

    public void SetBoosting(bool boosting) 
    {
        if(EngineWheels.IsHopping)
            return;
        if(boosting && !CanEngageBoost)
            return;

        if(EngineWheels.Drifting)
            EngineWheels.SetDrifting(false);

        Boosting = boosting;
        if(!boosting)
            BoostDecayTime = 0;
    }

    private void RaceManager_RacePhaseChanged(RacePhase previousPhase, RacePhase currentPhase)
    {
        drainBoostNatural = currentPhase == RacePhase.RACING || currentPhase == RacePhase.FINISHED;

        if(currentPhase == RacePhase.COUNTDOWN)
            BoostAmount = gameplayManager.RaceManager.Settings.startBoostPercent*Settings.maxBoost;
    }

    public float SetBoostDecayTime(float boostDecayTime) => this.BoostDecayTime = boostDecayTime;
    public void SetBoostDecayType(BoostDecayType boostDecayType) => this.boostDecayType = boostDecayType;
    public void SetBoostToMax() => this.BoostAmount = Settings.maxBoost;

}

public enum BoostDecayType {
    NATURAL_PASSIVE,
    NATURAL_POST_USE,
}