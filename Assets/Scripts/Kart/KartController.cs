using UnityEngine;
using System;
using UnityEngine.Rendering;
using Palmmedia.ReportGenerator.Core;
// Kart Controller is NOT ALLOWED to use UnityEngine.InputSystem. See HumanDriver!

/**
 * Kart ControllerV3 by Ethan Mullen
 */
public class KartController : MonoBehaviour
{

	/* ----- Settings variables ----- */
	public Transform kartModel;

	[Header("General")]
	public float inputDeadzone = 0.1f;
	public float wheelSlideTorque = 60;

	[Header("Speed")]
	public float maxSpeed = 20f;
	public float maxBoostSpeed = 30f;
    public float acceleration = 5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public AnimationCurve kartTurnPower;
	public float steeringWheelTurnSpeed = 5f;
	public Vector2 turnMultiplierRangeDriftMatch = new(1.5f, 2f); // Turn boost for drift that matches joystick
	public Vector2 turnMultiplierRangeDriftDiffer = new(-0.2f, 0.35f); // Turn decrease for drift that doesn't match joystick

	[Header("Drift")]
	public float driftAge = 4f; // The time (in seconds) it takes for a drift to reach its age
	public float driftAngleMin = 0.225f;
	public float driftAngleMax = 0.4f;

	public float driftHopSpeedPercent = 0.1f; // The percent of max speed that the player is allowed to hop at
	public float driftEngageSpeedPercent = 0.25f; // The percent of max speed that the player is allowed to engage a drift at
	public float driftVerticalVelocity = 3f;

	[Header("Boost")]
	public float requiredBoostPercentage = 0.3f;
	public float maxBoost = 3f; // Boost will be time in seconds
	public float boostGain = 1f;
	public float passiveBoostDrain = 3f;
	public float activeBoostDrain = 1.75f;

	/* ----- Runtime variable ----- */
	private RaceManager rm;
	private KartStateManager stateMgr;
	private Rigidbody rb;
	private PositionTracker pt;

	/* Input variables */
	[Header("Input"), SerializeField] private Vector2 turn;
	[SerializeField] private float throttle;
	[SerializeField] private bool boosting;

	[Header("Runtime fields")] public Vector3 up = new(0, 1, 0);
	// public Vector3 kartForward;
	[SerializeField] private Vector3 _kartForward;
	[SerializeField] private Vector3 velocity;
	[SerializeField] private float theta;
	public int momentum;
	public bool grounded; // Stores last update's grounded status
	public float airtime;
	public float timeSinceLastCollision;
	public float steeringWheelDirection; // A [-1, 1] range float indicating the amount the steering wheel is turned and the direction.
	public float boostAmount;

	public int driftDirection; // Indicates if we're in a left/right drift
	private float driftTheta;
	private float driftThetaTarget;
	public bool driftParticles; // True if drift particles should be showing
	private bool driftHopRequest;

    private void Start()
    {
        GameObject gm = GameObject.Find("GameplayManager");
        if(gm == null) throw new InvalidOperationException("Failed to find GameplayManager in scene!");
        rm = gm.GetComponent<RaceManager>();
		stateMgr = GetComponent<KartStateManager>();
		rb = GetComponent<Rigidbody>();
		pt = GetComponent<PositionTracker>();

		if(kartModel == null) Debug.LogWarning("KartController on \"" + gameObject.name + "\" doesn't have a kartModel assigned."); 
		timeSinceLastCollision = 1000f;
    }

