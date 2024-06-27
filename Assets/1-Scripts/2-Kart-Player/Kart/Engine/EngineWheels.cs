using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Engine component: Tires, driven by EngineBase
/// Reponsible for
///  - Turning
///  - Drifting
///  - Recieving torque from the engine
///  - Physics interactions such that velocity vector is in line with tire vector
///  - Suspending the car above the track
/// </summary>
public class EngineWheels : KartBehavior
{

#region Configuration fields
    [Header("Configuration fields"), SerializeField] private float rideHeight = 0.75f;
    public float RideHeight => rideHeight;
	[SerializeField] private float wheelSlideTorque = 60;
	[SerializeField] private float velocityDecay = 10f;

    /* Turning configuration */
    /// <summary>Turn boost for drift that matches joystick</summary>
	[SerializeField] private Vector2 turnMultiplierRangeDriftMatch = new(1.5f, 2f);
    /// <summary>Turn decrease for drift that doesn't match joystick</summary>
	[SerializeField] private Vector2 turnMultiplierRangeDriftDiffer = new(-0.2f, 0.35f);

    /* Drift configuration */
    /// <summary>The time (in seconds) it takes for a drift to reach its age</summary>
    [SerializeField] private float driftAge = 4f;
    [SerializeField] private Vector2 driftAngleExtents = new(0.225f, 0.4f);
    public Vector2 DriftAngleExtents => driftAngleExtents;

    /// <summary>The percent of max speed that the player is allowed to engage a drift at</summary>
	[SerializeField] private float driftEngageSpeedPercent = 0.25f;
    /// <summary>The duration in seconds that it takes for the little hop to complete</summary>
	[SerializeField] private float driftEngageDuration = 0.33f;
    public float DriftEngageDuration => driftEngageDuration;
#endregion

#region Runtime fields
	public int Momentum { get; private set; }

	public float TurnForce { get; private set;}
	public float TurnForceMax { get; private set; }

    /// <summary>Stores last update's grounded status</summary>
	public new bool Grounded { get; private set; }
	private RaycastHit groundHit;
    public RaycastHit GroundHit => groundHit;
	public float DistanceFromGround { get; private set; }
	public float Airtime { get; private set; }
	private LinearVelocityState linearVelocityState;
	public LinearVelocityState LinearVelocityState { 
		get { return linearVelocityState; } 
		private set {
			TimeInLinearVelocityState = 0f;
			LinearVelocityState prev = linearVelocityState;
			linearVelocityState = value;
			LinearVelocityStateChangedEvent?.Invoke(value, prev);
		} 
	}
	public float TimeInLinearVelocityState { get; private set; }

    public bool Drifting { get; private set; }
	public float DriftTimeElapsed { get; private set; }
    /// <summary>Indicates if we're in a left/right drift</summary>
	public new int DriftDirection { get; private set; }
#endregion

#region Utility fields
	public bool CanDriftEngage => kartCtrl.CanMove && kartCtrl.RawDriftInput && Grounded && EngineBase.SpeedRatio >= driftEngageSpeedPercent && Momentum == 1 && !EngineBoost.ActivelyBoosting;
	public bool IsHopping => DriftTimeElapsed >= 0 && DriftTimeElapsed < DriftEngageDuration;
#endregion

#region Events
	public delegate void LinearVelocityStateHandler(LinearVelocityState curr, LinearVelocityState prev);
	public event LinearVelocityStateHandler LinearVelocityStateChangedEvent;

	public delegate void KartLandedHandler();
	public event KartLandedHandler KartLandedEvent;
#endregion

    protected new void Awake() 
    {
        base.Awake();

    }

	private void OnEnable() { KartLandedEvent += EngineWheels_KartLanded; }
	private void OnDisable() { KartLandedEvent -= EngineWheels_KartLanded; }

