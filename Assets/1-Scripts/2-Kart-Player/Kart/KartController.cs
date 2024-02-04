using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine.Pool;
using UnityEditor;
// Kart Controller is NOT ALLOWED to use UnityEngine.InputSystem. See HumanDriver!

/**
 * Kart ControllerV3.5 by Ethan Mullen
 */
public class KartController : KartBehavior
{

	/* ----- Settings variables ----- */
	private Transform kartModel;

	public KartSettings settings;

	[Space, Header("General")]
	public float inputDeadzone = 0.1f;
	public float wheelSlideTorque = 60;
	public float damageStateSpinSpeed = 5f;
	public float rideHeight = 0.75f;
	public float velocityDecay = 10f;
	public float minimumVelocityThreshold = 0.1f; // If the angular/regular velocity of the rigidbody is less than this, set to zero

	[Header("Turning")]
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
	public float boostGain = 1f;
	public float passiveBoostDrain = 3f;
	public float activeBoostDrain = 1.75f;
	public AnimationCurve boostDecayCurve; // Converts the boost decay time into a [0,1] float representing the speed of passive boost drain

	[Header("Misc")]
	public bool drawVectors = false;

	/* ----- Runtime variable ----- */
	/* Input variables */
	[Header("Input"), SerializeField] private Vector2 turn;
	[SerializeField] private float throttle;
	[SerializeField] private bool boosting;

	private float initKartModelY;

	[Header("Runtime fields")] public Vector3 up = new(0, 1, 0);
	[SerializeField] private Vector3 velocity;
	public int momentum;
	public bool grounded; // Stores last update's grounded status
	public RaycastHit groundHit;
	public float distanceFromGround;
	public float airtime;
	public float steeringWheelDirection; // A [-1, 1] range float indicating the amount the steering wheel is turned and the direction.
	public float boostAmount;
	public float boostDecayTime; // Time counting how long its been for the boost to drain

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

		KartDataPackage kdp = GameplayManager.KartAtlas.RetrieveData(kartManager.GetPlayerData().kartName);
		settings = kdp.settings;
	
		GameObject newKartModel = Instantiate(kdp.model.gameObject, transform);
		newKartModel.GetComponent<KartModel>().SetKartController(this);
		kartModel = newKartModel.transform;

