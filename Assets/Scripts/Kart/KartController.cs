using UnityEngine;
using System;
using Palmmedia.ReportGenerator.Core;
using JetBrains.Annotations;
// Kart Controller is NOT ALLOWED to use UnityEngine.InputSystem. See HumanDriver!

/**
 * Kart ControllerV3.5 by Ethan Mullen
 */
public class KartController : MonoBehaviour
{

	/* ----- Settings variables ----- */
	public Transform kartModel;

	[Header("General")]
	public float inputDeadzone = 0.1f;
	public float wheelSlideTorque = 60;
	public float damageStateSpinSpeed = 5f;

	[Header("Speed")]
	public float maxSpeed = 20f;
	public float maxBoostSpeed = 30f;
    public float acceleration = 5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public float steeringWheelTurnSpeed = 5f;
	public Vector2 turnMultiplierRangeDriftMatch = new(1.5f, 2f); // Turn boost for drift that matches joystick
	public Vector2 turnMultiplierRangeDriftDiffer = new(-0.2f, 0.35f); // Turn decrease for drift that doesn't match joystick

	[Header("Drift")]
	public float driftAge = 4f; // The time (in seconds) it takes for a drift to reach its age
	public float driftAngleMin = 0.225f;
	public float driftAngleMax = 0.4f;

	public float driftEngageSpeedPercent = 0.25f; // The percent of max speed that the player is allowed to engage a drift at
	public float driftEngageDuration = 0.33f; // The duration in seconds that it takes for the little hop to complete
	public float driftHopHeight = 0.7f;

	[Header("Boost")]
	public float requiredBoostPercentage = 0.3f;
	public float maxBoost = 3f; // Boost will be time in seconds
	public float boostGain = 1f;
	public float passiveBoostDrain = 3f;
	public float activeBoostDrain = 1.75f;

	[Header("Misc")]
	public bool drawVectors = false;

	/* ----- Runtime variable ----- */
	/* Input variables */
	[Header("Input"), SerializeField] private Vector2 turn;
	[SerializeField] private float throttle;
	[SerializeField] private bool boosting;

	private KartStateManager stateMgr;
	private Rigidbody rb;
	private PositionTracker pt;
	private float initKartModelY;

	[Header("Runtime fields")] public Vector3 up = new(0, 1, 0);
	[SerializeField] private Vector3 velocity;
	public int momentum;
	public bool grounded; // Stores last update's grounded status
	public float airtime;
	public float steeringWheelDirection; // A [-1, 1] range float indicating the amount the steering wheel is turned and the direction.
	public float boostAmount;

	public float modelTheta;

	private float driftEngageTime;
	public int driftDirection; // Indicates if we're in a left/right drift
	private float driftTheta;
	private float driftThetaTarget;
	public bool driftParticles; // True if drift particles should be showing

	public float timeSinceLastCollision;

	public float damageCooldown; // When >0, no input goes to the kart

    private void Start()
    {
		stateMgr = GetComponent<KartStateManager>();
		rb = GetComponent<Rigidbody>();
		pt = GetComponent<PositionTracker>();

		if(kartModel != null) 
			initKartModelY = kartModel.localPosition.y;
		else
			Debug.LogWarning("KartController on \"" + gameObject.name + "\" doesn't have a kartModel assigned."); 
    }