    private void Update() 
    {
        /* Grounded */
		bool lastFrameGrounded = this.Grounded;
		Grounded = CheckGrounded();
		if(Grounded) { 
			if(Airtime > 0.25f)
				kartVisualsManager.SpawnLandEffect(transform);
			if(Airtime > 0)
				KartLandedEvent?.Invoke();

			Airtime = 0;
		} else { 
			if(lastFrameGrounded) Airtime = 0; // We've just gone airborne, reset airtime
			Airtime += Time.deltaTime;	
		}

        /* Drifting */
		if(DriftTimeElapsed >= 0)
			DriftTimeElapsed += Time.deltaTime;

		// If we're drifting and airborne, change drift direction to match joystick
		// Use airtime to ensure we maintain drift direction during small falls.
		if(Drifting && IsHopping && Math.Abs(kartCtrl.TurnInput.x) >= EngineSteeringWheel.INPUT_DEADZONE) 
			DriftDirection = (int)Mathf.Sign(kartCtrl.TurnInput.x);

        bool exitDriftState = (Grounded && EngineSteeringWheel.SteeringWheelDirection == 0 && DriftTimeElapsed >= 0.15f) || !CanDriftEngage || ActivelyBoosting;
        if(Drifting && exitDriftState) {
            SetDrifting(false);
        }
    }

    private void FixedUpdate() 
    {
		/* Momentum calculation */
		if(EngineBase.TrackSpeed > 0.1f) {
			if(Vector3.Dot(rb.velocity, transform.forward) >= 0)
				Momentum = 1;
			else
				Momentum = -1;
		} else
			Momentum = 0;

		UpdateLinearVelocityState();

        /* Variables */
        float throttleInput = kartCtrl.ThrottleInput;
		float processedThrottleInput = EngineBoost.ActivelyBoosting ? 1f : throttleInput;
		float processedAccelerationInput = Settings.acceleration;
		if(EngineBoost.ActivelyBoosting)
			processedAccelerationInput *= 5f;
		else if(LinearVelocityState == LinearVelocityState.BRAKING_DECELERATION)
			processedAccelerationInput *= 2.5f;

        /* Forward/backward velocity */
		BLog.Highlight("Linear velocity state: " + LinearVelocityState);
		switch(LinearVelocityState) {
			case LinearVelocityState.NORMAL_ACCELERATION:
				Vector3 throttleForce = processedThrottleInput * processedAccelerationInput * transform.forward;
				rb.AddForce(throttleForce, ForceMode.Acceleration);	
				KartVectorDrawer.DrawVector(throttleForce, Color.yellow);
				break;
			case LinearVelocityState.BRAKING_DECELERATION:
				throttleForce = processedThrottleInput * processedAccelerationInput * transform.forward;
				rb.AddForce(throttleForce, ForceMode.Acceleration);	
				break;
			case LinearVelocityState.VELOCITY_DECAY:
				rb.AddForce(-kartCtrl.RemoveUpComponent(rb.velocity.normalized)*(velocityDecay*Time.deltaTime), ForceMode.VelocityChange);
				break;
			case LinearVelocityState.STOPPED:
				rb.velocity = Vector3.zero;
				break;
		}

		TurnForce = EngineSteeringWheel.SteeringWheelDirection*
					Settings.turnSpeed*
					DriftTurnMultiplier*
					(Grounded ? 1 : 0.25f)*
					(Momentum != -1 ? 1 : -1);

		// All values that could be 1 are set to 1
		TurnForceMax = Mathf.Sign(EngineSteeringWheel.SteeringWheelDirection)*
					   Settings.turnSpeed*
					   DriftTurnMultiplier*
					   (Momentum != -1 ? 1 : -1);

		/* Turning: Each frame, we want to change transform.forward by a certain amount specified by the steeringWheelDirection. */
		if(Math.Abs(EngineSteeringWheel.SteeringWheelDirection) > EngineSteeringWheel.INPUT_DEADZONE)				
			rb.angularVelocity = Up*TurnForce;
		else
			rb.angularVelocity = Vector3.zero;

        /* Tire force: Since we're simulating tires rolling, the velocity direction
		 *   should be in the direction of the transform.forward. */
		if(Grounded) {
			Vector3 forward = kartCtrl.RemoveUpComponent(transform.forward);
			Vector3 vel  = EngineBase.TrackVelocity;
			float dot = Vector3.Dot(forward, vel);
			if(Math.Abs(dot) > 0.8f) {
				// Forward is close enough in line with velocity, switch velocity to match foward
				Vector3 targetVel = forward*dot;
				rb.AddForce(targetVel-vel, ForceMode.VelocityChange);
			} else if(EngineBase.SpeedRatio > 0 && EngineBase.SpeedRatio != Mathf.Infinity) {
				// Forward is sliding sideways in relation to velocity
				// Apply a torque that rotates the car to be in line with the velocity
				Vector3 rawTorque = wheelSlideTorque*Math.Max(Mathf.Abs(dot), EngineBase.SpeedRatio)*-Vector3.Cross(rb.velocity.normalized, transform.forward);
				Vector3 torque = Up.normalized*Vector3.Dot(rawTorque, Up);
				rb.AddTorque(torque, ForceMode.Acceleration);
			}
		}
    }