    private void Update()
    {

		bool grounded = Grounded();
		if(grounded) { 
			airtime = 0;
		} else { 
			if(this.grounded) airtime = 0;
			airtime += Time.deltaTime;	
		}
		this.grounded = grounded;
		this.driftParticles = airtime <= 0.05f && stateMgr.state == KartState.DRIFTING && driftDirection != 0;

		// Steering wheel direction modification
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

		// If we're drifting and airborne, change drift direction to match joystick
		// Use airtime to ensure we maintain drift direction during small falls.
		if(stateMgr.state == KartState.DRIFTING && airtime > 0.05f && Math.Abs(TurnInput.x) >= inputDeadzone) 
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

		// Boosting
		if(ActivelyBoosting) {
			boostAmount = Mathf.Max(boostAmount - activeBoostDrain*Time.deltaTime, 0); 
		} else if(driftParticles && SteeringWheelMatchesDrift) {
			boostAmount += boostGain*Time.deltaTime;
			if(boostAmount > maxBoost) boostAmount = maxBoost;
		} else { 
			boostAmount = Mathf.Max(boostAmount - passiveBoostDrain*Time.deltaTime, 0);									
		}

		if(rm.raceTime <= 0) {
			boostAmount = rm.settings.startBoostPercent*maxBoost;
		}

		// Animate kartModel forward
		if(kartModel != null)
			kartModel.forward = RotateVectorAroundAxis(transform.forward, up, driftTheta);

	}

	private void FixedUpdate() 
	{

		DrawVector(KartForward, Color.blue);
		DrawVector(rb.velocity, Color.cyan);

		Vector3 velNoY = rb.velocity;
		velNoY.y = 0;

		timeSinceLastCollision += Time.fixedDeltaTime;

		// Momentum gets updated first, then kartForward second
		momentum = TrackSpeed > 0.1f ? (Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1) : 0;

		// Update theta
		theta = kartTurnSpeed * steeringWheelDirection * kartTurnPower.Evaluate(SpeedRatio) * Math.Max(SpeedRatio, 0.05f) * DriftTurnMultiplier * Time.fixedDeltaTime;
		if(!grounded) theta /= 3;
		if(momentum < 0) theta = -theta;

		switch(stateMgr.state) {
			case KartState.DRIVING:
			case KartState.DRIFTING:

				if(velNoY.magnitude > 0.5f) {
					KartForward = TrackVelocity.normalized*momentum;
				} else {
					Vector3 forwardNoY = transform.forward;
					forwardNoY.y = 0;
					if(forwardNoY.magnitude > 0.05f) KartForward = forwardNoY;
				}

				transform.forward = KartForward;

				Vector3 throttleForce = (ActivelyBoosting ? 1f : ThrottleInput) * (ActivelyBoosting ? acceleration*5f : acceleration) * transform.forward;
				if(Math.Abs(TurnInput.x) > inputDeadzone)
					throttleForce = RotateVectorAroundAxis(throttleForce, up, theta);

				if(Mathf.Abs(throttle) > inputDeadzone && TrackSpeed <= CurrentMaxSpeed) {
					rb.AddForce(throttleForce, ForceMode.Acceleration);	
					DrawVector(throttleForce, Color.yellow);
				}

				Vector3 turnForce = theta*1000f*-Vector3.Cross(rb.velocity.normalized, up);
				rb.AddForce(turnForce, ForceMode.Acceleration);
				DrawVector(turnForce, Color.red);

				// Correction force: if kartForward != velocity forward, apply a correctional force to fix it
				if(turnForce.magnitude <= 0.5f && momentum == 1) {
					float similarity = Vector3.Dot(KartForward.normalized, RemoveUpComponent(rb.velocity).normalized);
					Vector3 correctionForce = SpeedRatio * similarity * Mathf.Clamp01(timeSinceLastCollision/2f) * 120 * (RemoveUpComponent(KartForward)-RemoveUpComponent(rb.velocity.normalized));
					rb.AddForce(correctionForce, ForceMode.Acceleration);
					DrawVector(correctionForce, new Color(0.7f, 0.35f, 0f));
				}

				if(driftHopRequest) {
					rb.AddForce(up * driftVerticalVelocity, ForceMode.VelocityChange);
					driftHopRequest = false;
				}
				break;

			case KartState.COLLISION:

				float dot = Vector3.Dot(rb.velocity.normalized, transform.forward);
				KartForward = RemoveUpComponent(transform.forward);
				if((TrackSpeed < 0.1f*maxSpeed || Mathf.Abs(dot) > 0.975f) && timeSinceLastCollision > 0.05f) {
					stateMgr.state = KartState.DRIVING;
					momentum = (int)Mathf.Sign(dot);
				} else { 
					// Wheel sliding force
					float minSR = 0f;
					float sr = SpeedRatio;
					if(sr < minSR) sr = minSR;
					Vector3 rawTorque = wheelSlideTorque*Math.Max(Mathf.Abs(dot), sr)*-Vector3.Cross(rb.velocity.normalized, transform.forward);
					Vector3 torque = up.normalized*Vector3.Dot(rawTorque, up);
					rb.AddTorque(torque, ForceMode.Acceleration);
				}

				break;
		}

		if(airtime > 0.5f && rb.velocity.y == 0) rb.MovePosition(transform.position+Vector3.up*0.1f);

	}