    private void Update()
    {

		/* Grounded */
		bool grounded = Grounded();
		if(grounded) { 
			airtime = 0;
		} else { 
			if(this.grounded) airtime = 0;
			airtime += Time.deltaTime;	
		}
		this.grounded = grounded;

		/* Steering wheel direction modification */
		if(Mathf.Abs(TurnInput.x) > 0) { 
			steeringWheelDirection += (!SteeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * TurnInput.x * Time.deltaTime;
			steeringWheelDirection += (!SteeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * TurnInput.x * Time.deltaTime;
			steeringWheelDirection = Mathf.Clamp(steeringWheelDirection, -1, 1);
		} else { 
			steeringWheelDirection = Mathf.Lerp(steeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+SpeedRatio) * Time.deltaTime);
			if(Mathf.Abs(steeringWheelDirection) <= inputDeadzone) 
			steeringWheelDirection = Mathf.Lerp(steeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+SpeedRatio) * Time.deltaTime);
			if(Mathf.Abs(steeringWheelDirection) <= inputDeadzone) 
				steeringWheelDirection = 0;
		}

		/* Drifting */
		driftEngageTime = Mathf.Clamp(driftEngageTime + Time.deltaTime, 0, driftEngageDuration);

		// If we're drifting and airborne, change drift direction to match joystick
		// Use airtime to ensure we maintain drift direction during small falls.
		if(stateMgr.state == KartState.DRIFTING && driftEngageTime < driftEngageDuration && Math.Abs(TurnInput.x) >= inputDeadzone) 
			driftDirection = (int)Mathf.Sign(TurnInput.x);

		driftThetaTarget = 0;
		if(stateMgr.state == KartState.DRIFTING && driftDirection != 0 && Mathf.Abs(TurnInput.x) >= inputDeadzone) { 
			driftThetaTarget = Mathf.Sign(driftDirection)*driftAngleMin;
			if(SteeringWheelMatchesDrift) 
			if(SteeringWheelMatchesDrift) 
				driftThetaTarget += steeringWheelDirection*(driftAngleMax-driftAngleMin);
		}			
		driftTheta = Mathf.Lerp(driftTheta, driftThetaTarget, 20*Time.deltaTime);
		if(Mathf.Abs(driftTheta) < 0.01f) driftTheta = 0;

		driftParticles = IsDriftEngaged && driftDirection != 0;

		/* Boosting */
		if(ActivelyBoosting) {
			boostAmount = Mathf.Max(boostAmount - activeBoostDrain*Time.deltaTime, 0); 
		} else if(driftParticles && SteeringWheelMatchesDrift) {
			boostAmount += boostGain*Time.deltaTime;
			if(boostAmount > maxBoost) boostAmount = maxBoost;
		} else { 
			boostAmount = Mathf.Max(boostAmount - passiveBoostDrain*Time.deltaTime, 0);									
		}

		if(GameplayManager.RaceManager.raceTime <= 0) {
			boostAmount = GameplayManager.RaceManager.settings.startBoostPercent*maxBoost;
		}

		/* Damage effects */
		if(damageCooldown > 0) {
			damageCooldown -= Time.deltaTime;
			if(damageCooldown < 0) damageCooldown = 0;
		}

		/* Animate kartModel forward and vertical */
		if(kartModel != null) {
			if(damageCooldown > 0) {
				modelTheta += Time.deltaTime*damageStateSpinSpeed;
				if(modelTheta > Mathf.PI*2) modelTheta -= Mathf.PI*2;
			}

			kartModel.forward = RotateVectorAroundAxis(transform.forward, transform.up, driftTheta + modelTheta);
			float t = driftEngageTime/driftEngageDuration;
			kartModel.localPosition = (initKartModelY + driftHopHeight*(-4*(t*t)+4*t))*Vector3.up;
		}

	}