	private void UpdateLinearVelocityState() 
	{
		float throttleInput = kartCtrl.ThrottleInput;
		bool throttleInputValid = Mathf.Abs(throttleInput) > EngineSteeringWheel.INPUT_DEADZONE;
		bool throttleMatchesMomentum;
		if(Momentum != 0)
			throttleMatchesMomentum = Mathf.Sign(throttleInput) == Momentum;
		else
			throttleMatchesMomentum = true;

		BLog.Highlight($"sign ti: {Mathf.Sign(throttleInput)} == {Momentum} yields {throttleMatchesMomentum}");
		bool canAccelerate = Momentum == 0 || EngineBase.TrackSpeed <= EngineBase.CurrentMaxSpeed;
		BLog.Highlight($"can accelerate " + canAccelerate);
		bool shouldApplyNormalAcceleration = canAccelerate && throttleMatchesMomentum;
		bool shouldApplyBreakingDeceleration = canAccelerate && !throttleMatchesMomentum;
		bool normalAcceleration = throttleInputValid && shouldApplyNormalAcceleration;

		bool trackSpeedAboveThreshold = EngineBase.TrackSpeed > 0.1f;

		if(normalAcceleration) {
			LinearVelocityState = LinearVelocityState.NORMAL_ACCELERATION;
		} else if(trackSpeedAboveThreshold) {
			LinearVelocityState = LinearVelocityState.VELOCITY_DECAY;
		} else {
			LinearVelocityState = LinearVelocityState.STOPPED;
		}
	}

	private void EngineWheels_KartLanded()
    {
		if(CanDriftEngage)
			SetDrifting(true, true);
    }

    /// <summary>Attempt to engage drift, has the possibility of failing due to missed conditions.</summary>
    public void SetDrifting(bool drifting, bool skipHop = false) 
    {
		if(IsHopping)
			return;
		if(EngineBase.SpeedRatio < driftEngageSpeedPercent)
			return;

		DriftDirection = 0;
		if(drifting && CanMove) {
			Drifting = true;
			DriftDirection = (int)Mathf.Sign(kartCtrl.TurnInput.x);
			DriftTimeElapsed = skipHop ? driftEngageDuration : 0;
		} else {
			Drifting = false;
			DriftTimeElapsed = -1;
		}
    }

    private bool CheckGrounded() 
	{ 
		Physics.Raycast(transform.position, -transform.up, out groundHit, rideHeight);
		if(GroundHit.collider != null && GroundHit.collider.CompareTag("Ground")) {
			DistanceFromGround = GroundHit.distance;
			if(DistanceFromGround <= rideHeight) {
				EngineBase.SetUpVector(GroundHit.normal);
				return true;
			}
		}
		DistanceFromGround = -1;
		return false;
	}

    public new bool IsDriftEngaged { get { return Drifting && DriftTimeElapsed >= driftEngageDuration; } }
	public float DriftTurnMultiplier { 
		get { 
			if(IsDriftEngaged) {
				float driftAgeRatio = DriftTimeElapsed/driftAge;
				return EngineSteeringWheel.SteeringWheelMatchesDrift ? 
					Mathf.Lerp(turnMultiplierRangeDriftMatch.x, turnMultiplierRangeDriftMatch.y, 1-driftAgeRatio) :
					Mathf.Lerp(turnMultiplierRangeDriftDiffer.x, turnMultiplierRangeDriftDiffer.y, 1-driftAgeRatio);
			} else 
				return 1f;
		} 
	}

}

public enum LinearVelocityState {
	NORMAL_ACCELERATION,
	BRAKING_DECELERATION,
	VELOCITY_DECAY,
	STOPPED
}