	public void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.isTrigger) return;

		for(int i = 0; i < collision.contactCount; i++) { 
			Vector3 norm = collision.GetContact(i).normal;
			if(Vector3.Dot(up, norm) < 0.5) { 
				stateMgr.state = KartState.COLLISION;
				rb.AddForce(-(rb.velocity.normalized*(Vector3.Dot(norm, rb.velocity)))*0.5f, ForceMode.VelocityChange);
				return;
			}
		}

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

				driftHopRequest = true;
				break;
			case KartState.COLLISION:
				timeSinceLastCollision = 0;
				boostAmount = 0;
				break;
			default:
				break;
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

	public Vector3 RemoveUpComponent(Vector3 input) {
		return RemoveComponent(input, up);
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

	public Vector3 KartForward {
		get { return _kartForward; }
		set {
			if(AcceptableKartForward(value)) _kartForward = RemoveUpComponent(value);
		}
	}

	public bool AcceptableKartForward(Vector3 value) {
		return RemoveUpComponent(value).magnitude > 0;
	}

	public Vector3 TrackVelocity { get { 
		if(rb == null) rb = GetComponent<Rigidbody>();
		return RemoveUpComponent(rb.velocity); 
	} }
	/** The velocity magnitude tangential to the up vector */
	public float TrackSpeed { get { return TrackVelocity.magnitude; } }

	public void DrawVector(Vector3 forceVector, Color col) 
	{
		Debug.DrawLine(transform.position, transform.position + forceVector, col);
	}

	public float CurrentMaxSpeed { get { return momentum == 1 ? (ActivelyBoosting ? maxBoostSpeed : maxSpeed) : maxSpeed/4f; } }
	public float SpeedRatio { get { return TrackSpeed/CurrentMaxSpeed; } }
	public bool CanMove { get { return rm != null && rm.CanMove && pt.lapNumber < rm.settings.laps; } }

	public bool SteeringWheelMatchesTurn { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(turn.x); } }
	public bool SteeringWheelMatchesDrift { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(driftDirection); } }

	public bool CanDriftEngage { get { return SpeedRatio >= driftEngageSpeedPercent && momentum == 1 && CanMove; } }

	public bool ActivelyBoosting { get { return CanMove && boosting && boostAmount > 0;} }
	public float BoostRatio { get { return boostAmount/maxBoost; } }
	
	public float DriftTurnMultiplier { 
		get { 
			if(stateMgr.state == KartState.DRIFTING) {
				float driftAgeRatio = stateMgr.timeInState/driftAge;
				return SteeringWheelMatchesDrift ? 
					Mathf.Lerp(turnMultiplierRangeDriftMatch.x, turnMultiplierRangeDriftMatch.y, 1-driftAgeRatio) :
					Mathf.Lerp(turnMultiplierRangeDriftDiffer.x, turnMultiplierRangeDriftDiffer.y, 1-driftAgeRatio);
			} else 
				return 1f;
		} 
	}

	private void DrawForces() { 
		// Debug.DrawLine(transform.position, transform.position + kartForward*3f, Color.blue, Time.deltaTime);
	}

}