	private void FixedUpdate() 
	{

		HandleVelocity();
		
		/* Tire force: Since we're simulating tires rolling, the velocity direction
		 *   should be in the direction of the transform.forward. */
		if(Grounded()) {
			Vector3 forward = RemoveUpComponent(transform.forward);
			Vector3 vel  = TrackVelocity;
			float dot = Vector3.Dot(forward, vel);
			if(Math.Abs(dot) > 0.8f) {
				// Forward is close enough in line with velocity, switch velocity to match foward
				Vector3 targetVel = forward*dot;
				rb.AddForce(targetVel-vel, ForceMode.VelocityChange);
			} else {
				// Forward is sliding sideways in relation to velocity
				// Apply a torque that rotates the car to be in line with the velocity
				float minSR = 0f;
				float sr = SpeedRatio;
				if(sr < minSR) sr = minSR;
				Vector3 rawTorque = wheelSlideTorque*Math.Max(Mathf.Abs(dot), sr)*-Vector3.Cross(rb.velocity.normalized, transform.forward);
				Vector3 torque = up.normalized*Vector3.Dot(rawTorque, up);
				rb.AddTorque(torque, ForceMode.Acceleration);
			}
		}

		/* Up force: It should always be that transform.up == up, add a torque to match this */
		float upDot = Vector3.Dot(up, transform.up);
		if(upDot < 0.975f) {
            // Quaternion.FromToRotation(transform.up, up).ToAngleAxis(out float angle, out Vector3 axis);
            // rb.AddTorque(axis * angle * Mathf.Deg2Rad * 25f, ForceMode.Acceleration);
			rb.MoveRotation(Quaternion.FromToRotation(transform.up, up) * rb.rotation);
		}

		/* Player might be stuck in ground, get em out */
		if(airtime > 0.5f && rb.velocity.y == 0) rb.MovePosition(transform.position+Vector3.up*0.1f);

	}

	/** Handle the current state of the velocity along with the user inputs to produce new
	  *   velocity/rotation. */
	private void HandleVelocity()
	{
		DrawVector(KartForward, Color.blue);
		DrawVector(rb.velocity, Color.cyan);

		// Momentum gets updated first, then kartForward second
		momentum = TrackSpeed > 0.1f ? (Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1) : 0;

		Vector3 throttleForce = (ActivelyBoosting ? 1f : ThrottleInput) * (ActivelyBoosting ? acceleration*5f : acceleration) * transform.forward;

		if(Grounded() && Mathf.Abs(throttle) > inputDeadzone && TrackSpeed <= CurrentMaxSpeed) {
			rb.AddForce(throttleForce, ForceMode.Acceleration);	
			DrawVector(throttleForce, Color.yellow);
		}

		float maxAngularVel = 2f;
		float catchupFactor = 2f*(1-Mathf.Clamp01(rb.angularVelocity.magnitude/maxAngularVel));
		Vector3 turnForce = 
			kartTurnSpeed * 															
			steeringWheelDirection *		
			DriftTurnMultiplier * 
			catchupFactor *					// If our angular momentum is really slow, add extra torque
			(momentum != -1 ? 1 : -1) *
			up;
		
		rb.AddRelativeTorque(turnForce, ForceMode.Acceleration);
		DrawVector(turnForce, Color.red);

	}

	/** The callback from KartStateManager indicating when we've changed state.
	 *  Thrown immediately before the state changes, newState is the state we're changing to. */
	public void StateChanged(KartState newState) 
	{ 
		if(newState != KartState.DRIFTING) driftDirection = 0;

		switch(newState) { 
			case KartState.DRIVING:
				break;
			case KartState.DRIFTING:
				if(!CanDriftEngage) {
					stateMgr.state = KartState.DRIVING;
					break;
				}

				driftEngageTime = 0;
				break;
			default:
				break;
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.isTrigger) return;

		for(int i = 0; i < collision.contactCount; i++) { 
			Vector3 norm = collision.GetContact(i).normal;
			if(Vector3.Dot(up, norm) < 0.5) { 
				timeSinceLastCollision = 0;
				return;
			}
		}

	}

	private Vector3 RemoveComponent(Vector3 vector, Vector3 normal)
    {
        // Calculate the projection of vector onto normal
        float projection = Vector3.Dot(vector, normal);
        Vector3 projectionVector = projection * normal;

        // Subtract the projection from the original vector
        Vector3 result = vector - projectionVector;

        return result;
    }

	/** Remove the up component of the input vector */
	public Vector3 RemoveUpComponent(Vector3 input) {
		return RemoveComponent(input, up);
	}

	/** Get the up component of the input vector*/
	public Vector3 IsolateUpComponent(Vector3 input) {
		return up.normalized*Vector3.Dot(input, up.normalized);
	}

