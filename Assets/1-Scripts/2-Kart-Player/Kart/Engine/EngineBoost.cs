using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Engine component: Boost
/// Reponsible for
///  - Responds to drift time to generate boost
///  - Puts engine in the proper state for player to go max speed
/// </summary>
public class EngineBoost : KartBehavior
{

#region Configuration fields
    /// <summary>Allow boost to drain naturally</summary>
    [Header("Configuration fields"), SerializeField] private bool drainBoostNatural = true;
    /// <summary>Allow boost to drain while using it</summary>
    [SerializeField] private bool drainBoostUsing = true;

    [SerializeField] private float requiredBoostPercentage = 0.3f;
    public new float RequiredBoostPercentage => requiredBoostPercentage;
    [SerializeField] private float boostGain = 1f;
    [SerializeField] private float passiveBoostDrain = 3f;
    [SerializeField] private float activeBoostDrain = 1.75f;
    /// <summary>Converts the boost decay time into a [0,1] float representing the speed of passive boost drain</summary>
    [SerializeField] private AnimationCurve boostDecayCurve;
#endregion

#region Runtime fields
    public bool Boosting { get; private set; }
    public new float BoostAmount { get; private set; }
    /// <summary>Time counting how long its been for the boost to drain</summary>
	public new float BoostDecayTime { get; private set; }
#endregion

#region Utility fields
    public bool CanEngageBoost => BoostRatio >= requiredBoostPercentage;

	public new bool ActivelyBoosting { get { return kartCtrl.CanMove && Boosting && BoostAmount > 0;} }
	public new float BoostRatio { get { return BoostAmount/Settings.maxBoost; } }
#endregion

    protected new void Awake() 
    {
        base.Awake();
        
    }

    private void Update() 
    {
        /* Boosting */
		if(ActivelyBoosting) { 
            // Using boost, drain it
			BoostDecayTime = 0;
            if(drainBoostUsing)
    			BoostAmount = Mathf.Max(BoostAmount - activeBoostDrain*Time.deltaTime, 0); 
		} else if(EngineWheels.IsDriftEngaged && EngineSteeringWheel.SteeringWheelMatchesDrift) {
            // Drifting to gain boost, add boost gain
			BoostDecayTime = 0;
			BoostAmount += boostGain*Time.deltaTime;
			if(BoostAmount > Settings.maxBoost) BoostAmount = Settings.maxBoost;
		} else if(drainBoostNatural) { 
            // Not using boost, drain it naturally
			BoostDecayTime += Time.deltaTime;
			BoostAmount = Mathf.Max(BoostAmount - boostDecayCurve.Evaluate(BoostDecayTime)*passiveBoostDrain*Time.deltaTime, 0);									
		}

		if(kartCtrl.GameplayManager.RaceManager.RaceTime <= 0) {
			BoostAmount = kartCtrl.GameplayManager.RaceManager.settings.startBoostPercent*Settings.maxBoost;
		}
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

    public float SetBoostDecayTime(float boostDecayTime) => this.BoostDecayTime = boostDecayTime;
    public void SetBoostToMax() => this.BoostAmount = Settings.maxBoost;

}
