using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Engine component: Tires, driven by EngineBase
/// Reponsible for
///  - Simulating RPM
///  - Generating torque for tires
/// </summary>
public class EngineBase : KartBehavior, GameplayManagerBehavior
{

#region Configuration fields
    // [Header("Configuration fields")]
#endregion

#region Runtime fields
    private GameplayManager gameplayManager;

    /// <summary>The up vector that the kart follows. Will always be normalized.</summary>
    public new Vector3 Up { get; private set; } = new(0, 1, 0);
    public new Vector3 TrackVelocity { get; private set; }
    public new float TrackSpeed { get; private set; }
    public float TrackSpeedDerivative { get; private set; }
    /// <summary>Time engine is stalled, stalls usually come from damage</summary>
    public new float EngineStallTime { get; private set; } = 0;
    public StallType StallType { get; private set; } = StallType.NONE;
#endregion

#region Utility fields
    public new float CurrentMaxSpeed { get {
        float maxSpeed = EngineBoost.ActivelyBoosting ? Settings.maxBoostSpeed : Settings.maxSpeed;
        float absSteeringWheelDirection = Mathf.Abs(EngineSteeringWheel.SteeringWheelDirection);
        if(absSteeringWheelDirection >= EngineSteeringWheel.INPUT_DEADZONE)
            return maxSpeed*absSteeringWheelDirection;
        else if(EngineWheels.Momentum == 1)
            return maxSpeed;
        else
            return Settings.maxSpeed/4f;
    } }
	public new float SpeedRatio => EngineBase.TrackSpeed/CurrentMaxSpeed;
#endregion

#region Events
    public delegate void EngineStallHandler(float stallTime, StallType stallType);
    public EngineStallHandler EngineStallEvent;
#endregion

    protected new void Awake() 
    {
        base.Awake();
        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
    }

    private void Update() 
    {
        /* Engine stall */
        if(EngineStallTime > 0)
			EngineStallTime = Math.Max(EngineStallTime-Time.deltaTime, 0);

        if(Input.GetKeyDown(KeyCode.J))
            ApplyStall(2.5f, StallType.SMALL);
    }

    private void FixedUpdate() 
    {
        if(gameplayManager == null)
            return;

        /* Track velocity*/
        TrackVelocity = kartCtrl.RemoveUpComponent(rb.velocity);
        float oldTrackSpeed = TrackSpeed;
        TrackSpeed = TrackVelocity.magnitude;
        TrackSpeedDerivative = (TrackSpeed-oldTrackSpeed)/Time.deltaTime;
  
        /* Up force: It should always be that transform.Up == up */
		// Code found here https://gamedev.stackexchange.com/questions/194641/how-to-set-transform-up-without-locking-the-y-axis
		Quaternion zToUp = Quaternion.LookRotation(Up, -transform.forward);
		Quaternion yToz = Quaternion.Euler(90, 0, 0);
		transform.rotation = zToUp * yToz;
    
		/* Apply gravity */
		if(!Grounded) {
            RacePhase phase = gameplayManager.RaceManager.Phase;
            float gravityForce = Physics.gravity.magnitude*
                                 ((phase == RacePhase.COUNTDOWN || phase == RacePhase.WAITING_FOR_PLAYERS) ? 10f : 1f);
			rb.AddForce(-Up.normalized*gravityForce, ForceMode.Acceleration);
        }

		/* Check if player is stuck in ground*/
		if(Grounded && EngineWheels.DistanceFromGround < EngineWheels.RideHeight-0.015f && EngineWheels.DistanceFromGround != -1)
			transform.position = EngineWheels.GroundHit.point + Up*EngineWheels.RideHeight;

		/* Interpolate up vector back to default if we're not grounded */
		if(!Grounded)
			Up = Vector3.Lerp(Up, Vector3.up, Mathf.Clamp01(EngineWheels.Airtime/0.5f));
    }

    public void SetUpVector(Vector3 upVector) => Up = upVector;
    public void ApplyStall(float stallTime, StallType type) {
        if(StallType > type)
            return;
        EngineStallTime = stallTime;
        StallType = type;
        EngineStallEvent?.Invoke(stallTime, type);
    }
}

// Order elements by strength, later entries get higher priority
public enum StallType {
    NONE,
    SMALL,
    LARGE
}