	public Vector3 RotateVectorAroundAxis(Vector3 inputVector, Vector3 rotationAxis, float angleRadians)
    {
        rotationAxis = rotationAxis.normalized;
        Quaternion rotation = Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, rotationAxis);
        return rotation * inputVector;
    }

	public bool Grounded() 
	{ 
		float distance = 0.1f;
		Vector3 raycastOrigin = new(transform.position.x, GetComponent<BoxCollider>().bounds.min.y+0.01f, transform.position.z);
		return Physics.Raycast(raycastOrigin, Vector3.down, distance);
	}

	/* Input */
	public Vector2 TurnInput {
		get { return CanMove ? turn : Vector2.zero; }
		set {
			turn = value;
			if(turn.magnitude <= inputDeadzone) this.turn = Vector2.zero;
		}
	}

	public float ThrottleInput {
		get { return CanMove ? (ActivelyBoosting ? 1f : throttle) : 0; }
		set {
			this.throttle = value;
		}
	}

	public bool DriftInput { 
		get { return CanMove && stateMgr.state == KartState.DRIFTING; } 
		set {
			if(value && stateMgr.state == KartState.DRIVING && Grounded() && !ActivelyBoosting)
				stateMgr.state = KartState.DRIFTING;
			else if(!value && stateMgr.state == KartState.DRIFTING)
				stateMgr.state = KartState.DRIVING;
		}
	}

	public bool BoostInput {
		get { return CanMove && boosting; }
		set {
			if(value && !boosting && (boostAmount/maxBoost) >= requiredBoostPercentage) { 
				boosting = true;
				stateMgr.state = KartState.DRIVING;
			} else if(!value) { 
				if(boosting) boostAmount = 0;
				boosting = false;
			}
		}
	}

	public void DrawVector(Vector3 forceVector, Color col) 
	{
		if(drawVectors) Debug.DrawLine(transform.position, transform.position + forceVector, col);
	}

	public Vector3 KartForward { get { return RemoveUpComponent(transform.forward.normalized); } }

	public Vector3 TrackVelocity { get { 
		if(rb == null) rb = GetComponent<Rigidbody>();
		return RemoveUpComponent(rb.velocity); 
	} }
	/** The velocity magnitude tangential to the up vector */
	public float TrackSpeed { get { return TrackVelocity.magnitude; } }

	public float CurrentMaxSpeed { get { return momentum == 1 ? (ActivelyBoosting ? maxBoostSpeed : maxSpeed) : maxSpeed/4f; } }
	public float SpeedRatio { get { return TrackSpeed/CurrentMaxSpeed; } }
	public bool CanMove { get { return GameplayManager.RaceManager.CanMove && pt.lapNumber < GameplayManager.RaceManager.settings.laps && damageCooldown <= 0; } }

	public bool SteeringWheelMatchesTurn { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(turn.x); } }
	public bool SteeringWheelMatchesDrift { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(driftDirection); } }

	public bool CanDriftEngage { get { return SpeedRatio >= driftEngageSpeedPercent && momentum == 1 && CanMove; } }

	public bool ActivelyBoosting { get { return CanMove && boosting && boostAmount > 0;} }
	public float BoostRatio { get { return boostAmount/maxBoost; } }
	
	public bool IsDriftEngaged { get { return stateMgr.state == KartState.DRIFTING && driftEngageTime == driftEngageDuration; } }
	public float DriftTurnMultiplier { 
		get { 
			if(stateMgr.state == KartState.DRIFTING && IsDriftEngaged) {
				float driftAgeRatio = stateMgr.timeInState/driftAge;
				return SteeringWheelMatchesDrift ? 
					Mathf.Lerp(turnMultiplierRangeDriftMatch.x, turnMultiplierRangeDriftMatch.y, 1-driftAgeRatio) :
					Mathf.Lerp(turnMultiplierRangeDriftDiffer.x, turnMultiplierRangeDriftDiffer.y, 1-driftAgeRatio);
			} else 
				return 1f;
		} 
	}

}