		if(kartModel != null) 
			initKartModelY = kartModel.localPosition.y;
		else
			Debug.LogWarning("KartController on \"" + gameObject.name + "\" doesn't have a kartModel assigned."); 
    }

    private void Update()
    {

		/* Grounded */
		bool lastFrameGrounded = this.grounded;
		grounded = Grounded();
		if(grounded) { 
			airtime = 0;
		} else { 
			if(lastFrameGrounded) airtime = 0; // We've just gone airborne, reset airtime
			airtime += Time.deltaTime;	
		}

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
		if(kartStateManager.state == KartState.DRIFTING && driftEngageTime < driftEngageDuration && Math.Abs(TurnInput.x) >= inputDeadzone) 
			driftDirection = (int)Mathf.Sign(TurnInput.x);

		driftThetaTarget = 0;
		if(kartStateManager.state == KartState.DRIFTING && driftDirection != 0 && Mathf.Abs(TurnInput.x) >= inputDeadzone) { 
			driftThetaTarget = Mathf.Sign(driftDirection)*driftAngleMin;
			if(SteeringWheelMatchesDrift) 
				driftThetaTarget += steeringWheelDirection*(driftAngleMax-driftAngleMin);
		}			
		driftTheta = Mathf.Lerp(driftTheta, driftThetaTarget, 20*Time.deltaTime);
		if(Mathf.Abs(driftTheta) < 0.01f) driftTheta = 0;

		driftParticles = IsDriftEngaged && driftDirection != 0;

		/* Boosting */
		if(ActivelyBoosting) {
			boostDecayTime = 0;
			boostAmount = Mathf.Max(boostAmount - activeBoostDrain*Time.deltaTime, 0); 
		} else if(driftParticles && SteeringWheelMatchesDrift) {
			boostDecayTime = 0;
			boostAmount += boostGain*Time.deltaTime;
			if(boostAmount > settings.maxBoost) boostAmount = settings.maxBoost;
		} else { 
			boostDecayTime += Time.deltaTime;
			boostAmount = Mathf.Max(boostAmount - boostDecayCurve.Evaluate(boostDecayTime)*passiveBoostDrain*Time.deltaTime, 0);									
		}

		if(GameplayManager.RaceManager.RaceTime <= 0) {
			boostAmount = GameplayManager.RaceManager.settings.startBoostPercent*settings.maxBoost;
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
			} else 
				modelTheta = 0;

			kartModel.forward = RotateVectorAroundAxis(transform.forward, transform.up, driftTheta + modelTheta);
			float t = driftEngageTime/driftEngageDuration;
			kartModel.localPosition = (initKartModelY + driftHopHeight*(-4*(t*t)+4*t))*Vector3.up;
		}

	}

	private void FixedUpdate() 
	{

		DrawVector(KartForward, Color.blue);
		DrawVector(rb.velocity, Color.cyan);
		DrawVector(rb.angularVelocity, Color.cyan);
		DrawVector(up, Color.green);

		grounded = Grounded();
		momentum = TrackSpeed > 0.1f ? (Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1) : 0;

		/* Forward/backward velocity */
		if(Mathf.Abs(throttle) > inputDeadzone && (TrackSpeed <= CurrentMaxSpeed || Mathf.Sign(ThrottleInput) != momentum)) {
			// Adding
			Vector3 throttleForce = (ActivelyBoosting ? 1f : ThrottleInput) * (ActivelyBoosting ? settings.acceleration*5f : settings.acceleration) * transform.forward;

			if(grounded) {
				rb.AddForce(throttleForce, ForceMode.Acceleration);	
				DrawVector(throttleForce, Color.yellow);
			}
		} else {
			// Decay
			if(rb.velocity.magnitude > minimumVelocityThreshold) {
				rb.AddForce(-rb.velocity.normalized*velocityDecay*Time.deltaTime, ForceMode.VelocityChange);
			} else {
				rb.velocity = Vector3.zero;
			}
		}

		/* Turning: Each frame, we want to change transform.forward by a certain amount specified by the steeringWheelDirection. */
		if(Math.Abs(steeringWheelDirection) > inputDeadzone) {
			float turnForce = steeringWheelDirection*
							  settings.turnSpeed*
							  DriftTurnMultiplier*
							  (momentum != -1 ? 1 : -1);

			if(!grounded) 
				turnForce *= 0.25f; // Air turn speed is a quarter of ground turn speed
				
			rb.angularVelocity = up*turnForce;
		} else
			rb.angularVelocity = Vector3.zero;

		/* Up force: It should always be that transform.up == up, add a torque to match this */
		// Code found here https://gamedev.stackexchange.com/questions/194641/how-to-set-transform-up-without-locking-the-y-axis
		// I wish I understood it. Someday.
		// Quaternion zToUp = Quaternion.LookRotation(up, -(transform.forward + transform.right.normalized*steeringWheelDirection*kartTurnSpeed));
		Quaternion zToUp = Quaternion.LookRotation(up, -transform.forward);
		Quaternion yToz = Quaternion.Euler(90, 0, 0);
		transform.rotation = zToUp * yToz;

		/* Tire force: Since we're simulating tires rolling, the velocity direction
		 *   should be in the direction of the transform.forward. */
		if(grounded) {
			Vector3 forward = RemoveUpComponent(transform.forward);
			Vector3 vel  = TrackVelocity;
			float dot = Vector3.Dot(forward, vel);
			if(Math.Abs(dot) > 0.8f) {
				// Forward is close enough in line with velocity, switch velocity to match foward
				Vector3 targetVel = forward*dot;
				rb.AddForce(targetVel-vel, ForceMode.VelocityChange);
			} else if(SpeedRatio > 0 && SpeedRatio != Mathf.Infinity) {
				// Forward is sliding sideways in relation to velocity
				// Apply a torque that rotates the car to be in line with the velocity
				Vector3 rawTorque = wheelSlideTorque*Math.Max(Mathf.Abs(dot), SpeedRatio)*-Vector3.Cross(rb.velocity.normalized, transform.forward);
				Vector3 torque = up.normalized*Vector3.Dot(rawTorque, up);
				rb.AddTorque(torque, ForceMode.Acceleration);
			}
		}

		/* Apply gravity */
		if(!grounded) 
			rb.AddForce(-up.normalized*Physics.gravity.magnitude, ForceMode.Acceleration);

		/* Check if player is stuck in ground*/
		// print("is: " + Vector3.Dot(rb.velocity, up));
		if(grounded && distanceFromGround < rideHeight-0.015f && distanceFromGround != -1)
			transform.position = groundHit.point + up*rideHeight;

		/* Interpolate up vector back to default if we're not grounded */
		if(!grounded)
			up = Vector3.Lerp(up, Vector3.up, Mathf.Clamp01(airtime/0.5f));

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
					kartStateManager.state = KartState.DRIVING;
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

		// Check if it's a wall collision
		for(int i = 0; i < collision.contactCount; i++) { 
			Vector3 norm = collision.GetContact(i).normal;
			if(Vector3.Dot(up, norm) < 0.5) { 
				timeSinceLastCollision = 0;
				break;
			}
		}

		// Kart on kart collision has bounce effect
		if(collision.gameObject.CompareTag("Kart")) {
			Vector3 collPoint = collision.GetContact(0).point;
			
			kartEffectManager.SpawnBumpEffect(collPoint);

			rb.velocity += RemoveUpComponent(transform.position-collPoint)*15f + up*2f;
			collision.rigidbody.velocity += RemoveUpComponent(collision.transform.position-collPoint)*15f + up*2f;
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
		Physics.Raycast(transform.position, -transform.up, out groundHit, rideHeight);
		if(groundHit.collider != null && groundHit.collider.CompareTag("Ground")) {
			distanceFromGround = groundHit.distance;
			if(distanceFromGround <= rideHeight) {
				up = groundHit.normal;
				return true;
			}
		}
		distanceFromGround = -1;
		return false;
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
		get { return CanMove && kartStateManager.state == KartState.DRIFTING; } 
		set {
			if(value && kartStateManager.state == KartState.DRIVING && Grounded() && !ActivelyBoosting)
				kartStateManager.state = KartState.DRIFTING;
			else if(!value && kartStateManager.state == KartState.DRIFTING)
				kartStateManager.state = KartState.DRIVING;
		}
	}

	public bool BoostInput {
		get { return CanMove && boosting; }
		set {
			if(value && !boosting && (boostAmount/settings.maxBoost) >= requiredBoostPercentage) { 
				boosting = true;
				kartStateManager.state = KartState.DRIVING;
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

	public float CurrentMaxSpeed { get { return momentum == 1 ? (ActivelyBoosting ? settings.maxBoostSpeed : settings.maxSpeed) : settings.maxSpeed/4f; } }
	public float SpeedRatio { get { return TrackSpeed/CurrentMaxSpeed; } }
	public bool CanMove { get { return GameplayManager.RaceManager.CanMove && damageCooldown <= 0; } }

	public bool SteeringWheelMatchesTurn { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(turn.x); } }
	public bool SteeringWheelMatchesDrift { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(driftDirection); } }

	public bool CanDriftEngage { get { return SpeedRatio >= driftEngageSpeedPercent && momentum == 1 && CanMove; } }

	public bool ActivelyBoosting { get { return CanMove && boosting && boostAmount > 0;} }
	public float BoostRatio { get { return boostAmount/settings.maxBoost; } }
	
	public bool IsDriftEngaged { get { return kartStateManager.state == KartState.DRIFTING && driftEngageTime == driftEngageDuration; } }
	public float DriftTurnMultiplier { 
		get { 
			if(kartStateManager.state == KartState.DRIFTING && IsDriftEngaged) {
				float driftAgeRatio = kartStateManager.timeInState/driftAge;
				return SteeringWheelMatchesDrift ? 
					Mathf.Lerp(turnMultiplierRangeDriftMatch.x, turnMultiplierRangeDriftMatch.y, 1-driftAgeRatio) :
					Mathf.Lerp(turnMultiplierRangeDriftDiffer.x, turnMultiplierRangeDriftDiffer.y, 1-driftAgeRatio);
			} else 
				return 1f;
		} 
	}

}

[Serializable]
public struct KartSettings 
{
	public float maxSpeed;
	public float maxBoost;
	public float maxBoostSpeed;
	public float acceleration;
	public float turnSpeed;
}